using System.Net;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay;
using Tgs.Payment.UBPay.Client;
using Tgs.Payment.UBPay.MessageQueue;

namespace UBPay.Core.UnitTests;

public class DepositProcessorTests
{
    [Fact]
    public async Task 儲值要求處理_USDT()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");

        var fakeUBPayClient = new UBPayClient(
            httpClient,
            new UBPayHasher(NullLogger<UBPayHasher>.Instance),
            NullLogger<UBPayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new UBPayOptions
        {
            CallbackBaseUrl = new Uri("https://dev-payment.rpgp.cc/ubpay"),
            PaymentApiUrl = new Uri("https://ubtxn.com"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };
        var formId = Guid.Parse("5c224598-32d5-4db7-8a21-0f06862925bc");
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var sut = new DepositProcessor(
            fakeUBPayClient,
            fakePaymentClient,
            new OptionsWrapper<UBPayOptions>(options));

        var applySettings = new UBPayDepositSettings(
            apiToken,
            notifyToken,
            formId,
            null,
            1);

        var request = new DepositRequest
        {
            Amount = "200000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, UBPayUtility.SerializerOptions)
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://ubtxn.com/api/player-wallet/register")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "firm_unique_id": "5c224598-32d5-4db7-8a21-0f06862925bc",
                        "wallet_address": "TDjorjHSFb32iS59VYV1kw47Vtv87rb16g"
                    },
                    "success": true
                }
                """));

        _ = mockHttp.Expect(HttpMethod.Post, "https://ubtxn.com/api/transaction")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "trade_no": "0d1dc4bb-8f1f-48e5-b241-395a1cb61baf",
                        "out_trade_no": "5c224598-32d5-4db7-8a21-0f06862925bc",
                        "amount": 200000,
                        "address": "TDjorjHSFb32iS59VYV1kw47Vtv87rb16g",
                        "uri": "https:\/\/ubtxn.com\/r\/t\/0d1dc4bb-8f1f-48e5-b241-395a1cb61baf"
                    },
                    "success": true
                }
                """));

        // Act
        var actual = await sut.HandleAsync("test", request, null);

        var applyInfo = JsonSerializer.Deserialize<UBPayDepositApplyInfo>(actual.Result, UBPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
        .ScheduleAsync(
                Arg.Is("tgs.payment.ubpay.deposit.schedule.reject"),
                Arg.Is(applySettings.FormId),
                Arg.Any<DateTime>());
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(
            new UBPayDepositResponse(
                200000m,
                applySettings.FormId,
                new Uri("https://ubtxn.com/r/t/0d1dc4bb-8f1f-48e5-b241-395a1cb61baf"),
                "0d1dc4bb-8f1f-48e5-b241-395a1cb61baf"),
            applyInfo.Response);
    }

    [Fact]
    public async Task 儲值要求處理_USDT_匯率轉換()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");

        var fakeUBPayClient = new UBPayClient(
            httpClient,
            new UBPayHasher(NullLogger<UBPayHasher>.Instance),
            NullLogger<UBPayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new UBPayOptions
        {
            CallbackBaseUrl = new Uri("https://dev-payment.rpgp.cc/ubpay"),
            PaymentApiUrl = new Uri("https://ubtxn.com"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };
        var formId = Guid.Parse("5c224598-32d5-4db7-8a21-0f06862925bc");
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var sut = new DepositProcessor(
            fakeUBPayClient,
            fakePaymentClient,
            new OptionsWrapper<UBPayOptions>(options));

        var applySettings = new UBPayDepositSettings(
            apiToken,
            notifyToken,
            formId,
            null,
            ExchangeRate: 1000m);

        var request = new DepositRequest
        {
            Amount = "200",
            ApplyInfo = JsonSerializer.Serialize(applySettings, UBPayUtility.SerializerOptions)
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://ubtxn.com/api/player-wallet/register")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "firm_unique_id": "5c224598-32d5-4db7-8a21-0f06862925bc",
                        "wallet_address": "TDjorjHSFb32iS59VYV1kw47Vtv87rb16g"
                    },
                    "success": true
                }
                """));

        _ = mockHttp.Expect(HttpMethod.Post, "https://ubtxn.com/api/transaction")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "trade_no": "0d1dc4bb-8f1f-48e5-b241-395a1cb61baf",
                        "out_trade_no": "5c224598-32d5-4db7-8a21-0f06862925bc",
                        "amount": 200000,
                        "address": "TDjorjHSFb32iS59VYV1kw47Vtv87rb16g",
                        "uri": "https:\/\/ubtxn.com\/r\/t\/0d1dc4bb-8f1f-48e5-b241-395a1cb61baf"
                    },
                    "success": true
                }
                """));

        // Act
        var actual = await sut.HandleAsync(
            "test",
            request,
            null);
        
        var applyInfo = JsonSerializer.Deserialize<UBPayDepositApplyInfo>(actual.Result, UBPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
        .ScheduleAsync(
                Arg.Is("tgs.payment.ubpay.deposit.schedule.reject"),
                Arg.Is(applySettings.FormId),
                Arg.Any<DateTime>());
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(
            new UBPayDepositResponse(
                200000m,
                applySettings.FormId,
                new Uri("https://ubtxn.com/r/t/0d1dc4bb-8f1f-48e5-b241-395a1cb61baf"),
                "0d1dc4bb-8f1f-48e5-b241-395a1cb61baf"),
            applyInfo.Response);
    }
}
