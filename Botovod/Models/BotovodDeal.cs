using Prism.Mvvm;

namespace Botovod.Models
{
    public class BotovodDeal : BindableBase
    {
        public BotovodDeal(XCommas.Net.Objects.Deal inXDeal)
        {
            xDeal = inXDeal;
            CurrentPrice = inXDeal.CurrentPrice;
            OutMessage_Deal = "Новый";
            // вычисляемые поля/свойства
            TrailingMaxPrice = inXDeal.CurrentPrice;
            LblTrailingMaxPercent = (decimal)inXDeal.ActualProfitPercentage;
            LblCurrentTrailing = 0;            
            // параметры трейлинга
            LblSafetyOrder = -0.7m;
            LblTrailingPercent = 0.3m;
            IsTrailing = false;
            LastFundPrice = inXDeal.CurrentPrice;
        }


        public XCommas.Net.Objects.Deal xDeal { get; internal set; }
        // текущая цена
        private decimal _currentPrice;
        public decimal CurrentPrice
        {
            get => _currentPrice;
            set { SetProperty(ref _currentPrice, value); }
        }
        // вывод сосотояния сделки
        private string _outMessage_Deal;
        public string OutMessage_Deal
        {
            get => _outMessage_Deal;
            set { SetProperty(ref _outMessage_Deal, value); }
        }
        // максимальное отклонение цены, цена
        private decimal _trailingMax;
        public decimal TrailingMaxPrice
        {
            get => _trailingMax;
            set { SetProperty(ref _trailingMax, value); }
        }
        // максимальное отклонение цены, %
        private decimal _lblTrailingMaxPercent;
        public decimal LblTrailingMaxPercent
        {
            get => _lblTrailingMaxPercent;
            set { SetProperty(ref _lblTrailingMaxPercent, value); }
        }
        // отображаем текущий процент отклонения, потому-что округленный
        private decimal _lblCurrentTrailing;
        public decimal LblCurrentTrailing
        {
            get => _lblCurrentTrailing;
            set { SetProperty(ref _lblCurrentTrailing, value); }
        }
        // отображаем шаг страховочного ордера в %
        private decimal _lblSafetyOrder;
        public decimal LblSafetyOrder
        {
            get => _lblSafetyOrder;
            set { SetProperty(ref _lblSafetyOrder, value); }
        }
        // отображаем Трейлинг отклонение %
        private decimal _lblTrailingPercent;
        public decimal LblTrailingPercent
        {
            get => _lblTrailingPercent;
            set { SetProperty(ref _lblTrailingPercent, value); }
        }
        // флаг трейлинга, хз зачем, может чтоб красить шкалу прогресса
        private bool _isTrailing;
        public bool IsTrailing
        {
            get => _isTrailing;
            set { SetProperty(ref _isTrailing, value); }
        }
        // последнее усреднение, цена
        private decimal _lastFundPrice;
        public decimal LastFundPrice
        {
            get => _lastFundPrice;
            set { SetProperty(ref _lastFundPrice, value); }
        }
        // последнее усреднение, %
        private decimal _lastFundPercent;
        public decimal LastFundPercent
        {
            get => _lastFundPercent;
            set { SetProperty(ref _lastFundPercent, value); }
        }
        
    }
}
