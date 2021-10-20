using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using XCommas.Net.Objects;

namespace Botovod.Models
{
    public class Trader : INotifyPropertyChanged
    {
        internal XCommas.Net.XCommasApi client;
        InitializedData initData;
        // тут депозит торгоша
        //public ReadOnlyObservableCollection<CoinStack> TraderCash { get; }
        //private readonly ObservableCollection<CoinStack> _traderCash;
        // тут коллекция сделок торгоша
        public ReadOnlyObservableCollection<BotovodDeal> TraderDeals { get; }
        private readonly ObservableCollection<BotovodDeal> _traderDeals;

        public Trader(InitializedData inInitData)
        {
            initData = inInitData;
            client = new XCommas.Net.XCommasApi(initData.KData, initData.SData)
            {
                UserMode = UserMode.Paper
            };

            // сделки торговца
            _traderDeals = new ObservableCollection<BotovodDeal>();
            TraderDeals = new ReadOnlyObservableCollection<BotovodDeal>(_traderDeals);
        }

        internal async Task<bool> AddFunds(BotovodDeal deal)
        {
            // Усреднение. volume - это сколько торгуемой валюты купить на битки например.
            decimal volume = deal.XDeal.BaseOrderVolume / deal.XDeal.CurrentPrice;
            volume = volume * (deal.XDeal.CompletedManualSafetyOrdersCount + 1); // мартин 2

            deal.OutMessageDeal = $"Покупаю {Math.Round(volume, 5)} торгуемых монет";
            //return true;
            var data = new DealAddFundsParameters
            {
                Quantity = volume,
                IsMarket = true,
                DealId = deal.XDeal.Id,
            };

            var response = await client.AddFundsToDealAsync(data);
            // отлов ошибок
            if (!String.IsNullOrEmpty(response.Error))
            {
                deal.OutMessageDeal = response.Error;
                MessageBox.Show(response.Error, "AddFunds", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            else
            {
                deal.OutMessageDeal = $"Усреднение сделки №{deal.XDeal.Id} выполнено.";
                return true;
            }

        }

        internal async Task<bool> GetDeals()
        {
            if (string.IsNullOrEmpty(initData.KData) || string.IsNullOrEmpty(initData.SData))
            {
                MessageBox.Show("Вбейте API ключи в настройках", "GetDeals", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            //var response = await client.GetDealsAsync(limit: 60, dealScope: DealScope.Active, dealOrder: DealOrder.CreatedAt);
            var response = await client.GetDealsAsync(dealScope: DealScope.Active);
            // отлов ошибок
            if (!String.IsNullOrEmpty(response.Error))
            {
                MessageBox.Show(response.Error, "GetDeals", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // смотрим есть ли сделки вообще.            
            if ((response.Data != null) && (response.Data.Count() > 0))
            {
                foreach (var xDeal in response.Data)
                {
                    string dealStatus;
                    switch (xDeal.Status)
                    {
                        case DealStatus.Bought:
                            dealStatus = "Закупился";
                            break;
                        case DealStatus.Cancelled:
                            dealStatus = "Отменен";
                            break;
                        case DealStatus.Failed:
                            dealStatus = "Ошибка";
                            break;
                        default:
                            dealStatus = $"{xDeal.Status}";
                            break;
                    }
                    BotovodDeal bDeal = new BotovodDeal(xDeal);
                    bDeal.OutMessageDeal = dealStatus;
                    UpdateDeal(bDeal);
                }
                // удаление старых/закрытых сделок
                for (int i = _traderDeals.Count - 1; i >= 0; i--)
                {
                    bool del = true;
                    foreach (var item in response.Data)
                    {
                        if (item.Id == _traderDeals[i].XDeal.Id)
                        {
                            del = false;
                            break;
                        }
                    }
                    if (del)
                    {
                        _traderDeals.RemoveAt(i);
                        RaisePropertyChanged();
                    }
                }

            }
            return true;
        }

        private void UpdateDeal(BotovodDeal inDeal)
        {
            bool exist = false;
            foreach (BotovodDeal deal in _traderDeals)
            {
                // обновляем существующую сделку
                if (deal.XDeal.Id == inDeal.XDeal.Id)
                {
                    deal.XDeal = inDeal.XDeal;
                    deal.OutMessageDeal = inDeal.OutMessageDeal;
                    deal.CurrentPrice = inDeal.XDeal.CurrentPrice;
                    exist = true;
                }
            }
            // если не обнаружена сделка, создаем новую
            if (!exist)
            {
                // первое "создание" объекта BotovodDeal
                inDeal.MaxSafetyOrders = initData.MaxSafetyOrders;    // при первом создании используем данные для всех.
                inDeal.SafetyOrderStep = initData.SafetyOrderStep;
                inDeal.TrailingDeviation = initData.TrailingDeviation;
                _traderDeals.Add(inDeal);
                RaisePropertyChanged();
            }

        }

        public bool FillingDeals(List<BotovodDeal> loadDeals)
        {
            _traderDeals.Clear();
            foreach (BotovodDeal deal in loadDeals)
            {
                _traderDeals.Add(deal);
            }
            RaisePropertyChanged(nameof(_traderDeals));
            return true;
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string prop = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
