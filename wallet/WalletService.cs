using System.Text.Json;
using MongoDB.Driver;
using RedLockNet.SERedis;
using StackExchange.Redis;
using wallet.model;
using wallet.Models.Exceptions;

namespace wallet
{
    public class WalletService(IConnectionMultiplexer connectionMultiplexer, RedLockFactory redLockFactory, MongoClient mongoClient)
    {
        private readonly IDatabase _redis = connectionMultiplexer.GetDatabase(0);
        private readonly IMongoDatabase _walletDatabase = mongoClient.GetDatabase("Wallet");

        /// <summary>
        /// 創建錢包
        /// </summary>
        /// <param name="walletId"></param>
        /// <returns></returns>
        /// <exception cref="WalletDuplicateException"></exception>
        /// <exception cref="RedisLockException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<Wallet> PostWalletAsync(Guid walletId)
        {
            var existsWallet = await GetWalletAsync(walletId);
            if (existsWallet != null) throw new WalletDuplicateException(walletId);

            await using var redLock = await redLockFactory.CreateLockAsync(walletId.ToString(), TimeSpan.FromMinutes(1));
            if (!redLock.IsAcquired) throw new RedisLockException(walletId.ToString());

            var newWallet = new Wallet(walletId);
            var transferRecord = new TransferRecord()
            {
                TransferRecordId = Guid.NewGuid(),
                WalletId = newWallet.WalletId,
                Amount = newWallet.Amount,
                BeforeAmount = newWallet.Amount,
                AfterAmount = newWallet.Amount += decimal.Zero,
                CreatedTime = DateTime.Now
            };
            newWallet.ModifiedTime = DateTime.Now;

            var tran = _redis.CreateTransaction();

            _ = tran.StringSetAsync($"{WalletRedisKey.Wallet}:{newWallet.WalletId}:{{{newWallet.GetWalletIdSlot()}}}", JsonSerializer.Serialize(newWallet));
            _ = tran.ListLeftPushAsync($"{WalletRedisKey.TransferRecord}:{{{newWallet.GetWalletIdSlot()}}}",
                JsonSerializer.Serialize(transferRecord));

            var result = await tran.ExecuteAsync();
            if (!result) throw new Exception("Execute Fail!");

            return newWallet;
        }

        /// <summary>
        /// 錢包轉帳
        /// </summary>
        /// <param name="transferId"></param>
        /// <param name="walletId"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        /// <exception cref="RedisLockException"></exception>
        /// <exception cref="WalletNotFoundException"></exception>
        /// <exception cref="BalanceInsufficientException"></exception>
        /// <exception cref="TransferDuplicateException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<TransferRecord> TransferAsync(Guid transferId, Guid walletId, decimal amount)
        {
            await using var redLock = await redLockFactory.CreateLockAsync(walletId.ToString(), TimeSpan.FromMinutes(1));
            if (!redLock.IsAcquired) throw new RedisLockException(walletId.ToString());

            var wallet = await GetWalletAsync(walletId) ?? throw new WalletNotFoundException(walletId);

            if ((wallet.Amount + amount) < 0) throw new BalanceInsufficientException(walletId);

            if (await CheckTransferRecordExists(transferId)) throw new TransferDuplicateException(transferId);

            var transferRecord = new TransferRecord()
            {
                TransferRecordId = transferId,
                WalletId = wallet.WalletId,
                Amount = amount,
                BeforeAmount = wallet.Amount,
                AfterAmount = wallet.Amount += amount,
                CreatedTime = DateTime.Now
            };
            wallet.ModifiedTime = DateTime.Now;
            var tran = _redis.CreateTransaction();

            _ = tran.StringSetAsync($"{WalletRedisKey.Wallet}:{wallet.WalletId}:{{{wallet.GetWalletIdSlot()}}}", JsonSerializer.Serialize(wallet));
            _ = tran.StringSetAsync($"{WalletRedisKey.TransferRecord}:{transferRecord.TransferRecordId}", string.Empty);
            _ = tran.ListLeftPushAsync($"{WalletRedisKey.TransferRecord}:{{{wallet.GetWalletIdSlot()}}}",
                JsonSerializer.Serialize(transferRecord));

            var result = await tran.ExecuteAsync();
            if (!result) throw new Exception("Redis Execute Fail!");

            return transferRecord;
        }

        /// <summary>
        /// 取得錢包
        /// </summary>
        /// <param name="walletId"></param>
        /// <returns></returns>
        public async Task<Wallet?> GetWalletAsync(Guid walletId)
        {
            var walletValue = await _redis.StringGetAsync($"{WalletRedisKey.Wallet}:{walletId}:{{{GetWalletIdSlot(walletId)}}}");
            if (walletValue.HasValue) return JsonSerializer.Deserialize<Wallet>(walletValue);

            var walletCollection = _walletDatabase.GetCollection<wallet.model.mongo.Wallet>("Wallet");
            var walletFilter = Builders<wallet.model.mongo.Wallet>.Filter.Eq(w => w.WalletId, walletId.ToString());
            var bsonWallet = (await walletCollection.FindAsync(walletFilter)).FirstOrDefault();
            if (bsonWallet == default) return null;

            var wallet = new wallet.model.Wallet(Guid.Parse(bsonWallet.WalletId))
            {
                Amount = bsonWallet.Amount,
                CreatedTime = bsonWallet.CreatedTime,
                ModifiedTime = bsonWallet.ModifiedTime
            };

            await _redis.StringSetAsync($"{WalletRedisKey.Wallet}:{wallet.WalletId}:{{{wallet.GetWalletIdSlot()}}}", JsonSerializer.Serialize(wallet));

            return wallet;
        }

        /// <summary>
        /// 檢查轉帳ID是否存在
        /// </summary>
        /// <param name="transferId"></param>
        /// <returns></returns>
        private async Task<bool> CheckTransferRecordExists(Guid transferId)
        {
            var inQueue = await _redis.KeyExistsAsync($"{WalletRedisKey.TransferRecord}:{transferId}");
            if (inQueue) return true;

            var inDatabase = await _redis.KeyExistsAsync($"{WalletRedisKey.TransferRecord}:{transferId}:InDataBase");
            if (inDatabase) return true;

            var transferRecordCollection =
                _walletDatabase.GetCollection<wallet.model.mongo.TransferRecord>("TransferRecord");
            var transferRecordFilter =
                Builders<wallet.model.mongo.TransferRecord>.Filter.Eq(t => t.TransferRecordId,
                    transferId.ToString());
            var bsonTransferRecordCursor = await transferRecordCollection.FindAsync(transferRecordFilter);

            var bsonTransferRecord = await bsonTransferRecordCursor.FirstOrDefaultAsync();
            if (bsonTransferRecord == default)
                return false;

            _ = _redis.StringSetAsync($"{WalletRedisKey.TransferRecord}:{bsonTransferRecord.TransferRecordId}:InDataBase",
                string.Empty, TimeSpan.FromMinutes(10));

            return true;
        }

        private static int GetWalletIdSlot(Guid walletId) => Math.Abs(walletId.GetHashCode()) % 10;
    }
}
