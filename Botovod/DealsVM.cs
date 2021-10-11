using Botovod.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows;

namespace Botovod
{
    // прокладка ViewModel описания сделок
    public class DealVM : BindableBase
    {
        public BotovodDeal BotovodDeal { get; }
        public DealVM(BotovodDeal botovodDeal, Calculator calculator = null)
        {
            BotovodDeal = botovodDeal;
            // проброс уведомлений. если что-то меняется у BotovodDeal, то обновить соответствующее поле у DealVM
            botovodDeal.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(PrgTrailing)); }; // без этой херни не обновляется
            botovodDeal.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(PrgTrailingR)); }; // без этой херни не обновляется
            botovodDeal.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(Foreground)); }; // без этой херни не обновляется
            botovodDeal.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(ForegroundR)); }; // без этой херни не обновляется
            botovodDeal.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(ManualSafetyOrders)); }; // без этой херни не обновляется
            botovodDeal.PropertyChanged += (s, a) => { RaisePropertyChanged(a.PropertyName); };

            if (calculator != null)
                BuyCommand = new DelegateCommand(() =>
                {
                    //calculator.Trader.AddFunds(BotovodDeal);
                });
        }

        public Visibility IsBuyVisible => BuyCommand == null ? Visibility.Collapsed : Visibility.Visible;
        public DelegateCommand BuyCommand { get; }
        public string BotName => BotovodDeal.xDeal.BotName;
        public string OutMessage_Deal => BotovodDeal.OutMessage_Deal;
        public Visibility IsAmountVisible => BuyCommand == null ? Visibility.Collapsed : Visibility.Visible;
        public string Pair => BotovodDeal.xDeal.Pair;
        public decimal CurrentPrice => BotovodDeal.CurrentPrice;
        public decimal LblCurrentTrailing => BotovodDeal.LblCurrentTrailing; // текущее отклонение цены в % "Value was either too large or too small for a Decimal."
        public decimal LblTrailingMaxPercent => BotovodDeal.LblTrailingMaxPercent; // максимальное отклонение цены в % //  Math.Round(25.657446842, 2) // Выведет 25,66 
        public decimal PrgTrailing => BotovodDeal.LblCurrentTrailing < 0 ? Math.Abs(BotovodDeal.LblCurrentTrailing)*2 : 0;   // значение Отрицательной шкалы прогресса
        public decimal PrgTrailingR => BotovodDeal.LblCurrentTrailing >= 0 ? BotovodDeal.LblCurrentTrailing*2 : 0; // значение Положительной-правой шкалы прогресса
        // меняем цвет шкалы
        public string Foreground => !BotovodDeal.IsTrailing ? "Crimson" : "CornflowerBlue";
        public string ForegroundR => !BotovodDeal.IsTrailing ? "LightSeaGreen" : "CornflowerBlue";
        public decimal LblSafetyOrder => BotovodDeal.LblSafetyOrder; // шаг страховочного ордера в %
        public decimal LblTrailingPercent => BotovodDeal.LblTrailingPercent; // Трейлинг отклонение %
        public int ManualSafetyOrders => BotovodDeal.xDeal.CompletedManualSafetyOrdersCount; // количество усреднений
        public decimal LastFundPercent => Math.Round(BotovodDeal.LastFundPercent, 2); // последнее усреднение, %s

    }

}
