// See https://aka.ms/new-console-template for more information

using Coravel;
using Coravel.Scheduling.Schedule.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using wallet.schedule.sync;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddSingleton(sp =>
{
    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
    var multiplexers = new List<RedLockMultiplexer>
    {
        (ConnectionMultiplexer)redis.GetDatabase().Multiplexer
    };
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return RedLockFactory.Create(multiplexers, loggerFactory);
});

builder.Services.AddSingleton(sp => new MongoClient("mongodb://host.docker.internal:27017/"));

builder.Services.AddScheduler();

builder.Services.AddSingleton<SyncTransferRecordSchedule>();
builder.Services.AddSingleton<ConfirmTransferRecordSchedule>();

var host = builder.Build();

host.Services.UseScheduler(scheduler =>
{
    scheduler.Schedule<SyncTransferRecordSchedule>().EverySecond()
        .PreventOverlapping(nameof(SyncTransferRecordSchedule)).RunOnceAtStart();
    scheduler.Schedule<ConfirmTransferRecordSchedule>().EverySecond()
        .PreventOverlapping(nameof(ConfirmTransferRecordSchedule)).RunOnceAtStart();
}).LogScheduledTaskProgress(host.Services.GetRequiredService<ILogger<IScheduler>>());

await host.RunAsync();
