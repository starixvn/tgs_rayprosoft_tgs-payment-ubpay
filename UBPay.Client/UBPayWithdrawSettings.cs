namespace Tgs.Payment.UBPay.Client;
public record UBPayWithdrawSettings(
    string ApiToken,
    string NotifyToken,
    Guid FormId,
    string Address);