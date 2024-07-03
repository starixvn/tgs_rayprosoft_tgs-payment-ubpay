using System.Net;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay;
using Tgs.Payment.UBPay.Client;
using Tgs.Payment.UBPay.MessageQueue;

namespace UBPay.Core.UnitTests;

public class QueryExchangeRateProcessorTests
{
    [Fact]
    public async Task 查詢交易所匯率()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");

        var fakeUBPayClient = new UBPayClient(
            httpClient,
            new UBPayHasher(NullLogger<UBPayHasher>.Instance),
            NullLogger<UBPayClient>.Instance);

        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var currency = "VND";

        var sut = new QueryExchangeRateProcessor(fakeUBPayClient);

        var applySettings = new UBPayQueryExchangeRateSettings(apiToken, currency, ExchangeRate: 1000);

        var request = new GeneralRequest
        {
            Content = JsonSerializer.Serialize(applySettings, UBPayUtility.SerializerOptions)
        };

        _ = mockHttp
            .Expect(HttpMethod.Get, $"https://ubtxn.com/api/blockchain/rate?fiat={currency}")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": [
                        {
                            "symbol": "usdt",
                            "platform": "binance",
                            "rate": "24880.00"
                        }
                    ],
                    "success": true
                }
                """));
        // Act
        var actual = await sut.HandleAsync("test", request, null);
        
        var applyInfo = JsonSerializer.Deserialize<UBPayQueryExchangeRateApplyInfo>(actual.Result, UBPayUtility.SerializerOptions);

        // Assert
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(new UBPayQueryExchangeRateResponse()
        {
            Symbol = "usdt",
            Platform = "binance",
            Rate = 24.88m
        }, applyInfo.Response);
    }
}
