using System.Text.Json;
using Coravel.Invocable;
using MongoDB.Driver;
using RedLockNet.SERedis;
using StackExchange.Redis;
using wallet.model;

namespace wallet.schedule.sync
{
    internal class SyncTransferRecordSchedule(IConnectionMultiplexer connectionMultiplexer, MongoClient mongoClient) : IInvocable, ICancellableInvocable
    {
        private readonly IMongoDatabase _walletDatabase = mongoClient.GetDatabase("Wallet");
        private readonly IDatabase _redis = connectionMultiplexer.GetDatabase(0);

        public async Task Invoke()
        {
            var transferRecordCollection = _walletDatabase.GetCollection<wallet.model.mongo.TransferRecord>("TransferRecord");
            var walletCollection = _walletDatabase.GetCollection<wallet.model.mongo.Wallet>("Wallet");
            while (!CancellationToken.IsCancellationRequested)
            {
                var slot = Math.Abs(Guid.NewGuid().GetHashCode() % 10);
                var lastTransferRecordValue = await _redis.ListRightPopLeftPushAsync(
                    $"{WalletRedisKey.TransferRecord}:{{{slot}}}",
                    $"{WalletRedisKey.TransferRecordConfirm}:{{{slot}}}");
                if(!lastTransferRecordValue.HasValue) continue;
                var transferRecord = JsonSerializer.Deserialize<wallet.model.TransferRecord>(lastTransferRecordValue);
                var bsonTransferRecord = new wallet.model.mongo.TransferRecord()
                {
                    TransferRecordId = transferRecord.TransferRecordId.ToString(),
                    WalletId = transferRecord.WalletId.ToString(),
                    AfterAmount = transferRecord.AfterAmount,
                    BeforeAmount = transferRecord.BeforeAmount,
                    Amount = transferRecord.Amount,
                    CreatedTime = transferRecord.CreatedTime
                };

                var walletValue = await _redis.StringGetAsync(
                    $"{WalletRedisKey.Wallet}:{transferRecord.WalletId}:{{{GetWalletIdSlot(transferRecord.WalletId)}}}");
                if (!walletValue.HasValue) throw new Exception("Wallet Not Exists!");
                var wallet = JsonSerializer.Deserialize<wallet.model.Wallet>(walletValue);
                var bsonWallet = new wallet.model.mongo.Wallet()
                {
                    WalletId = wallet.WalletId.ToString(),
                    Amount = wallet.Amount,
                    CreatedTime = wallet.CreatedTime,
                    ModifiedTime = wallet.ModifiedTime,
                };

                #region With Transaction Ver

                //using var session = await mongoClient.StartSessionAsync(cancellationToken: CancellationToken);
                //session.StartTransaction();

                //await transferRecordCollection.InsertOneAsync(session, bsonTransferRecord, cancellationToken: CancellationToken);

                //var walletUpsertFilter =
                //    Builders<wallet.model.mongo.Wallet>.Filter.Eq(w => w.WalletId, bsonWallet.WalletId);
                //var walletUpsertOption = new ReplaceOptions() { IsUpsert = true };
                //await walletCollection.ReplaceOneAsync(session, walletUpsertFilter, bsonWallet, walletUpsertOption,
                //    CancellationToken);

                //await session.CommitTransactionAsync(CancellationToken);

                #endregion

                #region Without Transaction Ver

                await transferRecordCollection.InsertOneAsync(bsonTransferRecord, cancellationToken: CancellationToken);

                var walletUpsertFilter =
                    Builders<wallet.model.mongo.Wallet>.Filter.Eq(w => w.WalletId, bsonWallet.WalletId);
                var walletUpsertOption = new ReplaceOptions() { IsUpsert = true };
                await walletCollection.ReplaceOneAsync(walletUpsertFilter, bsonWallet, walletUpsertOption,
                    CancellationToken);

                #endregion

            }
        }

        public CancellationToken CancellationToken { get; set; }
        private static int GetWalletIdSlot(Guid walletId) => Math.Abs(walletId.GetHashCode()) % 10;
    }
}
