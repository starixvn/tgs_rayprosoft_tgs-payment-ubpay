using System.Text.Json.Serialization;

namespace Tgs.Payment.UBPay.ViewModels;

public class CallbackDepositViewModel
{
    [JsonPropertyName("trade_no")]
    public string TradeNo { get; set; } = default!;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("out_trade_no")]
    public string OutTradeNo { get; set; } = default!;

    [JsonPropertyName("state")]
    public string State { get; set; } = default!;

    [JsonPropertyName("blockchain_rate")]
    public string BlockchainRate { get; set; } = default!;

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = default!;
}
