namespace Tgs.Payment.UBPay;
public record UBPayClientRateV1Response(
    string Symbol,
    string Platform,
    decimal Rate);
