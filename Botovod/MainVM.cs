using Botovod.Commands;
using Botovod.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Botovod
{
    // Основная прокладка.
    // Этот вьюмоделя управляем только общим списком сделок - DealsInBotovod,
    // внутри не лезет, только элементы списка. Содержимое элементов в соседнем моделвью DealVM
    class MainVM : INotifyPropertyChanged, IDisposable
    {
        private Calculator calculator;
        private Trader _trader;
        internal Action RequestClose;

        // список сделок для отображения в интерфейсе
        public ObservableCollection<DealVM> DealsInBotovod { get; }

        //  Максимальное количество страховочных ордеров (для всех новых сделок)
        public int MaxSafetyOrders
        {
            get => calculator.initData.MaxSafetyOrders;
            set => calculator.initData.MaxSafetyOrders = value;
        }
        
        // Трейлинг отклонение, % (для всех новых сделок)
        public decimal TrailingDeviation
        {
            get => calculator.initData.TrailingDeviation;
            set => calculator.initData.TrailingDeviation = value;
        }
        // Шаг страховочного ордера, % (для всех новых сделок)
        public decimal SafetyOrderStep
        {
            get => calculator.initData.SafetyOrderStep;
            set => calculator.initData.SafetyOrderStep = value;
        }

        public MainVM(Calculator inCalculator)
        {
            calculator = inCalculator;
            _trader = calculator.Trader;

            calculator.initData.PropertyChanged += InitData_PropertyChanged;
            // сделки торговца
            //DealsInBotovod = new ObservableCollection<DealVM>(_trader.TraderDeals.Select(ap => new DealVM(ap, calculator)));
            _trader.FillingDeals(BinaryDataStorage.LoadDealsFormDisk());
            DealsInBotovod = new ObservableCollection<DealVM>(_trader.TraderDeals.Select(dl => new DealVM(dl)));
            //Watch(_trader.TraderDeals, DealsInBotovod, p => p.BotovodDeal);

            // преобразовывать каждый добавленный или удаленный элемент из модели
            // Синхронизация здесь с _trader.TraderDeals. Там убавилось, тут убавляем. Прибавилось - прибавляем.
            ((INotifyCollectionChanged)_trader.TraderDeals).CollectionChanged += (s, a) =>
            {
                //if (a.NewItems?.Count == 1) DealsInBotovod.Add(new DealVM(a.NewItems[0] as BotovodDeal));
                if (a.NewItems?.Count >= 1) { foreach (BotovodDeal deal in a.NewItems) { DealsInBotovod.Add(new DealVM(deal)); } }

                //if (a.OldItems?.Count == 1) DealsInBotovod.Remove(DealsInBotovod.First(mv => mv.BotovodDeal == a.OldItems[0]));
                if (a.OldItems?.Count >= 1) { foreach (BotovodDeal deal in a.OldItems) { DealsInBotovod.Remove(DealsInBotovod.First(mv => mv.BotovodDeal == deal)); } }

            };

        }

        public void Dispose()
        {
            calculator.initData.PropertyChanged -= InitData_PropertyChanged;
        }

        // Чтобы получать изменение не из VM, а из наблюдаемого класса
        private void InitData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // проброс уведомлений. если что-то меняется у calculator.initData, то обновить соответствующее поле у DealVM
            RaisePropertyChanged(e.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string prop = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        #region Commands


        // ввод API ключей
        private RelayCommand addAPIKeyCommand;
        public RelayCommand AddAPIKeyCommand
        {
            get {
                return addAPIKeyCommand ??
                      (addAPIKeyCommand = new RelayCommand(obj =>
                      {
                          string str = obj as string;
                          if (str != null)
                          {
                              calculator.initData.KData = str;
                          }
                      }, (obj) => true)); // если false, то кнопка будет неактивна
            }
        }

        public RelayCommand AddAPISecCommand
        {
            get {
                return new RelayCommand((object obj) =>
                {
                    // ввод APISec
                    calculator.initData.SData = (string)obj;
                    MessageBox.Show("API ключи добавлены. Перезапустите программу", "AddAPIKeyCommand", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        public RelayCommand RemoveCommand
        {
            get {
                return new RelayCommand((object obj) =>
                {
                    //if (i.HasValue) calculator.RemoveValue(i.Value);
                    // удаляем API из хранилища
                    calculator.initData.KData = string.Empty;
                    calculator.initData.SData = string.Empty;
                    MessageBox.Show("API ключи удалены. Перезапустите программу", "RemoveAPIkey", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }
        
        #endregion

        //Синхронизация модели и VM //Watch(_trader.TraderDeals, DealsInBotovod, p => p.BotovodDeal);
        // только по количеству элементов в списках, но не по содержанию
        /*private static void Watch<T, T2>(ReadOnlyObservableCollection<T> collToWatch, ObservableCollection<T2> collToUpdate, Func<T2, object> modelProperty)
        {
            ((INotifyCollectionChanged)collToWatch).CollectionChanged += (s, a) =>
            {
                if (a.NewItems?.Count == 1) collToUpdate.Add((T2)Activator.CreateInstance(typeof(T2), (T)a.NewItems[0], null));
                if (a.OldItems?.Count == 1) collToUpdate.Remove(collToUpdate.First(mv => modelProperty(mv) == a.OldItems[0]));
            };
        }*/

        // 
        public void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            //MessageBox.Show("Закрываемся", "ProcessExit", MessageBoxButton.OK, MessageBoxImage.Information);
            //RequestClose();
            BinaryDataStorage.SaveDealsToDisk(_trader.TraderDeals);
        }
    }


}
