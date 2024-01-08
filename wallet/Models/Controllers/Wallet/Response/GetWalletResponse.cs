namespace wallet.Models.Controllers.Wallet.Response
{
    public class GetWalletResponse : ResponseBase
    {
        public GetWalletResponsePayload Data { get; set; } = default;

        public class GetWalletResponsePayload(Guid walletId)
        {
            public Guid WalletId { get; } = walletId;

            public decimal Amount { get; set; } = decimal.Zero;
        }
    }
}
