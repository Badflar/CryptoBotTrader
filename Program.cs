using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coinbase.Pro.Models;
using CoinbasePro;
using CoinbasePro.Network.Authentication;
using CoinbasePro.Shared.Types;
using CoinbasePro.WebSocket.Types;
using Flurl.Util;
using Newtonsoft.Json;


namespace CryptoTradeBot
{
    class Program
    {
        public static int interval = 30 * 1000;

        public static KEYS Keys;

        public bool isNextOperationBuy = true;

        public double DIP_THREHOLD = -2.25;
        public double UPWARD_TREND_THRESHOLD = 1.5;

        public double PROFIT_THRESHOLD = 1.25;
        public double STOP_LOSS_THRESHOLD = -2.00;

        public double lastOpPrice = 11805.73;


        static async Task Main(string[] args)
        {
            Program program = new Program();
            var productTypes = new List<ProductType>() { ProductType.BtcUsd };
            var channels = new List<ChannelType>() { ChannelType.Full, ChannelType.User };



            using (StreamReader r = new StreamReader(@"E:\_BlueFoxDen\Programs\CryptoTradeBot\CryptoTradeBot\Keys.json"))
            {
                string json = r.ReadToEnd();
                Keys = JsonConvert.DeserializeObject<KEYS>(json);
            }

            var authenticator = new Authenticator(Keys.apikey, Keys.apisecret, Keys.apiphrase);

            var coinbaseProClient = new CoinbasePro.CoinbaseProClient(authenticator);

            while (true)
            {
                await program.MakeTrade(coinbaseProClient);
                Thread.Sleep(interval);
            }
        }
        
        private async Task MakeTrade(CoinbaseProClient coinbaseProClient)
        {
            var currentTicker = await coinbaseProClient.ProductsService.GetProductTickerAsync(ProductType.BtcUsd);
            double currentPrice = (double) currentTicker.Price;
            var percentDiff = (currentPrice - lastOpPrice) / lastOpPrice * 100;
            if (isNextOperationBuy == true) 
            {
                if (percentDiff >= UPWARD_TREND_THRESHOLD || percentDiff <= DIP_THREHOLD)
                {
                    var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                    Console.WriteLine(USDAccount.Balance);
                    var trade = await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Buy, ProductType.BtcUsd, Math.Round(USDAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                    lastOpPrice = currentPrice;
                    var BTCAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("4fb67e84-e6e9-46fd-bac4-ea03dc242c61");
                    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Buy\n[Price] = " + currentPrice.ToString() + "\n[BTC Avaliable] = " + BTCAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Balance.ToString() + "[Percent Difference] = " + percentDiff.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                    isNextOperationBuy = false;
                }

                //Console.WriteLine("[Percent Difference] = " + percentDiff.ToString() + " (No Action)");
            }else if(isNextOperationBuy == false)
            {
                if (percentDiff >= PROFIT_THRESHOLD || percentDiff <= STOP_LOSS_THRESHOLD)
                {
                    var BTCAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("4fb67e84-e6e9-46fd-bac4-ea03dc242c61");
                    Console.WriteLine(BTCAccount.Balance);
                    var trade = await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Sell, ProductType.BtcUsd, Math.Round(BTCAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                    lastOpPrice = currentPrice;
                    var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Sell\n[Price] = " + currentPrice.ToString() + "\n[BTC Avaliable] = " + BTCAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Available.ToString() + "[Percent Difference] = " + percentDiff.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                    isNextOperationBuy = true;
                }

                //Console.WriteLine("[Percent Difference] = " + percentDiff.ToString() + " (No Action)");
            }
        }
    }
}
