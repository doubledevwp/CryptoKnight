using CoinbasePro.Services.Products.Types;

namespace CryptoKnight
{
    public class DailyStat : ProductStats
    {
        public DailyStat(string productId, ProductStats b)
        {
            this.ProductId = productId;
            this.High = b.High;
            this.Low = b.Low;
            this.Open = b.Open;
            this.Last = b.Last;
            this.Volume = b.Volume;
            this.Volume30Day = b.Volume30Day;
        }

        public string ProductId { get; set; }
    }
}
