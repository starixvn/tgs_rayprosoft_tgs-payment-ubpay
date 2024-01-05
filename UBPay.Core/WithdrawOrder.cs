using System.ComponentModel;

namespace Tgs.Payment.UBPay;
public record WithdrawOrder(
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    string Address,
    [property:Description("开单币种 预设U开单")]
    string? Currency);
