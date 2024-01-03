using Tgs.Payment.Clients;
using Tgs.Payment.UBPay.Client;

namespace Tgs.Payment.UBPay;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
decimal Amount,
    PaymentFlowState Status,
    UBPayDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);