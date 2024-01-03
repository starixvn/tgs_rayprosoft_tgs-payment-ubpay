using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.UBPay;

namespace UBPay.Core.UnitTests;

public class HasherTests
{
    [Fact]
    public void 代付()
    {
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var dict = new Dictionary<string, string?>
        {
            ["out_trade_no"] = "1c4e5fd7-460f-4d18-882c-c16046d693dc",
            ["amount"] = 100.ToString(),
            ["callback_url"] = "https://dev-payment.rpgp.cc/ubpay/withdraw/callback",
            ["address"] = "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ",            
            ["sign"] = "229823be9390cf04b09c9527050bfa3b",
        };
        var expected = "229823be9390cf04b09c9527050bfa3b";

        var sut = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 代收回調成功()
    {
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var dict = new Dictionary<string, string?>
        {
            ["trade_no"] = "84e857ca-4d67-495f-a59b-d76a42c87d7a",
            ["amount"] = 200000.ToString(),
            ["out_trade_no"] = "5cb4ab68-e705-49ac-84b8-d7ff9abc8a30",
            ["state"] = "completed",
            ["blockchain_rate"] = "7.16",
            ["sign"] = "b81d36f6e7baecb3b928d9d8099d86b7",
        };
        var expected = "b81d36f6e7baecb3b928d9d8099d86b7";

        var sut = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 代付回調_成功()
    {
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var dict = new Dictionary<string, string?>
        {
            ["trade_no"] = "9c61fe85-19e7-4e53-922c-f3c96b9097f7",
            ["amount"] = 100.ToString(),
            ["out_trade_no"] = "1c4e5fd7-460f-4d18-882c-c16046d693dc",
            ["state"] = "completed",
            ["sign"] = "4afc59327f96a1799c46ff5339a9efc6",
        };
        var expected = "4afc59327f96a1799c46ff5339a9efc6";

        var sut = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 代付回調_失敗()
    {
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var dict = new Dictionary<string, string?>
        {
            ["trade_no"] = "78330832-b465-41c1-8e59-3ce2f6e79ae9",
            ["amount"] = 100.ToString(),
            ["out_trade_no"] = "1011c749-d2f9-4e3c-8aa1-221b4718c63a",
            ["state"] = "failed",
            ["sign"] = "6fe27cf80a7b0e082fe28d72359a149a",
        };
        var expected = "6fe27cf80a7b0e082fe28d72359a149a";

        var sut = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 代付回調_沖回()
    {
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var dict = new Dictionary<string, string?>
        {
            ["trade_no"] = "300adb28-9aea-4861-8240-70acdda414e7",
            ["amount"] = 100.ToString(),
            ["out_trade_no"] = "b5c10c19-f7dc-4bb9-adab-90242fa8287c",
            ["state"] = "refund",
            ["sign"] = "ac5731fd60adfe42df77efe29d7b606f",
        };
        var expected = "ac5731fd60adfe42df77efe29d7b606f";

        var sut = new UBPayHasher(NullLogger<UBPayHasher>.Instance);

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }
}
