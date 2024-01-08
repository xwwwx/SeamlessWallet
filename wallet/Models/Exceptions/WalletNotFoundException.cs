namespace wallet.Models.Exceptions
{
    public class WalletNotFoundException(Guid walletId) : Exception
    {
        public override string Message => $"Wallet Not Found! WalletId:{WalletId}";

        public Guid WalletId => walletId;
    }
}
