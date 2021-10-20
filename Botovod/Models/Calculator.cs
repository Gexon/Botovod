using System;
using System.Threading.Tasks;

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
                    continue;
                }

                // главный мозг
                if (!await CalculateTrailing(deal)) return; // серьезная ошибка, покидаем лодку.
            }

            // восстанавливаем таймер
            _timer.Start();
        }

        // Основыные расчеты движения цены, процентов, отклонений трейлинга.
        private async Task<bool> CalculateTrailing(BotovodDeal deal)
        {
            var currentPrice = deal.XDeal.CurrentPrice;
            if (currentPrice == 0)
            {
                return false;
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

            // вычисляем последнее усреднение, %
            // отклонение от средней цены, а не от текущей
            deal.LastFundPercent = (deal.LastFundPrice - boughtAveragePrice) * 100 / boughtAveragePrice;

            // проверка на активацию трейлинга
            // проверяем текущее отклонение цены в % < (последнее усреднение, % - шаг страховочного ордера в %). и ограничений количества СО. 
            if ((currentTrailingPercent < deal.LastFundPercent - deal.SafetyOrderStep) &&
                (deal.XDeal.CompletedManualSafetyOrdersCount < deal.MaxSafetyOrders))
            {
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
                            currentPrice; // обнувляем максимальное отклонение, иначе будет усреднять до посинения.
                    }

                    deal.IsTrailing = false;
                }
            }
            else
            {
                deal.IsTrailing = false;
            }

            return true;
        }
    }
}