using System.ComponentModel;

namespace Tgs.Payment.UBPay;
public record DepositOrder(
    decimal Amount,
    Guid FormId,
    Uri CallbackUrl,
    [property: Description("玩家钱包唯一辨识码")]
    string FirmUniqueId,    
    [property:Description("开单币种 预设U开单")]
    string? Currency);