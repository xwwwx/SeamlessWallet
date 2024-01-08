namespace wallet.model
{
    public static class WalletRedisKey
    {
        public static string Wallet => "Wallet";
        public static string TransferRecord => "TransferRecord";

        public static string TransferRecordConfirm => "TransferRecordConfirm";

        public static string TransferRecordRetry => "TransferRecordRetry";
    }
}
