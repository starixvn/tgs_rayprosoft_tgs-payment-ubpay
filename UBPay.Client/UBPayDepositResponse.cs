namespace Tgs.Payment.UBPay.Client;
public record UBPayDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl,
    string ThirdPartyOrderNo);