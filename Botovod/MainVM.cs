using Botovod.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace Botovod
{
    // Основная прокладка. Создает калькулятор из модели.
    class MainVM : BindableBase
    {
        private Calculator calculator;
        private Trader _trader;
        // список сделок для отображения в интерфейсе
        public ObservableCollection<DealVM> DealsInBotovod { get; }

        public MainVM(Calculator inCalculator)
        {
            calculator = inCalculator;
            _trader = calculator.Trader;
            //таким нехитрым способом мы пробрасываем изменившиеся свойства модели во View
            //_trader.PropertyChanged += (s, e) => { RaisePropertyChanged(e.PropertyName); };
            //_trader.PropertyChanged += (s, e) => { RaisePropertyChanged(nameof(DealsInBotovod)); };

            // ввод API ключей
            AddAPIKeyCommand = new DelegateCommand<string>(str =>
            {
                // ввод APIKey
                calculator.initData.SetKData(str);
                // проверка корректности ввода числа
                //int ival;
                //if (int.TryParse(str, out ival)) calculator.AddValue(ival);
            });
            AddAPISecCommand = new DelegateCommand<string>(str =>
            {
                // ввод APISec
                calculator.initData.SetSData(str);
                MessageBox.Show("API ключи добавлены. Перезапустите программу", "AddAPIKeyCommand", MessageBoxButton.OK, MessageBoxImage.Warning);
                // проверка корректности ввода числа
                //int ival;
                //if (int.TryParse(str, out ival)) calculator.AddValue(ival);
            });

            RemoveCommand = new DelegateCommand<int?>(i =>
            {
                //if (i.HasValue) calculator.RemoveValue(i.Value);
                // удаляем API из хранилища
                calculator.initData.SetKData("");
                calculator.initData.SetSData("");
                MessageBox.Show("API ключи удалены. Перезапустите программу", "RemoveAPIkey", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
            //--------------
            // сделки торговца
            //DealsInBotovod = new ObservableCollection<DealVM>(_trader.TraderDeals.Select(ap => new DealVM(ap, calculator)));
            DealsInBotovod = new ObservableCollection<DealVM>();
            //Watch(_trader.TraderDeals, DealsInBotovod, p => p.BotovodDeal);

            //-------ex-------            
            //преобразовывать каждый добавленный или удаленный элемент из модели
            ((INotifyCollectionChanged)_trader.TraderDeals).CollectionChanged += (s, a) =>
            {
                //if (a.NewItems?.Count == 1) DealsInBotovod.Add(new DealVM(a.NewItems[0] as BotovodDeal));
                if (a.NewItems?.Count >= 1) { foreach (BotovodDeal deal in a.NewItems) { DealsInBotovod.Add(new DealVM(deal)); } }

                //if (a.OldItems?.Count == 1) DealsInBotovod.Remove(DealsInBotovod.First(mv => mv.BotovodDeal == a.OldItems[0]));
                if (a.OldItems?.Count >= 1) { foreach (BotovodDeal deal in a.OldItems) { DealsInBotovod.Remove(DealsInBotovod.First(mv => mv.BotovodDeal == deal)); } }

            };

            //-------------
        }

        //------------------old-------------
        public DelegateCommand<string> AddAPIKeyCommand { get; }
        public DelegateCommand<string> AddAPISecCommand { get; }
        public DelegateCommand<int?> RemoveCommand { get; }
        public int Sum => calculator.Sum;
        public ReadOnlyObservableCollection<int> MyValues => calculator.MyPublicValues;

        private int _number1;
        public int Number1
        {
            get { return _number1; }
            set {
                _number1 = value;
                RaisePropertyChanged("Number3");    // уведомление View о том, что изменилась сумма
            }
        }

        private int _number2;
        public int Number2
        {
            get { return _number2; }
            set {
                _number2 = value;
                RaisePropertyChanged("Number3");
            }
        }

        //свойство только для чтения, оно считывается View каждый раз, когда обновляется Number1 или Number2
        public int Number3 => Calculator.GetSumOf(Number1, Number2);
        //--------------


        //Синхронизация модели и VM //Watch(_trader.TraderDeals, DealsInBotovod, p => p.BotovodDeal);
        // только по количеству элементов в списках, но не по содержанию
        private static void Watch<T, T2>(ReadOnlyObservableCollection<T> collToWatch, ObservableCollection<T2> collToUpdate, Func<T2, object> modelProperty)
        {
            ((INotifyCollectionChanged)collToWatch).CollectionChanged += (s, a) =>
            {
                if (a.NewItems?.Count == 1) collToUpdate.Add((T2)Activator.CreateInstance(typeof(T2), (T)a.NewItems[0], null));
                if (a.OldItems?.Count == 1) collToUpdate.Remove(collToUpdate.First(mv => modelProperty(mv) == a.OldItems[0]));
            };
        }
    }


}
