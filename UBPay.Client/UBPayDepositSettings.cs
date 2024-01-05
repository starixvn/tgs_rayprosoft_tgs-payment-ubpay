using System.ComponentModel;

namespace Tgs.Payment.UBPay.Client;
public record UBPayDepositSettings(
    string ApiToken,
    string NotifyToken,
    Guid FormId,    
    string? Currency,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);