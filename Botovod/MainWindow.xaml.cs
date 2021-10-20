using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using XCommas.Net.Objects;

namespace Botovod
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Имя: test1-wobtest3
    //  API key: a7949da2ca3f4ee9a5675208b4a5e730625f768195f448ae90f45e74f0b30df9
    //  Secret: 3919864b47ef68c4cbc404c79eaac404e676f11f912f8913e2cd164f3c443679620f879eac80368910470cb15a38c3a93436814174ad9a9eb5ac264bb4d6f66f24f6f825dd3910b8107a55395b3da5ba36577dfdcfd2ecc7bb2dbc42e4f8fb66e722a632
    //  Доступ: BotsRead, BotsWrite, AccountsRead, AccountsWrite, SmartTradesRead, SmartTradesWrite
    //  HTTP 429 return code is used when breaking a request rate limit.
    //  HTTP 418 return code is used when an IP has been auto-banned for continuing to send requests after receiving 429 codes.
    //  1. Overall API Rate limit is 15 requests per 1 second per IP.We understand that sometimes you need to update batch of entries or panic sell batch of smart trades or deals so requests to panic_sell and update  has significally higher burst limit.Even 1000requests/second for 5 seconds in a row should be fine
    //  2 Querying deals(/public/api/ver1/deals) is limited to 5 requests per 5 seconds for each account_id.Additional requests is possible if you change scope or order parametres. Remember that you could fetch up to 1000 deals per 1 request.
    //  4. Show deal method (/public/api/ver1/deals/deal_id/show) - 10 requests per minute per each deal_id per IP.
    //  5. Query smart trades V1 (DEPRECATED) ( /public/api/ver1/smart_trades )  - 4 requests per second
    /// </summary>
    public partial class MainWindow : Window
    {
        private XCommas.Net.XCommasApi client;
        private Dictionary<int, Bot> bots = new Dictionary<int, Bot>();
        private Dictionary<int, Deal> deals = new Dictionary<int, Deal>();
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        //int tralingDealId = -1;
        //double trailingMax = 0;
        string trailingPair = string.Empty;
        int accountID = 0;

        public MainWindow()
        {
            InitializeComponent();
            client = new XCommas.Net.XCommasApi("", "");
            client.UserMode = UserMode.Paper;

        }

        private async void GetAccounts(object sender, RoutedEventArgs e)
        {
            // Блокируем кнопку чтоб не натыкали мильен раз
            OutMessage.Text = "Получение списка подключенных бирж";
            BtnGetAccounts.IsEnabled = false;

            var response = await client.GetAccountsAsync();
            // отлов ошибок
            if (!String.IsNullOrEmpty(response.Error))
            {
                OutMessage.Text = response.Error;
                // Деблокирем кнопку
                BtnGetAccounts.IsEnabled = true;
                return;
            }

            if (response.Data != null)
            {
                if (response.Data.Count() == 0)
                {
                    OutMessage.Text = "Нет подключенных бирж";
                }
                else
                {
                    OutMessage.Inlines.Clear();
                    foreach (var account in response.Data)
                    {
                        OutMessage.Inlines.Add(account.Name);
                        accountID = account.Id;
                    }
                }
            }
            else
            {
                OutMessage.Text = response.RawData;
            }

            // Деблокирем кнопку
            BtnGetAccounts.IsEnabled = true;

        }

        private async void GetBots(object sender, RoutedEventArgs e)
        {
            // Блокируем кнопку чтоб не натыкали мильен раз
            OutMessage.Text = "Получение списка Long ботов";
            BtnGetBots.IsEnabled = false;
            CbxBots.Items.Clear();

            var response = await client.GetBotsAsync();
            // отлов ошибок
            if (!String.IsNullOrEmpty(response.Error))
            {
                OutMessage.Text = response.Error;
                // Деблокирем кнопку
                BtnGetBots.IsEnabled = true;
                return;
            }

            if ((response.Data != null) && (response.Data.Count() > 0))
            {
                // зачищаем списки
                OutMessage.Inlines.Clear();
                bots = new Dictionary<int, Bot>();

                foreach (var bot in response.Data)
                {
                    bots.Add(bot.Id, bot);
                    string botStatus = bot.IsEnabled ? "работает" : "отключен";
                    OutMessage.Inlines.Add($"{bot.Name}, Статус {botStatus}");
                }
                // заполняем combobox
                foreach (var item in bots)
                {
                    Bot bot = item.Value;
                    CbxBots.Items.Add(bot.Name);
                    if (CbxBots.Items.Count > 0) { CbxBots.SelectedIndex = 0; }
                }

            }
            else
            {
                OutMessage.Text = response.RawData;
            }

            // Деблокирем кнопку
            BtnGetBots.IsEnabled = true;

        }

        private async void btnStartBot_Click(object sender, RoutedEventArgs e)
        {
            // Блокируем кнопку чтоб не натыкали мильен раз
            OutMessage.Text = "Запуск бота...";
            BtnStartBot.IsEnabled = false;

            if (CbxBots.SelectedValue != null)
            {
                int BotId = (int)CbxBots.SelectedValue;
                bots.TryGetValue(BotId, out Bot bot);
                if (bot != null)
                {
                    if (bot.Id > 0)
                    {
                        OutMessage.Text = $"Запуск бота №{bot.Id}";
                        var response = await client.EnableBotAsync(bot.Id);
                        // отлов ошибок
                        if (!String.IsNullOrEmpty(response.Error))
                        {
                            OutMessage.Text = response.Error;
                        }
                        // Деблокирем кнопку
                        BtnStartBot.IsEnabled = true;
                        return;
                    }

                }
                OutMessage.Text = "Бот не найден";
                // Деблокирем кнопку
                BtnStartBot.IsEnabled = true;
            }

        }

        private async void btnStopBot_Click(object sender, RoutedEventArgs e)
        {
            // Блокируем кнопку чтоб не натыкали мильен раз
            OutMessage.Text = "Останов бота...";
            BtnStopBot.IsEnabled = false;

            if (CbxBots.SelectedValue != null)
            {
                int BotId = (int)CbxBots.SelectedValue;
                bots.TryGetValue(BotId, out Bot bot);
                if (bot != null)
                {
                    if (bot.Id > 0)
                    {
                        OutMessage.Text = $"Останов бота №{bot.Id}";
                        var response = await client.DisableBotAsync(bot.Id);
                        // отлов ошибок
                        if (!String.IsNullOrEmpty(response.Error))
                        {
                            OutMessage.Text = response.Error;
                        }
                        // Деблокирем кнопку
                        BtnStopBot.IsEnabled = true;
                        return;
                    }


                }
                OutMessage.Text = "Бот не найден";
                // Деблокирем кнопку
                BtnStopBot.IsEnabled = true;

            }


        }

        private async void GetDeals(object sender, RoutedEventArgs e)
        {
            // Блокируем кнопку чтоб не натыкали мильен раз
            //OutMessage_Deals.Text = "Получаем список сделок...";
            //btnGetDeals.IsEnabled = false;
            //cbxDeals.Items.Clear();

            //var response = await client.GetDealsAsync(limit: 60, dealScope: DealScope.Active, dealOrder: DealOrder.CreatedAt);
            var response = await client.GetDealsAsync(dealScope: DealScope.Active);
            // отлов ошибок
            if (!String.IsNullOrEmpty(response.Error))
            {
                //OutMessage_Deals.Text = response.Error;
                // Деблокирем кнопку
                //btnGetDeals.IsEnabled = true;
                return;
            }

            // смотрим есть ли сделки вообще.            
            if ((response.Data != null) && (response.Data.Count() > 0))
            {
                // зачищаем списки
                //OutMessage_Deals.Inlines.Clear();
                deals = new Dictionary<int, Deal>();

                foreach (var deal in response.Data)
                {
                    deals.Add(deal.Id, deal);
                    string dealStatus = string.Empty;
                    switch (deal.Status)
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
                            dealStatus = $"{deal.Status}";
                            break;
                    }
                    //OutMessage_Deals.Inlines.Add($"№{deal.Id}, Имя бота:{deal.BotName}. Статус {dealStatus}\n");
                }
                // заполняем combobox
                foreach (var item in deals)
                {
                    Deal deal = item.Value;
                    //cbxDeals.Items.Add(deal.Id);
                    //if (cbxDeals.Items.Count > 0) { cbxDeals.SelectedIndex = 0; }
                }

            }
            else
            {
                //OutMessage_Deals.Text = response.RawData;
            }

            // Деблокирем кнопку
            //btnGetDeals.IsEnabled = true;

        }

        private void btnCancelDeal_Click(object sender, RoutedEventArgs e)
        {
            // Блокируем кнопку чтоб не натыкали мильен раз
            //OutMessage_Deals.Text = "Отменяю сделку...";
            //btnCancelDeal.IsEnabled = false;

            //if (cbxDeals.SelectedValue != null)
            {
                //int dealId = (int)cbxDeals.SelectedValue;
                //deals.TryGetValue(dealId, out Deal deal);
                //if (deal != null)
                {
                    //if (deal.Id > 0)
                    {
                        //OutMessage_Deals.Text = $"Отмена сделки №{deal.Id}";
                        //var response = await client.CancelDealAsync(dealId);
                        // отлов ошибок
                        //if (!String.IsNullOrEmpty(response.Error))
                        {
                            // OutMessage_Deals.Text = response.Error;
                        }
                        // Деблокирем кнопку
                        //btnCancelDeal.IsEnabled = true;
                        return;
                    }


                }
                //OutMessage_Deals.Text = "Сделка не найдена";
                // Деблокирем кнопку
                //btnCancelDeal.IsEnabled = true;

            }



        }

        private void btnAddFunds_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void btnCreateBot_Click(object sender, RoutedEventArgs e)
        {
            //These settings are for demonstration purposes ONLY!
            var data = new BotData
            {
                Pairs = new[] { "BTC_ETH", "BTC_KMD", "BTC_ENG" },
                ActiveSafetyOrdersCount = 1,
                BaseOrderVolume = 0.007m,
                SafetyOrderVolume = 0.014m,
                MartingaleStepCoefficient = 1.2m,
                MartingaleVolumeCoefficient = 1.1m,
                MinVolumeBtc24h = 75m,
                MaxActiveDeals = 6,
                ProfitCurrency = ProfitCurrency.QuoteCurrency,
                SafetyOrderStepPercentage = 2.5m,
                MaxSafetyOrders = 2,
                TakeProfitType = TakeProfitType.Total,
                TakeProfit = 3.5m,
                Name = "My new bot",
                TrailingEnabled = true,
                TrailingDeviation = 1.0m,
                StartOrderType = StartOrderType.Limit,
                Strategies = new BotStrategy[]
                {
                    new QflBotStrategy
                    {
                        Options = new QflOptions{ Percent = 3, Type = QflType.Original },
                    },
                    new TradingViewBotStrategy
                    {
                        Options = new TradingViewOptions { Time = TradingViewTime.OneHour, Type = TradingViewIndicatorType.StrongBuy }
                    },
                    new RsiBotStrategy
                    {
                        Options = new RsiOptions { Time = IndicatorTime.FiveMinutes, Points = 17 }
                    },
                    new CqsTelegramBotStrategy
                    {

                    },
                    new TaPresetsBotStrategy
                    {
                        Options = new TaPresetsOptions { Time = IndicatorTime.ThirtyMinutes, Type = TaPresetsType.MFI_14_20 }
                    },
                    new UltBotStrategy
                    {
                        Options = new UltOptions { Time = IndicatorTime.TwoHours, Points = 60 }
                    }
                }.ToList(),
                StopLossPercentage = 10m,
            };

            var response = await client.CreateBotAsync(accountID, Strategy.Long, data);
            // отлов ошибок
            if (!String.IsNullOrEmpty(response.Error))
            {
                OutMessage.Text = response.Error;
                // Деблокирем кнопку
                // btnGetAccounts.IsEnabled = true;
                return;
            }
        }
    }
}
