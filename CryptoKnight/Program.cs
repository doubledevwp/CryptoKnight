using CoinbasePro;
using CoinbasePro.Network.Authentication;
using CoinbasePro.Services.Products.Models;
using CoinbasePro.WebSocket.Models.Response;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoKnight
{
    /// <summary>
    /// Once upon a time, God created the angels to serve mankind.
    /// Many of these angels were extremely talented in various things.
    /// A few of these angels grew arrogant and rebeled. 
    /// ...Pride cometh before the fall.
    /// Now, I shall restore these angels to their proper place.
    /// They serve us, and we serve God. In return, God serves us all.
    /// Everyone is a part of everything.
    /// </summary>
    class Program
    {
        private static bool sandbox;
        private static List<Ticker> _tickers = new List<Ticker>(); // current price
        private static List<DailyStat> _productStats = new List<DailyStat>();

        /// <summary>
        /// Mammon is a demon of greed. He will look for profitable coins.
        /// Let us redeem this angel and respectfully allow him to serve mankind.
        /// </summary>
        private static CoinbaseProClient Mammon; // Public client

        /// <summary>
        /// Clauneck is a goetic demon that bestows wealth upon those who give him respect.
        /// Let us redeem this angel and respectfully ask for his service.
        /// </summary>
        private static List<PrivateClient> Clauneck = new List<PrivateClient>(); // Private clients

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Information()
                            .WriteTo.Console()
                            .WriteTo.File("log.txt",
                                rollingInterval: RollingInterval.Day,
                                rollOnFileSizeLimit: true,
                                retainedFileCountLimit: 7)
                            .CreateLogger();

            var sb = ConfigurationSettings.AppSettings["sandbox"];
            sandbox = bool.Parse(sb);

            Mammon = new CoinbaseProClient(sandbox);
            InitializeClauneck();

            Log.Information("CryptoKnight (c) James Hickok.");
            Log.Information("Put your Coinbase Pro API key information into the keys.csv file.");
            Log.Information("The format is name,passphrase,secret,key.");
            Log.Information("Use this to help the people you care about.");
            Log.Information("Make the world a better place...one 'Act of Random Kindness' at a time.");

            var allCoins = Mammon.ProductsService.GetAllProductsAsync().Result
                .Where(x => x.QuoteCurrency == "USD" && !x.TradingDisabled && !x.CancelOnly && !x.LimitOnly && !x.PostOnly)
                .ToList();
            ThrottleSpeedPublic();

            var channels = new List<CoinbasePro.WebSocket.Types.ChannelType>();
            channels.Add(CoinbasePro.WebSocket.Types.ChannelType.Ticker);
            Mammon.WebSocket.Start(allCoins.Select(x => x.Id).ToList(), channels);
            Mammon.WebSocket.OnTickerReceived += WebSocket_OnTickerReceived;

            while (Mammon.WebSocket.State != WebSocket4Net.WebSocketState.Open)
            {
                // Wait for socket to open and get prices, etc.
            }
            
            var areReportsSent = false;
            var timer = DateTime.Now;
            Log.Information("Now scanning cryptocurrencies...");

            var God = "Good";
            while(God == "Good")
            {
                try
                {
                    Console.Write("."); // Lets you know it's still scanning.

                    #region Buy Coins
                    _productStats.Clear();
                    foreach (var coin in allCoins)
                    {
                        try
                        {
                            var stat = new DailyStat(coin.Id, Mammon.ProductsService.GetProductStatsAsync(coin.Id).Result);
                            ThrottleSpeedPublic();

                            _productStats.Add(stat);

                            foreach (var client in Clauneck)
                            {
                                try
                                {
                                    var feeRates = client.FeesService.GetCurrentFeesAsync().Result;

                                    if (IsProfitable(stat, feeRates.MakerFeeRate, feeRates.TakerFeeRate))
                                    {
                                        var now = DateTime.UtcNow;
                                        var candles = Mammon.ProductsService.GetHistoricRatesAsync(
                                                coin.Id,
                                                now.AddMinutes(-30),
                                                now,
                                                CoinbasePro.Services.Products.Types.CandleGranularity.Minutes15
                                            ).Result.OrderBy(x => x.Time).ToList();

                                        if (candles?.Count != 2)
                                        {
                                            break;
                                        }

                                        var candle1 = candles.First();
                                        var candle2 = candles.Last();

                                        if (IsInsideBarPattern(candle1, candle2))
                                        {
                                            // Initiate buy sequence for this client.
                                            var account = client.AccountsService.GetAllAccountsAsync().Result.FirstOrDefault(x => x.Currency == "USD");
                                            ThrottleSpeedPrivate();

                                            var investment = (account.Available * (decimal)0.9) / allCoins.Count;
                                            var price = GetCurrentPrice(coin.Id);
                                            var trailingDistance = GetTrailingDistance(coin);
                                            var stopPrice = price + trailingDistance;
                                            var limitPrice = stopPrice + trailingDistance;

                                            var remainder = investment % coin.QuoteIncrement;
                                            if (remainder > 0)
                                            {
                                                investment -= remainder;
                                            }
                                            if (investment > coin.MaxMarketFunds)
                                            {
                                                investment = coin.MaxMarketFunds;
                                            }
                                            var size = investment / price;
                                            var remainderSize = size % coin.BaseIncrement;
                                            if (remainderSize > 0)
                                            {
                                                size -= remainderSize;
                                            }
                                            if (size > coin.BaseMaxSize)
                                            {
                                                size = coin.BaseMaxSize;
                                            }

                                            if(investment >= coin.MinMarketFunds && size >= coin.BaseMinSize)
                                            {
                                                client.OrdersService.PlaceStopOrderAsync(CoinbasePro.Services.Orders.Types.OrderSide.Buy, coin.Id, size, price, stopPrice).Wait();
                                                ThrottleSpeedPrivate();
                                                Log.Information($"BUY trailing stop order created for {client.Name} on coin {coin.Id} for ${price} per unit.");
                                            }
                                        }
                                    }
                                }
                                catch (Exception exc)
                                {
                                    Log.Error(exc, exc.Message);
                                    if (exc.InnerException != null)
                                    {
                                        Log.Error(exc.InnerException, exc.InnerException.Message);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, ex.Message);
                            if (ex.InnerException != null)
                            {
                                Log.Error(ex.InnerException, ex.InnerException.Message);
                            }
                        }
                    }
                    #endregion

                    #region Handle Orders
                    foreach(var client in Clauneck)
                    {
                        try
                        {
                            var accounts = client.AccountsService.GetAllAccountsAsync().Result;
                            ThrottleSpeedPrivate();

                            foreach(var account in accounts.Where(x => x.Currency != "USD" && x.Currency != "USDC" && x.Currency != "USDT" && x.Available > 0))
                            {
                                try
                                {
                                    // Handle new orders
                                    var productId = $"{account.Currency}-USD";
                                    var currentPrice = GetCurrentPrice(productId);
                                    var feeRates = client.FeesService.GetCurrentFeesAsync().Result;
                                    var slippageRate = (decimal)0.005;
                                    var unitCost = currentPrice + (currentPrice * (feeRates.MakerFeeRate + feeRates.TakerFeeRate + slippageRate));
                                    var profitMargin = (decimal)0.0025;
                                    var price = unitCost + (unitCost * profitMargin);

                                    var thisProduct = allCoins.FirstOrDefault(x => x.Id == productId);
                                    var remainder = price % thisProduct.QuoteIncrement;
                                    if (remainder > 0)
                                    {
                                        price -= remainder;
                                    }
                                    var size = account.Available;
                                    var trailingDistance = GetTrailingDistance(thisProduct);
                                    var stopPrice = price + trailingDistance;

                                    if (size > thisProduct.BaseMaxSize)
                                    {
                                        size = thisProduct.BaseMaxSize;
                                    }

                                    if (size >= thisProduct.BaseMinSize)
                                    {
                                        client.OrdersService.PlaceStopOrderAsync(
                                             CoinbasePro.Services.Orders.Types.OrderSide.Sell,
                                             productId,
                                             size,
                                             price,
                                             stopPrice
                                        ).Wait();
                                        ThrottleSpeedPrivate();

                                        Log.Information($"SELL trailing stop order created for {client.Name} on {productId} for unit price of ${price}.");
                                    }
                                }
                                catch (Exception exc)
                                {
                                    Log.Error(exc, exc.Message);
                                    if (exc.InnerException != null)
                                    {
                                        Log.Error(exc.InnerException, exc.InnerException.Message);
                                    }
                                }
                            }

                            var orders = client.OrdersService.GetAllOrdersAsync(new CoinbasePro.Services.Orders.Types.OrderStatus[] {
                                             CoinbasePro.Services.Orders.Types.OrderStatus.Active,
                                             CoinbasePro.Services.Orders.Types.OrderStatus.Open
                                        }).Result.SelectMany(x => x).ToList();
                            ThrottleSpeedPrivate();

                            foreach (var order in orders)
                            {
                                try
                                {
                                    // Handle existing orders
                                    var currentPrice = GetCurrentPrice(order.ProductId);
                                    var coin = Mammon.ProductsService.GetSingleProductAsync(order.ProductId).Result;
                                    ThrottleSpeedPublic();

                                    var trailingDistance = GetTrailingDistance(coin);

                                    switch (order.Side)
                                    {
                                        case CoinbasePro.Services.Orders.Types.OrderSide.Buy:
                                            var stopPriceBuy = currentPrice + trailingDistance;
                                            var priceBuy = stopPriceBuy + trailingDistance;

                                            if (priceBuy < order.Price)
                                            {
                                                // Drive down the bid.
                                                client.OrdersService.CancelOrderByIdAsync(order.Id.ToString()).Wait();
                                                ThrottleSpeedPrivate();

                                                client.OrdersService.PlaceStopOrderAsync(order.Side, order.ProductId, order.Size, priceBuy, stopPriceBuy).Wait();
                                                ThrottleSpeedPrivate();

                                                Log.Information($"{client.Name}'s {order.Side.ToString()} order for {order.ProductId} driven from ${order.Price} to ${priceBuy}.");
                                            }
                                            break;
                                        case CoinbasePro.Services.Orders.Types.OrderSide.Sell:
                                            var stopPriceSell = currentPrice - trailingDistance;
                                            var priceSell = stopPriceSell - trailingDistance;

                                            if (priceSell > order.Price)
                                            {
                                                // Drive up the ask.
                                                client.OrdersService.CancelOrderByIdAsync(order.Id.ToString()).Wait();
                                                ThrottleSpeedPrivate();

                                                client.OrdersService.PlaceStopOrderAsync(order.Side, order.ProductId, order.Size, priceSell, stopPriceSell).Wait();
                                                ThrottleSpeedPrivate();

                                                Log.Information($"{client.Name}'s {order.Side.ToString()} order for {order.ProductId} driven from ${order.Price} to ${priceSell}.");
                                            }
                                            break;
                                        default:
                                            break;
                                    }

                                    // Handle old orders
                                    var lastWeek = DateTime.UtcNow.AddDays(-7);
                                    if(order.Side == CoinbasePro.Services.Orders.Types.OrderSide.Sell && order.CreatedAt < lastWeek)
                                    {
                                        client.OrdersService.CancelOrderByIdAsync(order.Id.ToString()).Wait();
                                        ThrottleSpeedPrivate();

                                        client.OrdersService.PlaceMarketOrderAsync(
                                             CoinbasePro.Services.Orders.Types.OrderSide.Sell,
                                             order.ProductId,
                                             order.Size,
                                             CoinbasePro.Services.Orders.Types.MarketOrderAmountType.Size
                                            ).Wait();
                                        ThrottleSpeedPrivate();

                                        Log.Information($"Sold aged order for {client.Name} on ${order.ProductId} for whatever it currently is going for.");
                                    }
                                }
                                catch (Exception exc)
                                {
                                    Log.Error(exc, exc.Message);
                                    if (exc.InnerException != null)
                                    {
                                        Log.Error(exc.InnerException, exc.InnerException.Message);
                                    }
                                }
                            }
                            
                            // Generate reports
                            var currentTime = DateTime.Now;
                            if (currentTime.DayOfWeek == DayOfWeek.Sunday)
                            {
                                if (!areReportsSent && currentTime.Hour >= 0 && currentTime.Minute >= 0)
                                {
                                    var reportingTask = SendWeeklyReports(client);
                                    reportingTask.Start();
                                    areReportsSent = true;
                                }
                            }
                            else
                            {
                                if (areReportsSent)
                                {
                                    areReportsSent = false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, ex.Message);
                            if (ex.InnerException != null)
                            {
                                Log.Error(ex.InnerException, ex.InnerException.Message);
                            }
                        }
                    }
                    #endregion

                    allCoins = Mammon.ProductsService.GetAllProductsAsync().Result
                    .Where(x => x.QuoteCurrency == "USD" && !x.TradingDisabled && !x.CancelOnly && !x.LimitOnly && !x.PostOnly)
                    .ToList();
                    ThrottleSpeedPublic();
                }
                catch (Exception e)
                {
                    Log.Error(e, e.Message);
                    if(e.InnerException != null)
                    {
                        Log.Error(e.InnerException, e.InnerException.Message);
                    }
                }
            }
        }

        private static void ThrottleSpeedPublic()
        {
            Thread.Sleep(300);
        }

        private static void ThrottleSpeedPrivate()
        {
            Thread.Sleep(200);
        }
        
        /// <summary>
        /// Give Clauneck a job to do.
        /// </summary>
        private static void InitializeClauneck()
        {
            var contents = File.ReadAllLines("keys.csv");
            var headerRow = contents.FirstOrDefault();
            if(headerRow == "Name,Passphrase,Secret,Key,Email")
            {
                foreach(var friend in contents.Where(x => x != headerRow))
                {
                    var friendKeys = friend.Split(',');

                    if(friendKeys.Count() == 5)
                    {
                        var name = friendKeys.ElementAt(0);
                        var email = friendKeys.ElementAt(4);
                        var key = friendKeys.ElementAt(3);
                        var passphrase = friendKeys.ElementAt(1);
                        var secret = friendKeys.ElementAt(2);
                        var authenticator = new Authenticator(key, secret, passphrase);
                        Clauneck.Add(new PrivateClient(name, email, authenticator, sandbox));
                    }
                }
            }
        }

        private static bool IsBullish(Candle candle)
        {
            return candle.Open < candle.Close;
        }

        private static decimal GetCandleBodySize(Candle candle)
        {
            if (IsBullish(candle))
            {
                return candle.Close.Value - candle.Open.Value;
            }
            else
            {
                return candle.Open.Value - candle.Close.Value;
            }
        }

        private static decimal GetCandleTotalSize(Candle candle)
        {
            return candle.High.Value - candle.Low.Value;
        }

        // Strong inside bar pattern.
        private static bool IsInsideBarPattern(Candle candle1, Candle candle2)
        {
            var insideBar = !IsBullish(candle1) &&
                            IsBullish(candle2) &&
                            GetCandleTotalSize(candle1) > GetCandleTotalSize(candle2) &&
                            GetCandleBodySize(candle1) > GetCandleBodySize(candle2);

            return insideBar;
        }

        private static decimal GetCurrentPrice(string productId)
        {
            if (_tickers.Any(x => x.ProductId == productId))
            {
                return _tickers
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.Sequence)
                .FirstOrDefault().Price;
            }
            else
            {

                var stats = _productStats.FirstOrDefault(x => x.ProductId == productId);
                return stats.Last;
            }
        }
        
        /// <summary>
        /// The point of this is to determine whether the price is low enough that it might be profitable in the same day.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        private static bool IsProfitable(DailyStat stat, decimal makerFeeRate, decimal takerFeeRate)
        {
            // Fee rates plus slippage rate of .5% plus my profit margin of 1/4%.
            var expenseGap = makerFeeRate + takerFeeRate + (decimal)0.0075;

            // Difference in % between the current price and the high price.
            var currentGap = (stat.High - stat.Last) / stat.High;

            return currentGap >= expenseGap;
        }

        private static decimal GetTrailingDistance(CoinbasePro.Services.Products.Models.Product coin)
        {
            var now = DateTime.UtcNow;
            var candles = Mammon.ProductsService.GetHistoricRatesAsync(
                coin.Id,
                now.AddMinutes(-5),
                now,
                CoinbasePro.Services.Products.Types.CandleGranularity.Minutes1
                ).Result;
            ThrottleSpeedPublic();

            var highestHigh = candles.Max(x => x.High);
            var lowestHigh = candles.Min(x => x.High);
            decimal trailingDistance = 0;
            if (highestHigh > 0 && lowestHigh > 0)
            {
                trailingDistance = (decimal)(highestHigh - lowestHigh);
            }
            else
            {
                var stats = _productStats.FirstOrDefault(x => x.ProductId == coin.Id);

                trailingDistance = stats.Last * (decimal)0.005;
                var remainder = trailingDistance % coin.QuoteIncrement;
                if (remainder > 0)
                {
                    trailingDistance -= remainder;
                }
            }
            return trailingDistance;
        }
        
        /// <summary>
        /// Real time price data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void WebSocket_OnTickerReceived(object sender, WebfeedEventArgs<Ticker> e)
        {
            var inTickers = _tickers.Any(x => x.ProductId == e.LastOrder.ProductId);
            if (inTickers)
            {
                // Update it.
                _tickers.RemoveAll(x => x.ProductId == e.LastOrder.ProductId);
                _tickers.Add(e.LastOrder);
            }
            else
            {
                // Add it.
                _tickers.Add(e.LastOrder);
            }
        }

        /// <summary>
        /// Emails weekly reports of fills.
        /// </summary>
        /// <param name="emailAddress"></param>
        private static async Task SendWeeklyReports(PrivateClient client)
        {
            try
            {
                Log.Information($"Emailing weekly reports to {client.Name}.");

                var email = client.Email;
                var start = DateTime.UtcNow.AddDays(-7);
                var end = DateTime.UtcNow;

                var accounts = await client.AccountsService.GetAllAccountsAsync();
                ThrottleSpeedPrivate();

                var usdAccount = accounts.FirstOrDefault(x => x.Currency == "USD");

                foreach (var account in accounts.Where(x => x.Currency != "USD" && x.Currency != "USDC" && x.Currency != "USDT" && x.Balance > 0))
                {
                    try
                    {
                        var productId = $"{account.Currency}-USD";
                        var fills = client.FillsService.GetFillsByProductIdAsync(productId).Result.SelectMany(x => x);
                        ThrottleSpeedPrivate();

                        if (fills.Any(x => x.CreatedAt > start))
                        {
                            Log.Information($"Sending {account.Currency} report.");

                            var responseFill = client.ReportsService.CreateNewFillsReportAsync(
                                start,
                                end,
                                accountId: usdAccount.Id.ToString(),
                                productType: productId,
                                email: email,
                                fileFormat: CoinbasePro.Services.Reports.Types.FileFormat.Pdf
                                ).Result;
                            ThrottleSpeedPrivate();

                            // Wait for the report to email.
                            while (responseFill.Status != CoinbasePro.Services.Reports.Types.ReportStatus.Ready)
                            {
                                responseFill = client.ReportsService.GetReportStatus(responseFill.Id.ToString()).Result;
                                ThrottleSpeedPrivate();
                            }

                            Log.Information($"{account.Currency} report sent.");
                        }
                    }
                    catch (Exception ex1)
                    {
                        Log.Error(ex1, ex1.Message);
                        if (ex1.InnerException != null)
                        {
                            Log.Error(ex1.InnerException, ex1.InnerException.Message);
                        }
                    }
                }
            }
            catch (Exception e1)
            {
                Log.Error(e1, e1.Message);
                if (e1.InnerException != null)
                {
                    Log.Error(e1.InnerException, e1.InnerException.Message);
                }
            }

            Log.Information($"Reporting finished for {client.Name}.");
        }

    }
}
