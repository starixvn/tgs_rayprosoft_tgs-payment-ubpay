using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.UBPay.MessageQueue;

internal class DepositProcessor : IMessageProcessor
{
    private readonly UBPayClient _uBPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly UBPayOptions _options;

    public DepositProcessor(
        UBPayClient uBPayClient,
        IPaymentClient paymentClient,
        IOptions<UBPayOptions> optionsAccessor)
    {
        _uBPayClient = uBPayClient ?? throw new ArgumentNullException(nameof(uBPayClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _options = optionsAccessor.Value;
    }

    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = DepositRequest.Parser.ParseFrom(data.Span);

        var settings = JsonSerializer.Deserialize<UBPayDepositSettings>(
            request.ApplyInfo,
            UBPayUtility.SerializerOptions)!;

        var createWalletResponse = await _uBPayClient.CreatePlayerWalletAsync(
            settings.ApiToken,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            cancellationToken).ConfigureAwait(false);

        var order = new DepositOrder(
            decimal.Parse(request.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            createWalletResponse.FirmUniqueId,
            settings.Currency);

        var depositResponse = await _uBPayClient.DepositAsync(
            settings.ApiToken,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new UBPayDepositApplyInfo(
            settings,
            new UBPayDepositResponse(
                depositResponse.Amount,
                depositResponse.FormId,
                depositResponse.RedirectUrl,
                depositResponse.ThirdPartyOrderNo));

        await _paymentClient.ScheduleAsync(
            "tgs.payment.ubpay.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(61D),
            cancellationToken).ConfigureAwait(false);

        var msg = new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}
