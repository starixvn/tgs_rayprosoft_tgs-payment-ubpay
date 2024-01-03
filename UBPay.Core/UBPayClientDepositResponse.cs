namespace Tgs.Payment.UBPay;

public record UBPayClientDepositResponse(
    decimal Amount,
    Guid FormId,
    string ThirdPartyOrderNo,
    Uri RedirectUrl);