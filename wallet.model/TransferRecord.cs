namespace wallet.model
{
    public class TransferRecord
    {
        public Guid TransferRecordId { get; set; }

        public Guid WalletId { get; set; }

        public decimal BeforeAmount { get; set; }

        public decimal AfterAmount { get; set; }

        public decimal Amount { get; set; }

        public DateTime CreatedTime { get; set; }
    }
}
