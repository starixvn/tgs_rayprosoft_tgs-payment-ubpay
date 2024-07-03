using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Tgs.Payment.UBPay.MessageQueue;
using Tgs.Payment.UBPay;
using System.Text.Json;
using Google.Protobuf;

namespace UBPay.Core.UnitTests;

public class WithdrawProcessorTests
{
    [Fact]
    public async Task 處理提款要求()
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

        var sut = new WithdrawProcessor(
            fakeUBPayClient,
            fakePaymentClient,
        new OptionsWrapper<UBPayOptions>(options));

        var formId = Guid.Parse("1c4e5fd7-460f-4d18-882c-c16046d693dc");
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var applySettings = new UBPayWithdrawSettings(
            apiToken,
            notifyToken,
            formId,
            "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ",
            Currency: null,
            ExchangeRate: 1m);

        var request = new WithdrawRequest
        {
            Amount = "100",
            ApplyInfo = JsonSerializer.Serialize(applySettings, UBPayUtility.SerializerOptions)
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://ubtxn.com/api/payment")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "trade_no": "9c61fe85-19e7-4e53-922c-f3c96b9097f7",
                        "out_trade_no": "1c4e5fd7-460f-4d18-882c-c16046d693dc",
                        "address": "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ",
                        "amount": 100,
                        "callback_url": "https:\/\/dev-payment.rpgp.cc\/ubpay\/withdraw\/callback",
                        "state": "new"
                    },
                    "success": true
                }
                """));

        // Act
        var actual = await sut.HandleAsync("test", request, null);

        var applyInfo = JsonSerializer.Deserialize<UBPayWithdrawApplyInfo>(actual.Result, UBPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("9c61fe85-19e7-4e53-922c-f3c96b9097f7"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }

    [Fact]
    public async Task 處理提款要求_匯率轉換()
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

        var sut = new WithdrawProcessor(
            fakeUBPayClient,
            fakePaymentClient,
        new OptionsWrapper<UBPayOptions>(options));

        var formId = Guid.Parse("1c4e5fd7-460f-4d18-882c-c16046d693dc");
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var applySettings = new UBPayWithdrawSettings(
            apiToken,
            notifyToken,
            formId,
            "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ",
            Currency: "VND",
            ExchangeRate: 1000m);

        var request = new WithdrawRequest
        {
            Amount = "0.1",
            ApplyInfo = JsonSerializer.Serialize(applySettings, UBPayUtility.SerializerOptions)
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://ubtxn.com/api/payment")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "trade_no": "9c61fe85-19e7-4e53-922c-f3c96b9097f7",
                        "out_trade_no": "1c4e5fd7-460f-4d18-882c-c16046d693dc",
                        "address": "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ",
                        "amount": 100,
                        "callback_url": "https:\/\/dev-payment.rpgp.cc\/ubpay\/withdraw\/callback",
                        "state": "new"
                    },
                    "success": true
                }
                """));

        // Act
        var actual = await sut.HandleAsync("test", request, null);

        var applyInfo = JsonSerializer.Deserialize<UBPayWithdrawApplyInfo>(actual.Result, UBPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("9c61fe85-19e7-4e53-922c-f3c96b9097f7"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }
}
