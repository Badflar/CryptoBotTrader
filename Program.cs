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
        public bool isCurrentBTC = true;

        public double DIP_THREHOLD = -2.25;
        public double UPWARD_TREND_THRESHOLD = 1.5;

        public double PROFIT_THRESHOLD = 1.25;
        public double STOP_LOSS_THRESHOLD = -2.00;

        public double lastOpPriceBTC = 11805.73;
        public double lastOpPriceETH = 396.57;


        static async Task Main(string[] args)
        {
            Program program = new Program();

            using (StreamReader r = new StreamReader(@"E:\_BlueFoxDen\Programs\CryptoTradeBot\CryptoTradeBot\Keys.json"))
            {
                string json = r.ReadToEnd();
                Keys = JsonConvert.DeserializeObject<KEYS>(json);
            }

            var authenticator = new Authenticator(Keys.apikey, Keys.apisecret, Keys.apiphrase);

            var coinbaseProClient = new CoinbaseProClient(authenticator);

            var allAccounts = await coinbaseProClient.AccountsService.GetAllAccountsAsync();

            //foreach(var account in allAccounts)
            //Console.WriteLine(account.Id + " " + account.Currency + " " + account.Balance);

            while (true)
            {
                await program.MakeTrade(coinbaseProClient);
                Thread.Sleep(interval);
            }
        }
        
        private async Task MakeTrade(CoinbaseProClient coinbaseProClient)
        {
            var currentTickerBTC = await coinbaseProClient.ProductsService.GetProductTickerAsync(ProductType.BtcUsd);
            double currentPriceBTC = (double)currentTickerBTC.Price;
            var percentDiffBTC = (currentPriceBTC - lastOpPriceBTC) / lastOpPriceBTC * 100;

            var currentTickerETH = await coinbaseProClient.ProductsService.GetProductTickerAsync(ProductType.EthUsd);
            double currentPriceETH = (double)currentTickerETH.Price;
            var percentDiffETH = (currentPriceETH - lastOpPriceETH) / lastOpPriceETH * 100;

            if (isNextOperationBuy == true) 
            {
                if (percentDiffBTC >= UPWARD_TREND_THRESHOLD || percentDiffBTC <= DIP_THREHOLD)
                {
                    var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                    Console.WriteLine(USDAccount.Balance);
                    await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Buy, ProductType.BtcUsd, Math.Round(USDAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                    lastOpPriceBTC = currentPriceBTC;
                    var BTCAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("4fb67e84-e6e9-46fd-bac4-ea03dc242c61");
                    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Buy\n[Price] = " + currentPriceBTC.ToString() + "\n[BTC Avaliable] = " + BTCAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Balance.ToString() + "[Percent Difference] = " + percentDiffBTC.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                    isNextOperationBuy = false;
                    isCurrentBTC = true;
                }
                else if (percentDiffETH >= UPWARD_TREND_THRESHOLD || percentDiffETH <= DIP_THREHOLD)
                {
                    var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                    Console.WriteLine(USDAccount.Balance);
                    await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Buy, ProductType.EthUsd, Math.Round(USDAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                    lastOpPriceETH = currentPriceETH;
                    var ETHAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("e67d239b-1ca8-4eaa-a288-3de872439a81");
                    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Buy\n[Price] = " + currentPriceETH.ToString() + "\n[ETH Avaliable] = " + ETHAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Balance.ToString() + "[Percent Difference] = " + percentDiffETH.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                    isNextOperationBuy = false;
                    isCurrentBTC = false;
                }

                //Console.WriteLine("[Percent Difference] = " + percentDiff.ToString() + " (No Action)");
            }
            else if (isNextOperationBuy == false)
            {
                if (isCurrentBTC == true)
                {
                    if (percentDiffBTC >= PROFIT_THRESHOLD || percentDiffBTC <= STOP_LOSS_THRESHOLD)
                    {
                        var BTCAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("4fb67e84-e6e9-46fd-bac4-ea03dc242c61");
                        Console.WriteLine(BTCAccount.Balance);
                        var trade = await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Sell, ProductType.BtcUsd, Math.Round(BTCAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                        lastOpPriceBTC = currentPriceBTC;
                        var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                        Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Sell\n[Price] = " + currentPriceBTC.ToString() + "\n[BTC Avaliable] = " + BTCAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Available.ToString() + "[Percent Difference] = " + percentDiffBTC.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                        isNextOperationBuy = true;
                    }
                }
                else if (isCurrentBTC == false)
                {
                    if (percentDiffETH >= PROFIT_THRESHOLD || percentDiffETH <= STOP_LOSS_THRESHOLD)
                    {
                        var ETHAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("e67d239b-1ca8-4eaa-a288-3de872439a81");
                        Console.WriteLine(ETHAccount.Balance);
                        var trade = await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Sell, ProductType.BtcUsd, Math.Round(ETHAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                        lastOpPriceETH = currentPriceETH;
                        var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                        Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Sell\n[Price] = " + currentPriceETH.ToString() + "\n[ETH Avaliable] = " + ETHAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Available.ToString() + "[Percent Difference] = " + percentDiffETH.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                        isNextOperationBuy = true;
                    }
                }

                //Console.WriteLine("[Percent Difference] = " + percentDiff.ToString() + " (No Action)");
            }
        }
    }
}
