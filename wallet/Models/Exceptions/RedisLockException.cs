namespace wallet.Models.Exceptions
{
    public class RedisLockException(string lockKey) : Exception
    {
        public override string Message => $"Redis Lock! LockKey:{LockKey}";

        public string LockKey => lockKey;

    }
}
