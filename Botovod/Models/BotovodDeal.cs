using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Botovod.Models
{
    [Serializable]
    public class BotovodDeal : INotifyPropertyChanged
    {
        public BotovodDeal(XCommas.Net.Objects.Deal inXDeal)
        {
            XDeal = inXDeal;
            Id = inXDeal.Id;
            CurrentPrice = inXDeal.CurrentPrice;
            OutMessageDeal = "Новый";
            BotName = inXDeal.BotName;
            Pair = inXDeal.Pair;
            ManualSafetyOrders = inXDeal.CompletedManualSafetyOrdersCount;
            // вычисляемые поля/свойства
            TrailingMaxPrice = inXDeal.CurrentPrice;
            LblTrailingMaxPercent = inXDeal.ActualProfitPercentage != null
                ? LblTrailingMaxPercent = (decimal)inXDeal.ActualProfitPercentage
                : 0m;
            LblCurrentTrailing = 0;
            LastFundPrice = inXDeal.BoughtAveragePrice > 0 ? inXDeal.BoughtAveragePrice : inXDeal.CurrentPrice;
            // параметры трейлинга
            SafetyOrderStep = 0.7m;
            TrailingDeviation = 0.3m;
            IsTrailing = false;
        }

        [field: NonSerialized] private XCommas.Net.Objects.Deal _xDeal;

        public XCommas.Net.Objects.Deal XDeal
        {
            get => _xDeal;
            internal set
            {
                _xDeal = value;
                RaisePropertyChanged();
            }
        }

        // Id сделки
        private int _id;

        public int Id
        {
            get => _id;
            set => _id = value;
        }

        // имя бота
        private string _botName;

        public string BotName
        {
            get => _botName;
            set => _botName = value;
        }

        // валютная пара
        private string _pair;

        public string Pair
        {
            get => _pair;
            set => _pair = value;
        }

        // текущая цена
        private decimal _currentPrice;

        public decimal CurrentPrice
        {
            get => _currentPrice;
            set
            {
                if (_currentPrice == value) return;
                _currentPrice = value;
                RaisePropertyChanged();
            }
        }

        // вывод сосотояния сделки
        private string _outMessageDeal;

        public string OutMessageDeal
        {
            get => _outMessageDeal;
            set
            {
                if (_outMessageDeal == value) return;
                _outMessageDeal = value;
                RaisePropertyChanged();
            }
        }

        // максимальное отклонение цены, цена
        private decimal _trailingMax;

        public decimal TrailingMaxPrice
        {
            get => _trailingMax;
            set
            {
                if (_trailingMax == value) return;
                _trailingMax = value;
                RaisePropertyChanged();
            }
        }

        // максимальное отклонение цены, %
        private decimal _lblTrailingMaxPercent;

        public decimal LblTrailingMaxPercent
        {
            get => _lblTrailingMaxPercent;
            set
            {
                if (_lblTrailingMaxPercent == value) return;
                _lblTrailingMaxPercent = value;
                RaisePropertyChanged();
            }
        }

        // отображаем текущий процент отклонения, потому-что округленный
        private decimal _lblCurrentTrailing;

        public decimal LblCurrentTrailing
        {
            get => _lblCurrentTrailing;
            set
            {
                if (_lblCurrentTrailing == value) return;
                _lblCurrentTrailing = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// отображаем шаг страховочного ордера в %
        /// </summary>
        private decimal _safetyOrderStep;

        public decimal SafetyOrderStep
        {
            get => _safetyOrderStep;
            set
            {
                if (_safetyOrderStep == value) return;
                _safetyOrderStep = value;
                RaisePropertyChanged();
            }
        }

        // отображаем Трейлинг отклонение %
        private decimal _trailingDeviation;

        public decimal TrailingDeviation
        {
            get => _trailingDeviation;
            set
            {
                if (_trailingDeviation == value) return;
                _trailingDeviation = value;
                RaisePropertyChanged();
            }
        }

        // флаг трейлинга, хз зачем, может чтоб красить шкалу прогресса
        private bool _isTrailing;

        public bool IsTrailing
        {
            get => _isTrailing;
            set
            {
                if (_isTrailing == value) return;
                _isTrailing = value;
                RaisePropertyChanged();
            }
        }

        // последнее усреднение, цена
        private decimal _lastFundPrice;

        public decimal LastFundPrice
        {
            get => _lastFundPrice;
            set
            {
                if (_lastFundPrice == value || value <= 0) return;
                _lastFundPrice = value;
                RaisePropertyChanged();
            }
        }

        // последнее усреднение, %
        private decimal _lastFundPercent;

        public decimal LastFundPercent
        {
            get => _lastFundPercent;
            set
            {
                if (_lastFundPercent == value) return;
                _lastFundPercent = value;
                RaisePropertyChanged();
            }
        }

        // Максимум усреднений
        private int _maxSafetyOrders;

        public int MaxSafetyOrders
        {
            get => _maxSafetyOrders;
            set
            {
                if (_maxSafetyOrders == value) return;
                _maxSafetyOrders = value;
                RaisePropertyChanged();
            }
        }

        // количество выполненных усреднений
        private int _manualSafetyOrders;

        public int ManualSafetyOrders
        {
            get => _manualSafetyOrders;
            set
            {
                if (_manualSafetyOrders == value) return;
                _manualSafetyOrders = value;
                RaisePropertyChanged();
            }
        }

        //todo скорость роста/падения цены
        // массив цен с интервалом в Х сек(6 сек)


        [field: NonSerialized] public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string prop = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}