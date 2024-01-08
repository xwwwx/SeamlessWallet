namespace wallet.model
{
    public class Wallet(Guid walletId)
    {
        public Guid WalletId { get; } = walletId;

        public decimal Amount { get; set; } = decimal.Zero;

        public DateTime CreatedTime { get; set; } = DateTime.Now;

        public DateTime ModifiedTime { get; set; } = DateTime.Now;

        public int GetWalletIdSlot() => Math.Abs(WalletId.GetHashCode()) % 10;
    }
}
