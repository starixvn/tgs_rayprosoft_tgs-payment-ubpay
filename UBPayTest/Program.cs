using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Tgs.Payment.Clients;
using Tgs.Payment.UBPay;
using Tgs.Payment.UBPay.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPaymentClient()
    .AddPhoenixToolkitsMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigQueueOptions((opt, _) => opt.Url = "nats://localhost:4222"))
    .AddNatsGlobPatternExchange("*");

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var paymentClient = app.Services.GetRequiredService<IPaymentClient>();

var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

//firm_unique_id: 4ucmqplxb029
//wallet_address: TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ

//代收USDT
//var formId = Guid.NewGuid();
//var depositSettings = new UBPayDepositSettings(apiToken, notifyToken, formId, null, 1m);

//var depositReturn = await paymentClient.DepositAsync(
//    "tgs.payment.ubpay.deposit",
//    200000M,
//    JsonSerializer.Serialize(depositSettings, UBPayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("DepositReturn: {depositReturn}", depositReturn);

//==========================================================================
//代付
//var withdrawSettings = new UBPayWithdrawSettings(
//    apiToken,
//    notifyToken,
//    Guid.NewGuid(),
//    "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ",
//    1m);

//var withdrawReturn = await paymentClient.WithdrawAsync(
//    "tgs.payment.ubpay.withdraw",
//    100M,
//    JsonSerializer.Serialize(withdrawSettings, UBPayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("{withdrawReturn}", withdrawReturn);

//==========================================================================
//餘額查詢
//var http = new HttpClient
//{
//    BaseAddress = new Uri("https://ubtxn.com")
//};

//var client = new UBPayClient(http, new UBPayHasher(NullLogger<UBPayHasher>.Instance), NullLogger<UBPayClient>.Instance);
//var balance = await client.QueryBalanceAsync(apiToken).ConfigureAwait(false);
//logger.LogInformation("{balance}", balance);

//==========================================================================
//代收查詢接口
//var http = new HttpClient
//{
//    BaseAddress = new Uri("https://ubtxn.com")
//};

//var client = new UBPayClient(http, new UBPayHasher(NullLogger<UBPayHasher>.Instance), NullLogger<UBPayClient>.Instance);
//var depositResponse = await client
//    .QueryDepositFormStatusAsync(apiToken, Guid.Parse("5cb4ab68-e705-49ac-84b8-d7ff9abc8a30"))
//    .ConfigureAwait(false);
//logger.LogInformation("{state}", depositResponse.State);

//==========================================================================
//交易所匯率查詢
var exchangeRateSettings = new UBPayQueryExchangeRateSettings(apiToken, "VND", ExchangeRate: 1000m);

var exchangeRateReturn = await paymentClient.AskAsync(
    "tgs.payment.ubpay.queryexchangerate",
    JsonSerializer.Serialize(exchangeRateSettings, UBPayUtility.SerializerOptions));

logger.LogInformation("{exchangeRateReturn}", exchangeRateReturn);
