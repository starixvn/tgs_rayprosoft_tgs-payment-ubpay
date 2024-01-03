using Tgs.Payment.Clients;
using Tgs.Payment.UBPay.Client;

namespace Tgs.Payment.UBPay;
public record WithdrawForm(
    Guid Id,
    string? UserId,
    UserType UserType,
    Guid BrandId,
    Guid? BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    Guid BankInfoId,
    UBPayWithdrawApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
