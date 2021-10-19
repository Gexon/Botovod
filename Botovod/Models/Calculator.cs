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
            timer.Tick += TimerTick;
            timer.Interval = new TimeSpan(0, 0, 6);
            timer.Start();
        }

        /// <summary>
        /// Основной калькулятор
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TimerTick(object sender, EventArgs e)
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
                if (currentPrice == 0) { continue; }
                decimal boughtAveragePrice = deal.xDeal.BoughtAveragePrice; // средняя цена сделки
                if (boughtAveragePrice == 0) { boughtAveragePrice = currentPrice; }

                // рассчитываем текущее отклонение цены в %
                // deal.xDeal.ActualProfitPercentage;
                decimal currentTralingPercent = (currentPrice - boughtAveragePrice) * 100 / boughtAveragePrice;
                // считаем максимальное отклонение цены в %
                decimal tralingMaxPercent = (deal.TrailingMaxPrice - boughtAveragePrice) * 100 / boughtAveragePrice;

                // обновляем максимальное отклонение (алгоритм для лонгового ботаs)
                if (currentTralingPercent < tralingMaxPercent)
                {
                    deal.TrailingMaxPrice = currentPrice;    // обновляем максимальное отклонение 
                    deal.LblTrailingMaxPercent = Math.Round(currentTralingPercent, 2);
                }
                deal.LblCurrentTrailing = Math.Round(currentTralingPercent, 2);

                // вычисляем последнее усреднение, %
                // отклонение от средней цены, а не от текущей
                deal.LastFundPercent = (deal.LastFundPrice - boughtAveragePrice) * 100 / boughtAveragePrice;

                // проверка на активацию трейлинга
                // проверяем текущее отклонение цены в % < (последнее усреднение, % - шаг страховочного ордера в %). и ограничений количества СО. 
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
