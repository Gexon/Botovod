using System;
using System.Threading.Tasks;
using System.Windows;

namespace Botovod.Models
{
    public class Calculator
    {
        public Trader Trader { get; }

        //        
        public readonly InitializedData InitData;

        // общий таймер для всех сделок.
        private readonly System.Windows.Threading.DispatcherTimer _timer =
            new System.Windows.Threading.DispatcherTimer();

        public Calculator(InitializedData inInitData)
        {
            // проверка API ключей
            if (string.IsNullOrEmpty(inInitData.KData) || string.IsNullOrEmpty(inInitData.SData))
            {
                //Log.Error("GetDeals. API ключи не найдены");
                MessageBox.Show("Вбейте API ключи в настройках", "GetDeals", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            //
            InitData = inInitData;
            Trader = new Trader(InitData);

            // настройки таймера
            _timer.Tick += TimerTick;
            _timer.Interval = new TimeSpan(0, 0, 6);
            _timer.Start();
            
        }

        /// <summary>
        /// Основной калькулятор
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TimerTick(object sender, EventArgs e)
        {
            _timer.Stop(); // глушим таймер дабы дабы

            // грузим все сделки с сервиса. асинхронно, чтоб блять результат дождаться, а то будет все херово.
            // все херово - это когда изменения свойств с Trader.GetDeals() приходят позже timerTick(object sender, EventArgs e)
            // deal.OutMessage_Deal - вот эта херь например будет работать наоборот, затирая все данные с этого метода!
            var resultGetDeals = await Trader.GetDeals();
            if (!resultGetDeals)
            {
                if (string.IsNullOrEmpty(InitData.KData) || string.IsNullOrEmpty(InitData.SData))
                {
                    return;
                }
            }

            // обходим все сделки по порядку
            foreach (var deal in Trader.TraderDeals)
            {
                if (!resultGetDeals)
                {
                    deal.OutMessageDeal = "GetDeals вернул ошибку";
                    //todo переделать обработку ошибок API
                    await Task.Delay(60000); //Ждем 60 сек и продолжаем работу, запускаем таймер дальше.
                    continue;
                }

                // главный мозг
                await CalculateTrailing(deal); 
            }

            // восстанавливаем таймер
            _timer.Start();
        }

        // Основыные расчеты движения цены, процентов, отклонений трейлинга.
        private async Task CalculateTrailing(BotovodDeal deal)
        {
            if (deal.XDeal == null) return;
            var currentPrice = deal.XDeal.CurrentPrice;
            if (currentPrice == 0)
            {
                return;
            }

            var boughtAveragePrice = deal.XDeal.BoughtAveragePrice; // средняя цена сделки
            if (boughtAveragePrice == 0)
            {
                boughtAveragePrice = currentPrice;
            }

            // рассчитываем текущее отклонение цены в %
            // deal.xDeal.ActualProfitPercentage;
            var currentTrailingPercent = (currentPrice - boughtAveragePrice) * 100 / boughtAveragePrice;
            // считаем максимальное отклонение цены в %
            var trailingMaxPercent = (deal.TrailingMaxPrice - boughtAveragePrice) * 100 / boughtAveragePrice;

            // обновляем максимальное отклонение (алгоритм для лонгового ботаs)
            if (currentTrailingPercent < trailingMaxPercent)
            {
                deal.TrailingMaxPrice = currentPrice; // обновляем максимальное отклонение 
                deal.LblTrailingMaxPercent = Math.Round(currentTrailingPercent, 2);
            }

            deal.LblCurrentTrailing = Math.Round(currentTrailingPercent, 2);

            // вычисляем последнее усреднение, %. Отклонение от средней цены, а не от текущей(не ну если средняя равна нулю, то тогда от текущей)
            deal.LastFundPercent = (deal.LastFundPrice - boughtAveragePrice) * 100 / boughtAveragePrice;

            // проверка на активацию трейлинга
            // проверяем текущее отклонение цены в % < (последнее усреднение, % - шаг страховочного ордера в %). и ограничений количества СО. 
            if ((currentTrailingPercent < deal.LastFundPercent - deal.SafetyOrderStep) &&
                (deal.XDeal.CompletedManualSafetyOrdersCount < deal.MaxSafetyOrders))
            {
                // Как вариант, включать трейлинг после индикатора MACD ниже 0.
                deal.IsTrailing = true;
                // проверка на отклонение цены вверх(лонг) от минимального, чтоб начать усреднение
                var deltaTrailing = currentTrailingPercent - trailingMaxPercent;
                deal.OutMessageDeal = $"Трейлинг активирован {Math.Round(deltaTrailing, 2)}%";
                if (deltaTrailing > deal.TrailingDeviation)
                {
                    deal.OutMessageDeal = "Усредняюсь";
                    var resultAddFunds = await Trader.AddFunds(deal);
                    if (resultAddFunds)
                    {
                        deal.LastFundPrice = currentPrice; // обновляем последнее усреднениеы
                        deal.TrailingMaxPrice =
                            currentPrice; // обнувляем максимальное отклонение, иначе будет усреднять до посинения..
                    }

                    deal.IsTrailing = false;
                }
            }
            else
            {
                deal.IsTrailing = false;
            }
        }
        
        // коэффициент Шарпа
    }
}