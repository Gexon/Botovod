using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Botovod.Models
{
    /// <summary>
    /// Храним данные для инициализации нашей модели
    /// </summary>
    public class InitializedData : INotifyPropertyChanged
    {
        public InitializedData()
        {
            _KData = Properties.Settings.Default.kData;
            _SData = Properties.Settings.Default.sData;
            _maxSafetyOrders = Properties.Settings.Default.MaxSafetyOrders;
            _trailingDeviation = Properties.Settings.Default.TrailingDeviation;
            _safetyOrderStep = Properties.Settings.Default.SafetyOrderStep;
        }

        private string _KData;
        public string KData
        {
            get => _KData;
            set {
                if (_KData == value) { return; };
                _KData = value;
                Properties.Settings.Default.kData = _KData;
                Properties.Settings.Default.Save(); // Сохраняем переменные.
                RaisePropertyChanged();
            }
        }

        private string _SData;
        public string SData
        {
            get => _SData;
            set {
                if (_SData == value) { return; };
                _SData = value;
                Properties.Settings.Default.sData = _SData;
                Properties.Settings.Default.Save(); // Сохраняем переменные.
                RaisePropertyChanged();
            }
        }

        private int _maxSafetyOrders;    // Максимальное количество страховочных ордеров
        public int MaxSafetyOrders
        {
            get => _maxSafetyOrders;
            set {
                if (_maxSafetyOrders == value) { return; };
                _maxSafetyOrders = value;
                Properties.Settings.Default.MaxSafetyOrders = _maxSafetyOrders;
                Properties.Settings.Default.Save(); // Сохраняем переменные.
                RaisePropertyChanged();
            }
        }

        private decimal _trailingDeviation;  // Трейлинг отклонение, %
        public decimal TrailingDeviation
        {
            get => _trailingDeviation;
            set {
                if (_trailingDeviation == value) { return; };
                _trailingDeviation = value;
                Properties.Settings.Default.TrailingDeviation = _trailingDeviation;
                Properties.Settings.Default.Save(); // Сохраняем переменные.
                RaisePropertyChanged();
            }
        }

        private decimal _safetyOrderStep;    // Шаг страховочного ордера, %
        public decimal SafetyOrderStep
        {
            get => _safetyOrderStep;
            set {
                if (_safetyOrderStep == value) { return; };
                _safetyOrderStep = value;
                Properties.Settings.Default.SafetyOrderStep = _safetyOrderStep;
                Properties.Settings.Default.Save(); // Сохраняем переменные.
                RaisePropertyChanged();
            }
        }

        // ! не забудь в конструкторе задать переменным значения ! АЛЁ!!!!

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string prop = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
