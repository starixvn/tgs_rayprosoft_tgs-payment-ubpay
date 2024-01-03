namespace Tgs.Payment.UBPay.Client;
public record UBPayDepositApplyInfo(
    UBPayDepositSettings Settings,
    UBPayDepositResponse Response);