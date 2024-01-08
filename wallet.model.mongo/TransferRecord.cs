using MongoDB.Bson.Serialization.Attributes;

namespace wallet.model.mongo
{
    public class TransferRecord
    {
        [BsonId]
        public string TransferRecordId { get; set; } = Guid.NewGuid().ToString();

        public string WalletId { get; set; } = Guid.NewGuid().ToString();

        public decimal BeforeAmount { get; set; } = decimal.Zero;

        public decimal AfterAmount { get; set; } = decimal.Zero;

        public decimal Amount { get; set; } = decimal.Zero;

        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}
