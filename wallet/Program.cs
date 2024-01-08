using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using wallet;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("localhost:6379")
);

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

builder.Services.AddSingleton<WalletService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
