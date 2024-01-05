namespace Tgs.Payment.UBPay.Client;
public record UBPayWithdrawReturn
{
    public string ThirdPartyOrderNo { get; set; } = default!;

    public decimal Amount { get; set; }

    public Guid FormId { get; set; }

    public string Status { get; set; } = default!;

    public string Sign { get; set; } = default!;

    public decimal AfterExchangeRateAmount { get; set; }
}
