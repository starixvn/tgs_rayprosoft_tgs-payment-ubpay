namespace Tgs.Payment.UBPay.Client;
public record UBPayWithdrawApplyInfo(
    UBPayWithdrawSettings Settings,
    UBPayWithdrawResponse Response);