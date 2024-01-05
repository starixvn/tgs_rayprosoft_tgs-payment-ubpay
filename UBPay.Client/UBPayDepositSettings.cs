using System.ComponentModel;

namespace Tgs.Payment.UBPay.Client;
public record UBPayDepositSettings(
    string ApiToken,
    string NotifyToken,
    Guid FormId,
    [property: Description("開單幣種 預設U開單")]
    string? Currency,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);