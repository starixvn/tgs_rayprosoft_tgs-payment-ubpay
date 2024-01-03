namespace Tgs.Payment.UBPay.Client;

public record UBPayDepositReturn
{
    public string ThirdPartyOrderNo { get; set; } = default!;

    public decimal OrderAmount { get; set; }

    public Guid FormId { get; set; }

    public string Status { get; set; } = default!;

    public string Sign { get; set; } = default!;

    public string BlockchainRate { get; set; } = default!;
}
