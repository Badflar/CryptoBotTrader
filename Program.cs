using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoinbasePro;
using CoinbasePro.Network.Authentication;
using CoinbasePro.Shared.Types;
using Newtonsoft.Json;


namespace CryptoTradeBot
{
    class Program
    {
        // Interval in seconds   [secs] * [milli to seconds]
        public static int interval = 30 * 1000;

        // Object of keys from Coinbase Pro API
        public static KEYS Keys;

        // Status holders
        public bool isNextOperationBuy = true;
        public bool isCurrentBTC = false; // Dictates whether money is in ETH or BTC


        // Buying thresholds 
        public double DIP_THREHOLD = -2.25;
        public double UPWARD_TREND_THRESHOLD = 1.5;

        // Selling thesholds
        public double PROFIT_THRESHOLD = 1.25;
        public double STOP_LOSS_THRESHOLD = -2.00;

        // Price from currency from previous operation
        public double lastOpPriceBTC = 11805.73;
        public double lastOpPriceETH = 376.24;


        static async Task Main(string[] args)
        {
            // Call non-static methods
            Program program = new Program();

            // Convert JSON to KEYS object
            using (StreamReader r = new StreamReader(@"E:\_BlueFoxDen\Programs\CryptoTradeBot\CryptoTradeBot\Keys.json"))
            {
                string json = r.ReadToEnd();
                Keys = JsonConvert.DeserializeObject<KEYS>(json);
            }

            // Create authenticator
            var authenticator = new Authenticator(Keys.apikey, Keys.apisecret, Keys.apiphrase);

            // Start coinbase client
            var coinbaseProClient = new CoinbaseProClient(authenticator);


            // Used to get IDs of profiles and get balances for debugging
            //var allAccounts = await coinbaseProClient.AccountsService.GetAllAccountsAsync();
            //foreach(var account in allAccounts)
            //Console.WriteLine(account.Id + " " + account.Currency + " " + account.Balance);


            // Main loop
            while (true)
            {
                await program.MakeTrade(coinbaseProClient);
                Thread.Sleep(interval);
            }
        }
        
        private async Task MakeTrade(CoinbaseProClient coinbaseProClient)
        {
            // Get percent differences and current prices of currencies
            var currentTickerBTC = await coinbaseProClient.ProductsService.GetProductTickerAsync(ProductType.BtcUsd);
            double currentPriceBTC = (double)currentTickerBTC.Price;
            var percentDiffBTC = (currentPriceBTC - lastOpPriceBTC) / lastOpPriceBTC * 100;

            var currentTickerETH = await coinbaseProClient.ProductsService.GetProductTickerAsync(ProductType.EthUsd);
            double currentPriceETH = (double)currentTickerETH.Price;
            var percentDiffETH = (currentPriceETH - lastOpPriceETH) / lastOpPriceETH * 100;

            // Check if next action is to buy
            if (isNextOperationBuy == true) 
            {
                // TODO - Check both percent differences and pick the "better outcome" percent
                // Check if BTC percent difference is either greater than upward trend (spike upwards) or if percent difference is less than dip (negative number) 
                if (percentDiffBTC >= UPWARD_TREND_THRESHOLD || percentDiffBTC <= DIP_THREHOLD)
                {
                    var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                    Console.WriteLine(USDAccount.Balance);
                    await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Buy, ProductType.BtcUsd, Math.Round(USDAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                    lastOpPriceBTC = currentPriceBTC;
                    var BTCAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("4fb67e84-e6e9-46fd-bac4-ea03dc242c61");
                    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Buy\n[Price] = " + currentPriceBTC.ToString() + "\n[BTC Avaliable] = " + BTCAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Balance.ToString() + "\n[Percent Difference] = " + percentDiffBTC.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                    isNextOperationBuy = false;
                    isCurrentBTC = true;
                }
                // Check if ETH percent difference is either greater than upward trend (spike upwards) or if percent difference is less than dip (negative number) 
                else if (percentDiffETH >= UPWARD_TREND_THRESHOLD || percentDiffETH <= DIP_THREHOLD)
                {
                    var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                    Console.WriteLine(USDAccount.Balance);
                    await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Buy, ProductType.EthUsd, Math.Round(USDAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                    lastOpPriceETH = currentPriceETH;
                    var ETHAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("da8129ff-84e3-4612-8ec7-40eeee6c55ad");
                    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Buy\n[Price] = " + currentPriceETH.ToString() + "\n[ETH Avaliable] = " + ETHAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Balance.ToString() + "\n[Percent Difference] = " + percentDiffETH.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                    isNextOperationBuy = false;
                    isCurrentBTC = false;
                }

                //Console.WriteLine("[Percent Difference] = " + percentDiff.ToString() + " (No Action)");
            }
            else if (isNextOperationBuy == false)
            {
                // If money is put into BTC...
                if (isCurrentBTC == true)
                {
                    // And if current BTC price is going up or going down drastically...
                    if (percentDiffBTC >= PROFIT_THRESHOLD || percentDiffBTC <= STOP_LOSS_THRESHOLD)
                    {
                        // Then sell
                        var BTCAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("4fb67e84-e6e9-46fd-bac4-ea03dc242c61");
                        Console.WriteLine(BTCAccount.Balance);
                        var trade = await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Sell, ProductType.BtcUsd, Math.Round(BTCAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                        lastOpPriceBTC = currentPriceBTC;
                        var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                        Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Sell\n[Price] = " + currentPriceBTC.ToString() + "\n[BTC Avaliable] = " + BTCAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Available.ToString() + "\n[Percent Difference] = " + percentDiffBTC.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                        isNextOperationBuy = true;
                    }
                }
                // Else if money is put into ETH...
                else if (isCurrentBTC == false)
                {
                    // And if current ETH price is going up or going down drastically...
                    if (percentDiffETH >= PROFIT_THRESHOLD || percentDiffETH <= STOP_LOSS_THRESHOLD)
                    {
                        // Then sell
                        var ETHAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("da8129ff-84e3-4612-8ec7-40eeee6c55ad");
                        Console.WriteLine(ETHAccount.Balance);
                        var trade = await coinbaseProClient.OrdersService.PlaceMarketOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Sell, ProductType.EthUsd, Math.Round(ETHAccount.Balance, 2), CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size);
                        lastOpPriceETH = currentPriceETH;
                        var USDAccount = await coinbaseProClient.AccountsService.GetAccountByIdAsync("a8a83415-dc6c-4327-bf84-b3eb9859d81d");
                        Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + "[Operation] = Sell\n[Price] = " + currentPriceETH.ToString() + "\n[ETH Avaliable] = " + ETHAccount.Balance.ToString() + "\n[USD Avaliable] = " + USDAccount.Available.ToString() + "\n[Percent Difference] = " + percentDiffETH.ToString() + "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                        isNextOperationBuy = true;
                    }
                }

                //Console.WriteLine("[Percent Difference] = " + percentDiff.ToString() + " (No Action)");
            }
        }
    }
}
