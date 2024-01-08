namespace wallet.Models.Controllers.Wallet.Response
{
    public class TransferRecordResponse : ResponseBase
    {
        public TransferRecordResponsePayload Data { get; set; } = default;

        public class TransferRecordResponsePayload
        {
            public Guid TransferRecordId { get; set; }

            public Guid WalletId { get; set; }

            public decimal BeforeAmount { get; set; }

            public decimal AfterAmount { get; set; }

            public decimal Amount { get; set; }

            public DateTime CreatedTime { get; set; }
        }
    }
}
