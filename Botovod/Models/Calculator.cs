using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Botovod.Models
{
    public class Calculator : BindableBase
    {
        //--------------old----------------
        private readonly ObservableCollection<int> _myValues = new ObservableCollection<int>();
        public readonly ReadOnlyObservableCollection<int> MyPublicValues;
        //--------------

        public Trader Trader { get; }
        //        
        public InitializedData initData;
        // общий таймер для всех сделок.
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

        public Calculator(InitializedData inInitData)
        {
            //--------------old----------------
            MyPublicValues = new ReadOnlyObservableCollection<int>(_myValues);
            //--------------

            initData = inInitData;
            Trader = new Trader(initData);

            // настройки таймера
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = new TimeSpan(0, 0, 6);
            timer.Start();
        }
        //--------------old----------------
        //добавление в коллекцию числа и уведомление об изменении суммы
        public void AddValue(int value)
        {
            _myValues.Add(value);
            RaisePropertyChanged("Sum");
        }
        //проверка на валидность, удаление из коллекции и уведомление об изменении суммы
        public void RemoveValue(int index)
        {
            //проверка на валидность удаления из коллекции - обязанность модели
            if (index >= 0 && index < _myValues.Count) _myValues.RemoveAt(index);
            RaisePropertyChanged("Sum");
        }
        public int Sum => MyPublicValues.Sum(); //сумма

        public static int GetSumOf(int a, int b) => a + b;
        //--------------

        public int Sum2 => MyPublicValues.Sum(); //сумма

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
                if (string.IsNullOrEmpty(initData.GetKData) || string.IsNullOrEmpty(initData.GetSData)) { return; }

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
                if (currentTralingPercent < deal.LastFundPercent + deal.LblSafetyOrder)
                {
                    deal.IsTrailing = true;
                    // проверка на отклонение цены вверх(лонг) от минимального, чтоб начать усреднение
                    decimal deltaTrailing = currentTralingPercent - tralingMaxPercent;
                    deal.OutMessage_Deal = $"Трейлинг активирован {Math.Round(deltaTrailing, 2)}%";
                    if (deltaTrailing > deal.LblTrailingPercent)
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
