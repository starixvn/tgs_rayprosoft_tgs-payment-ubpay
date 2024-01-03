namespace Tgs.Payment.UBPay;
public record WithdrawOrder(
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    string Address);