using System.ComponentModel;

namespace Tgs.Payment.UBPay.Client;
public record UBPayWithdrawSettings(
    string ApiToken,
    string NotifyToken,
    Guid FormId,
    string Address,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);