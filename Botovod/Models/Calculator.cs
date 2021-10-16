using System;

namespace Botovod.Models
{
    public class Calculator
    {
        public Trader Trader { get; }
        //        
        public InitializedData initData;
        // общий таймер для всех сделок.
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

        public Calculator(InitializedData inInitData)
        {
            initData = inInitData;
            Trader = new Trader(initData);

            // настройки таймера
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = new TimeSpan(0, 0, 6);
            timer.Start();
        }

        /// <summary>
        /// Основной калькулятор
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void timerTick(object sender, EventArgs e)
        {
            timer.Stop();  // глушим таймер дабы дабы

            // грузим все сделки с сервиса. асинхронно, чтоб блять результат дождаться, а то будет все херово.
            // все херово - это когда изменения свойств с Trader.GetDeals() приходят позже timerTick(object sender, EventArgs e)
            // deal.OutMessage_Deal - вот эта херь например будет работать наоборот, затирая все данные с этого метода!
            bool resultGetDeals = await Trader.GetDeals();
            if (!resultGetDeals)
            {
                if (string.IsNullOrEmpty(initData.KData) || string.IsNullOrEmpty(initData.SData)) { return; }

            }
            // обходим все сделки по порядку
            foreach (BotovodDeal deal in Trader.TraderDeals)
            {
                if (!resultGetDeals)
                {
                    deal.OutMessage_Deal = "GetDeals вернул ошибку";
                    continue;
                }

                decimal currentPrice = deal.xDeal.CurrentPrice; // текущая цена
                decimal boughtAveragePrice = deal.xDeal.BoughtAveragePrice; // средняя цена сделки
                // текущее отклонение цены в %
                decimal currentTralingPercent;   //= (double)deal.xDeal.ActualProfitPercentage;
                decimal deltaPrice = currentPrice - boughtAveragePrice;   //0,0001
                if (deltaPrice == 0) { currentTralingPercent = 0; }
                else { currentTralingPercent = deltaPrice * 100 / boughtAveragePrice; }
                // максимальное отклонение цены в %
                decimal tralingMaxPercent;
                decimal deltaMaxPrice = deal.TrailingMaxPrice - boughtAveragePrice; // отклонение от средней цены, а не от текущей
                if (deltaMaxPrice == 0) { tralingMaxPercent = 0; }
                else { tralingMaxPercent = deltaMaxPrice * 100 / boughtAveragePrice; }
                // алгоритм для лонгового ботаs
                if (currentTralingPercent < tralingMaxPercent)
                {
                    deal.TrailingMaxPrice = currentPrice;    // обновляем максимальное отклонение 
                    deal.LblTrailingMaxPercent = Math.Round(currentTralingPercent, 2);
                }
                deal.LblCurrentTrailing = Math.Round(currentTralingPercent, 2);
                // если активирован трейлинг то сменить цвет на CornflowerBlue SteelBlue DeepSkyBlue
                // проверка на активацию трейлинга
                decimal deltaLastFundPrice = deal.LastFundPrice - boughtAveragePrice; // отклонение от средней цены, а не от текущей
                if (deltaLastFundPrice == 0) { deal.LastFundPercent = 0; }
                else { deal.LastFundPercent = deltaLastFundPrice * 100 / boughtAveragePrice; }
                // проверяем на диапазон отклонений и ограничений количества СО
                if ((currentTralingPercent < deal.LastFundPercent - deal.SafetyOrderStep) && 
                        (deal.xDeal.CompletedManualSafetyOrdersCount < deal.MaxSafetyOrders))
                {
                    deal.IsTrailing = true;
                    // проверка на отклонение цены вверх(лонг) от минимального, чтоб начать усреднение
                    decimal deltaTrailing = currentTralingPercent - tralingMaxPercent;
                    deal.OutMessage_Deal = $"Трейлинг активирован {Math.Round(deltaTrailing, 2)}%";
                    if (deltaTrailing > deal.TrailingDeviation)
                    {
                        deal.OutMessage_Deal = "Усредняюсь";
                        bool resultAddFunds = await Trader.AddFunds(deal);
                        if (resultAddFunds)
                        {
                            deal.LastFundPrice = currentPrice;  // обновляем последнее усреднениеы
                            deal.TrailingMaxPrice = currentPrice; // обнувляем максимальное отклонение, иначе будет усреднять до посинения.
                        }
                        deal.IsTrailing = false;
                    }

                }
                else { deal.IsTrailing = false; }
            }
            // восстанавливаем таймер
            timer.Start();
        }

        
    }
}
