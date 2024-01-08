using Coravel.Invocable;
using MongoDB.Driver;
using RedLockNet.SERedis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet.model;

namespace wallet.schedule.sync
{
    internal class ProcessRetryTransferRecordSchedule(IConnectionMultiplexer connectionMultiplexer) : IInvocable
    {
        private readonly IDatabase _redis = connectionMultiplexer.GetDatabase(0);
        public async Task Invoke()
        {
            var slot = Math.Abs(Guid.NewGuid().GetHashCode() % 10);

            await _redis.ListRightPopLeftPushAsync(
                $"{WalletRedisKey.TransferRecordRetry}:{{{slot}}}",
                $"{WalletRedisKey.TransferRecord}:{{{slot}}}");
        }
    }
}
