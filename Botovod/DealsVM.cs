using Botovod.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Botovod
{
    // прокладка ViewModel описания сделок
    public class DealVM : INotifyPropertyChanged, IDisposable
    {
        public BotovodDeal BotovodDeal { get; }
        public DealVM(BotovodDeal botovodDeal, Calculator calculator = null)
        {
            BotovodDeal = botovodDeal;
            BotovodDeal.PropertyChanged += Deal_PropertyChanged;
        }

        public void Dispose()
        {
            BotovodDeal.PropertyChanged -= Deal_PropertyChanged;
        }

        //public Visibility IsBuyVisible => BuyCommand == null ? Visibility.Collapsed : Visibility.Visible;        
        public string BotName => BotovodDeal.XDeal.BotName;
        public string OutMessageDeal => BotovodDeal.OutMessageDeal;
        //public Visibility IsAmountVisible => BuyCommand == null ? Visibility.Collapsed : Visibility.Visible;
        public string Pair => BotovodDeal.XDeal.Pair;
        public decimal CurrentPrice => BotovodDeal.CurrentPrice;
        public decimal LblCurrentTrailing => BotovodDeal.LblCurrentTrailing; // текущее отклонение цены в % "Value was either too large or too small for a Decimal."
        public decimal LblTrailingMaxPercent => BotovodDeal.LblTrailingMaxPercent; // максимальное отклонение цены в % //  Math.Round(25.657446842, 2) // Выведет 25,66 
        public decimal PrgTrailing => BotovodDeal.LblCurrentTrailing < 0 ? Math.Abs(BotovodDeal.LblCurrentTrailing) * 2 : 0;   // значение Отрицательной шкалы прогресса
        public decimal PrgTrailingR => BotovodDeal.LblCurrentTrailing >= 0 ? BotovodDeal.LblCurrentTrailing * 2 : 0; // значение Положительной-правой шкалы прогресса
        // меняем цвет шкалы
        public string Foreground => !BotovodDeal.IsTrailing ? "Crimson" : "CornflowerBlue";
        public string ForegroundR => !BotovodDeal.IsTrailing ? "LightSeaGreen" : "CornflowerBlue";
        public decimal SafetyOrderStep  // шаг страховочного ордера в %
        {
            get => BotovodDeal.SafetyOrderStep;
            set => BotovodDeal.SafetyOrderStep = value;
        }
        public decimal TrailingDeviation // Трейлинг отклонение %
        {
            get => BotovodDeal.TrailingDeviation;
            set => BotovodDeal.TrailingDeviation = value;
        }
        public int ManualSafetyOrders => BotovodDeal.XDeal.CompletedManualSafetyOrdersCount; // количество усреднений
        public decimal LastFundPercent => Math.Round(BotovodDeal.LastFundPercent, 2); // последнее усреднение, %s
        public string MaxSafetyOrders   // Максимум усреднений
        {
            get => BotovodDeal.MaxSafetyOrders.ToString();
            set { if (int.TryParse(value, out int num)) { BotovodDeal.MaxSafetyOrders = num; }
            }
        }

        // Чтобы получать изменение не из VM, а из наблюдаемого класса
        private void Deal_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // проброс уведомлений. если что-то меняется у BotovodDeal, то обновить соответствующее поле у DealVM
            RaisePropertyChanged(nameof(PrgTrailing));
            RaisePropertyChanged(nameof(PrgTrailingR));
            RaisePropertyChanged(nameof(Foreground));
            RaisePropertyChanged(nameof(ForegroundR));
            RaisePropertyChanged(nameof(ManualSafetyOrders));
            RaisePropertyChanged(nameof(SafetyOrderStep));
            RaisePropertyChanged(nameof(TrailingDeviation));
            RaisePropertyChanged(nameof(LblTrailingMaxPercent));
            RaisePropertyChanged(e.PropertyName);
            //RaisePropertyChanged();    // говорят это заставляет обновить все поля
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string prop = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    }
}
