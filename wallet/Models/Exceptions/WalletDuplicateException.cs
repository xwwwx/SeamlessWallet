namespace wallet.Models.Exceptions
{
    public class WalletDuplicateException(Guid walletId) : Exception
    {
        public override string Message => $"Wallet Record Id Duplicate! WallerId:{WallerId}";

        public Guid WallerId => walletId;
    }
}
