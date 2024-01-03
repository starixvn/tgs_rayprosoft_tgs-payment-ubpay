using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.UBPay;

namespace UBPay.Core.UnitTests;

public class UBPayClientTests
{
    [Fact]
    public async Task 代收接口_USDT()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");
        var signer = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var sut = new UBPayClient(httpClient, signer, NullLogger<UBPayClient>.Instance);

        var formId = Guid.Parse("5c224598-32d5-4db7-8a21-0f06862925bc");
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";

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

        var order = new DepositOrder(
            100M,
            formId,
            new Uri("https://dev-payment.rpgp.cc/ubpay/deposit/callback"),
            "5c224598-32d5-4db7-8a21-0f06862925bc",
            null);

        var actual = await sut.DepositAsync(apiToken, order);

        Assert.Equal(formId, actual.FormId);
        Assert.Equal("0d1dc4bb-8f1f-48e5-b241-395a1cb61baf", actual.ThirdPartyOrderNo);
    }

    [Fact]
    public async Task 代付接口()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");
        var signer = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var sut = new UBPayClient(httpClient, signer, NullLogger<UBPayClient>.Instance);

        var formId = Guid.Parse("1c4e5fd7-460f-4d18-882c-c16046d693dc");
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

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

        var order = new WithdrawOrder(
            formId,
            500m,
            new Uri("https://dev-payment.rpgp.cc/ubpay/withdraw/callback"),
            "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ");

        var actual = await sut.WithdrawAsync(apiToken, notifyToken, order);

        Assert.Equal(formId, actual.FormId);
        Assert.Equal("9c61fe85-19e7-4e53-922c-f3c96b9097f7", actual.ThirdPartyOrderNo);
    }

    [Fact]
    public async Task 查詢商戶餘額()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");

        var signer = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var sut = new UBPayClient(httpClient, signer, NullLogger<UBPayClient>.Instance);

        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";

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

        var actual = await sut.QueryBalanceAsync(apiToken);

        Assert.Equal(200000m, actual);
    }

    [Fact]
    public async Task 代收查詢_狀態成功()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");

        var signer = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var sut = new UBPayClient(httpClient, signer, NullLogger<UBPayClient>.Instance);

        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var formId = Guid.Parse("5cb4ab68-e705-49ac-84b8-d7ff9abc8a30");

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://ubtxn.com/api/transaction/5cb4ab68-e705-49ac-84b8-d7ff9abc8a30")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "trade_no": "84e857ca-4d67-495f-a59b-d76a42c87d7a",
                        "out_trade_no": "5cb4ab68-e705-49ac-84b8-d7ff9abc8a30",
                        "amount": 200000,
                        "state": "completed"
                    },
                    "success": true
                }
                """));

        var actual = await sut.QueryDepositFormStatusAsync(apiToken, formId);

        Assert.Equal("completed", actual.State);
    }

    [Fact]
    public async Task 代收查詢_狀態逾時()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");

        var signer = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var sut = new UBPayClient(httpClient, signer, NullLogger<UBPayClient>.Instance);

        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var formId = Guid.Parse("5c224598-32d5-4db7-8a21-0f06862925bc");

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://ubtxn.com/api/transaction/5c224598-32d5-4db7-8a21-0f06862925bc")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "trade_no": "0d1dc4bb-8f1f-48e5-b241-395a1cb61baf",
                        "out_trade_no": "5c224598-32d5-4db7-8a21-0f06862925bc",
                        "amount": 200000,
                        "state": "timeout"
                    },
                    "success": true
                }
                """));

        var actual = await sut.QueryDepositFormStatusAsync(apiToken, formId);

        Assert.Equal("timeout", actual.State);
    }

    [Fact]
    public async Task 創建用戶專屬錢包()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");
        var signer = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var sut = new UBPayClient(httpClient, signer, NullLogger<UBPayClient>.Instance);

        var formId = Guid.Parse("5c224598-32d5-4db7-8a21-0f06862925bc");
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";

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

        var actual = await sut.CreatePlayerWalletAsync(apiToken, formId, new Uri("https://dev-payment.rpgp.cc/ubpay/deposit/callback"));

        Assert.Equal("5c224598-32d5-4db7-8a21-0f06862925bc", actual.FirmUniqueId);
        Assert.Equal("TDjorjHSFb32iS59VYV1kw47Vtv87rb16g", actual.WalletAddress);
    }
}
