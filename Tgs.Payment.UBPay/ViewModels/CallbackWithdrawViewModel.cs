using System.Text.Json.Serialization;

namespace Tgs.Payment.UBPay.ViewModels;

public class CallbackWithdrawViewModel
{
    [JsonPropertyName("trade_no")]
    public string TradeNo { get; set; } = default!;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("out_trade_no")]
    public string OutTradeNo { get; set; } = default!;

    [JsonPropertyName("state")]
    public string State { get; set; } = default!;

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = default!;
}
