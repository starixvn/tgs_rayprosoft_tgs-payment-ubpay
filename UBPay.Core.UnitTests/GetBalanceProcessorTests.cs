using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay;
using Tgs.Payment.UBPay.MessageQueue;

namespace UBPay.Core.UnitTests;
public class GetBalanceProcessorTests
{
    [Fact]
    public async Task GetBalanceProcessor_取得商戶餘額()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");

        var fakeUBClient = new UBPayClient(
            httpClient,
            new UBPayHasher(NullLogger<UBPayHasher>.Instance),
            NullLogger<UBPayClient>.Instance);

        var sut = new GetBalanceProcessor(fakeUBClient);

        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";

        var request = new PaymentBalanceRequest
        {
            BrandPaymentSettings = """
				{
					"apiToken": "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ"
				}
				"""
        };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://ubtxn.com/api/balance/inquiry")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "balance": "200000.0000000"
                    },
                    "success": true
                }
                """));

        // Act
        var actual = await sut.HandleAsync(
            "tgs.payment.ubpay.balance",
            request,
            null);

        // Assert
        Assert.Equal(200000M, actual.Balance.ToDecimal());
    }
}
