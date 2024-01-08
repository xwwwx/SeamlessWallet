using MongoDB.Bson.Serialization.Attributes;

namespace wallet.model.mongo
{
    public class Wallet
    {
        [BsonId]
        public string WalletId { get; set; } = Guid.NewGuid().ToString();

        public decimal Amount { get; set; } = decimal.Zero;

        public DateTime CreatedTime { get; set; } = DateTime.Now;

        public DateTime ModifiedTime { get; set; } = DateTime.Now;
    }
}
