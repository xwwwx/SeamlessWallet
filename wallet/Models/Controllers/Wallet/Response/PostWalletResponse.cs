namespace wallet.Models.Controllers.Wallet.Response
{
    public class PostWalletResponse : ResponseBase
    {
        public PostWalletResponsePayload Data { get; set; } = default;

        public class PostWalletResponsePayload(Guid walletId)
        {
            public Guid WalletId { get; } = walletId;

            public decimal Amount { get; set; } = decimal.Zero;
        }
    }
}
