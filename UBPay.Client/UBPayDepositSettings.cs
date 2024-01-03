namespace Tgs.Payment.UBPay.Client;
public record UBPayDepositSettings(
    string ApiToken,
    string NotifyToken,
    Guid FormId,    
    string? Currency);