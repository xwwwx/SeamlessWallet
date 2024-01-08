using Coravel.Invocable;
using MongoDB.Driver;
using RedLockNet.SERedis;
using StackExchange.Redis;
using System.Text.Json;
using wallet.model;

namespace wallet.schedule.sync
{
    internal class ConfirmTransferRecordSchedule(IConnectionMultiplexer connectionMultiplexer, MongoClient mongoClient, RedLockFactory redLockFactory) : IInvocable, ICancellableInvocable
    {
        private readonly IMongoDatabase _walletDatabase = mongoClient.GetDatabase("Wallet");
        private readonly IDatabase _redis = connectionMultiplexer.GetDatabase(0);

        public async Task Invoke()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                var slot = Math.Abs(Guid.NewGuid().GetHashCode() % 10);

                await using var redLock =
                    await redLockFactory.CreateLockAsync($"lock:{WalletRedisKey.TransferRecordConfirm}:{{{slot}}}",
                        TimeSpan.FromSeconds(10));
                if (!redLock.IsAcquired) throw new Exception("Redis Lock!");

                var peekTransferRecord =
                    await _redis.ListRangeAsync($"{WalletRedisKey.TransferRecordConfirm}:{{{slot}}}", -1, -1);
                if (peekTransferRecord.Length == 0) continue;

                var transferRecord = JsonSerializer.Deserialize<wallet.model.TransferRecord>(peekTransferRecord[0]);
                // 交易紀錄建立超過1秒再confirm
                if (DateTime.Now - transferRecord.CreatedTime < TimeSpan.FromSeconds(1)) continue;

                var bsonTransferRecord = new wallet.model.mongo.TransferRecord()
                {
                    TransferRecordId = transferRecord.TransferRecordId.ToString(),
                    WalletId = transferRecord.WalletId.ToString(),
                    AfterAmount = transferRecord.AfterAmount,
                    BeforeAmount = transferRecord.BeforeAmount,
                    Amount = transferRecord.Amount,
                    CreatedTime = transferRecord.CreatedTime
                };

                if (await CheckRecordExistsAsync(bsonTransferRecord))
                {
                    await _redis.ListRightPopAsync($"{WalletRedisKey.TransferRecordConfirm}:{{{slot}}}");
                    await _redis.KeyDeleteAsync($"{WalletRedisKey.TransferRecord}:{transferRecord.TransferRecordId}");
                }
                else
                    await _redis.ListRightPopLeftPushAsync(
                        $"{WalletRedisKey.TransferRecordConfirm}:{{{slot}}}",
                        $"{WalletRedisKey.TransferRecord}:{{{slot}}}");
            }
        }

        public CancellationToken CancellationToken { get; set; }

        private async Task<bool> CheckRecordExistsAsync(wallet.model.mongo.TransferRecord record)
        {
            var transferRecordCollection =
                _walletDatabase.GetCollection<wallet.model.mongo.TransferRecord>("TransferRecord");
            var filter =
                Builders<wallet.model.mongo.TransferRecord>.Filter.Eq(w => w.TransferRecordId,
                    record.TransferRecordId);
            var findOptions = new FindOptions<wallet.model.mongo.TransferRecord>
            {
                Limit = 1
            };
            var findResult = await transferRecordCollection
                .FindAsync(filter, findOptions, CancellationToken);
            return await findResult.AnyAsync(CancellationToken);
        }
    }
}
