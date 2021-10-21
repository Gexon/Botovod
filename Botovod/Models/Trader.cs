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
        private readonly XCommas.Net.XCommasApi _client;

        private readonly InitializedData _initData;

        // тут депозит торгоша
        //public ReadOnlyObservableCollection<CoinStack> TraderCash { get; }
        //private readonly ObservableCollection<CoinStack> _traderCash;
        // тут коллекция сделок торгоша
        public ReadOnlyObservableCollection<BotovodDeal> TraderDeals { get; }
        private readonly ObservableCollection<BotovodDeal> _traderDeals;

        public Trader(InitializedData inInitData)
        {
            _initData = inInitData;
            _client = new XCommas.Net.XCommasApi(_initData.KData, _initData.SData)
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
            var volume = deal.XDeal.BaseOrderVolume / deal.XDeal.CurrentPrice;
            volume *= (deal.XDeal.CompletedManualSafetyOrdersCount + 1); // мартин 2

            deal.OutMessageDeal = $"Покупаю {Math.Round(volume, 5)} торгуемых монет";
            //return true;
            var data = new DealAddFundsParameters
            {
                Quantity = volume,
                IsMarket = true,
                DealId = deal.XDeal.Id,
            };

            var response = await _client.AddFundsToDealAsync(data);
            // отлов ошибок
            if (!string.IsNullOrEmpty(response.Error))
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
            if (string.IsNullOrEmpty(_initData.KData) || string.IsNullOrEmpty(_initData.SData))
            {
                MessageBox.Show("Вбейте API ключи в настройках", "GetDeals", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            //var response = await client.GetDealsAsync(limit: 60, dealScope: DealScope.Active, dealOrder: DealOrder.CreatedAt);
            var response = await _client.GetDealsAsync(dealScope: DealScope.Active);
            // отлов ошибок
            if (!string.IsNullOrEmpty(response.Error))
            {
                MessageBox.Show(response.Error, "GetDeals", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // смотрим есть ли сделки вообще.            
            if ((response.Data == null) || !response.Data.Any()) return true;
            foreach (var xDeal in response.Data)
            {
                string dealStatus;
                switch (xDeal.Status)
                {
                    case DealStatus.Bought:
                        dealStatus = "Активна";
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

                var bDeal = new BotovodDeal(xDeal)
                {
                    OutMessageDeal = dealStatus
                };
                UpdateDeal(bDeal);
            }

            // удаление старых/закрытых сделок
            for (var i = _traderDeals.Count - 1; i >= 0; i--)
            {
                var del = response.Data.All(item =>
                    item.Id != _traderDeals[i]
                        .Id); // короче эту строку предложила idea, не понимаю как это работает. Было все через foreach

                if (!del) continue;
                _traderDeals.RemoveAt(i);
                RaisePropertyChanged();
            }

            return true;
        }

        private void UpdateDeal(BotovodDeal inDeal)
        {
            var exist = false;
            foreach (var deal in _traderDeals)
            {
                // обновляем существующую сделку
                if (deal.Id != inDeal.XDeal.Id) continue;
                deal.XDeal = inDeal.XDeal;
                deal.OutMessageDeal = inDeal.OutMessageDeal;
                deal.CurrentPrice = inDeal.XDeal.CurrentPrice;
                deal.ManualSafetyOrders = inDeal.XDeal.CompletedManualSafetyOrdersCount;
                exist = true;
            }

            // если не обнаружена сделка, создаем новую
            if (exist) return;
            // первое "создание" объекта BotovodDeal. Почему тут а не там, потому что там чаще, тут реже.
            inDeal.MaxSafetyOrders = _initData.MaxSafetyOrders; // при первом создании используем данные для всех.
            inDeal.SafetyOrderStep = _initData.SafetyOrderStep;
            inDeal.TrailingDeviation = _initData.TrailingDeviation;
            inDeal.OutMessageDeal = "Новая сделка"; // хз зачем, пусть пока будет.
            _traderDeals.Add(inDeal);
            RaisePropertyChanged();
        }

        public bool FillingDeals(List<BotovodDeal> loadDeals)
        {
            _traderDeals.Clear();
            foreach (var deal in loadDeals)
            {
                _traderDeals.Add(deal);
            }

            RaisePropertyChanged(nameof(_traderDeals));
            return true;
        }

        [field: NonSerialized] public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string prop = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}