using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using XCommas.Net.Objects;

namespace Botovod.Models
{
    public class Trader : INotifyPropertyChanged
    {
        private readonly XCommas.Net.XCommasApi _client;

        private readonly InitializedData _initData;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
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
                UserMode = UserMode.Real
            };

            // сделки торговца
            _traderDeals = new ObservableCollection<BotovodDeal>();
            TraderDeals = new ReadOnlyObservableCollection<BotovodDeal>(_traderDeals);

            // инициализация логгера
            var config = new NLog.Config.LoggingConfiguration();
            // Targets where to log to: File and Console
            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var logfile = new NLog.Targets.FileTarget("trader_deals") { FileName = appDir + "\\trader_deals.log" };
            //var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            // Rules for mapping loggers to targets            
            //config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            // Apply config           
            LogManager.Configuration = config;
        }

        internal async Task<bool> AddFunds(BotovodDeal deal)
        {
            // Усреднение. volume - это сколько торгуемой валюты купить на битки например.
            var volume = deal.XDeal.BaseOrderVolume / deal.XDeal.CurrentPrice;
            var manualSO = deal.XDeal.CompletedManualSafetyOrdersCount; 
            //volume *= (manualSO+ 1); // мартин 2
            volume *= (decimal)Math.Pow(2, manualSO); // мартин 2 (SO1=x1, SO2=x2, SO3=x4)
            deal.OutMessageDeal = $"Покупаю {Math.Round(volume, 5)} торгуемых монет";
            Log.Info(deal.OutMessageDeal+$". Выполнено усреднений: {manualSO}");
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
                Log.Error($"AddFunds. Ошибка усреднения: {response.Error}");
                MessageBox.Show(response.Error, "AddFunds", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            else
            {
                deal.OutMessageDeal = $"Усреднение сделки №{deal.XDeal.Id} выполнено.";
                Log.Info(deal.OutMessageDeal);
                return true;
            }
        }

        internal async Task<bool> GetDeals()
        {
            if (string.IsNullOrEmpty(_initData.KData) || string.IsNullOrEmpty(_initData.SData))
            {
                Log.Error("GetDeals. API ключи не найдены");
                MessageBox.Show("Вбейте API ключи в настройках", "GetDeals", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            //var response = await client.GetDealsAsync(limit: 60, dealScope: DealScope.Active, dealOrder: DealOrder.CreatedAt);
            var response = await _client.GetDealsAsync(dealScope: DealScope.Active);
            // отлов ошибок
            if (!string.IsNullOrEmpty(response.Error))
            {
                Log.Error($"GetDeals. Ошибка получения сделок: {response.Error}");
                MessageBox.Show(response.Error, "GetDeals", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // смотрим есть ли сделки вообще.            
            //if ((response.Data == null) || !response.Data.Any()) return true;
            if (response.Data == null) return true;
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
            Log.Info($"Новая сделка: {inDeal.Id}");
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