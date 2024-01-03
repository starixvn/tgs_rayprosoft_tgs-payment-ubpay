namespace Tgs.Payment.UBPay;
public record UBPayClientWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal Amount);