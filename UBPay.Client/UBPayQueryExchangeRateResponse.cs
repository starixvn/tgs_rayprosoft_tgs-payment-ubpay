namespace Tgs.Payment.UBPay.Client;
public record UBPayQueryExchangeRateResponse
{
    public string? Symbol { get; set; }
    public string? Platform { get; set; }
    public decimal Rate { get; set; }
}
