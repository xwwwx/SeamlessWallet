namespace wallet.Models.Exceptions
{
    public class BalanceInsufficientException(Guid walletId) : Exception
    {
        public override string Message => $"Balance insufficient! WalletId:{WalletId}";

        public Guid WalletId => walletId;
    }
}
