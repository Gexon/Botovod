using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using XCommas.Net.Objects;

namespace Botovod.Models
{
    public class Trader : BindableBase
    {
        internal XCommas.Net.XCommasApi client;
        InitializedData initData;
        int maxSafetyOrders = 5;
        // тут депозит торгоша
        public ReadOnlyObservableCollection<CoinStack> TraderCash { get; }
        private readonly ObservableCollection<CoinStack> _traderCash;
        // тут коллекция сделок торгоша
        public ReadOnlyObservableCollection<BotovodDeal> TraderDeals { get; }
        private readonly ObservableCollection<BotovodDeal> _traderDeals;

        public Trader(InitializedData inInitData)
        {
            initData = inInitData;
            client = new XCommas.Net.XCommasApi(initData.GetKData, initData.GetSData)
            {
                UserMode = UserMode.Paper
            };

            // сделки торговца
            _traderDeals = new ObservableCollection<BotovodDeal>();
            TraderDeals = new ReadOnlyObservableCollection<BotovodDeal>(_traderDeals);
            // загружаем сделки с сервиса 3коммас
            //GetDeals(); // вызывала ошибку "System.InvalidOperationException: "Объект ItemsControl не соответствует своему источнику элементов.\n Дополнительные сведения см. в описании внутреннего исключения."
            //"System.Diagnostics.PresentationTraceSources.SetTraceLevel(myItemsControl.ItemContainerGenerator, System.Diagnostics.PresentationTraceLevel.High)"
        }

        internal async Task<bool> AddFunds(BotovodDeal deal)
        {
            // Усреднение. volume - это сколько торгуемой валюты купить на битки например.
            decimal volume = deal.xDeal.BaseOrderVolume / deal.xDeal.CurrentPrice;
            volume = volume * (deal.xDeal.CompletedManualSafetyOrdersCount + 1); // мартин 2
            // ограничить количество СО?
            if (deal.xDeal.CompletedManualSafetyOrdersCount >= maxSafetyOrders)
            {
                deal.OutMessage_Deal = $"Максимальное количество СО: {maxSafetyOrders}";
                return false;
            }
            deal.OutMessage_Deal = $"Покупаю {Math.Round(volume, 5)} торгуемых монет";
            //return true;
            var data = new DealAddFundsParameters
            {
                Quantity = volume,
                IsMarket = true,
                DealId = deal.xDeal.Id,
            };

            var response = await client.AddFundsToDealAsync(data);
            // отлов ошибок
            if (!String.IsNullOrEmpty(response.Error))
            {
                deal.OutMessage_Deal = response.Error;
                MessageBox.Show(response.Error, "AddFunds", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            else
            {
                deal.OutMessage_Deal = $"Усреднение сделки №{deal.xDeal.Id} выполнено.";
                return true;
            }

        }

        internal async Task<bool> GetDeals()
        {
            if (string.IsNullOrEmpty(initData.GetKData) || string.IsNullOrEmpty(initData.GetSData))
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
                    bDeal.OutMessage_Deal = dealStatus;
                    UpdateDeal(bDeal);
                }
                // удаление старых/закрытых сделок
                for (int i = _traderDeals.Count - 1; i >= 0; i--)
                {
                    bool del = true;
                    foreach (var item in response.Data)
                    {
                        if (item.Id == _traderDeals[i].xDeal.Id) { 
                            del = false;
                            continue;
                        }
                    }
                    if (del) { _traderDeals.RemoveAt(i); }
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
                if (deal.xDeal.Id == inDeal.xDeal.Id)
                {
                    deal.xDeal = inDeal.xDeal;
                    deal.OutMessage_Deal = inDeal.OutMessage_Deal;
                    deal.CurrentPrice = inDeal.xDeal.CurrentPrice;
                    exist = true;
                }
            }
            // если не обнаружена сделка, создаем новую
            if (!exist)
            {
                _traderDeals.Add(inDeal);
            }


        }
    }
}
