namespace Tgs.Payment.UBPay;
public record UBPayClientCreateWalletResponse(
    string FirmUniqueId,
    string WalletAddress);
