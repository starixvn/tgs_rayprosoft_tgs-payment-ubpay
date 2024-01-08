namespace Tgs.Payment.UBPay.Client;
public record UBPayQueryExchangeRateSettings(
    string ApiToken,
    string Currency,
    decimal ExchangeRate,
    string Platform = "binance");
