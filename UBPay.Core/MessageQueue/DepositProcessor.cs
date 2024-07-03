using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.UBPay.MessageQueue;

internal class DepositProcessor(
    UBPayClient uBPayClient,
    IPaymentClient paymentClient,
    IOptions<UBPayOptions> optionsAccessor)
    : IMessageProcessor<DepositRequest, DepositResponse>
{
    private readonly UBPayOptions _options = optionsAccessor.Value;

    public async ValueTask<DepositResponse> HandleAsync(
        string subject,
        DepositRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<UBPayDepositSettings>(
            data.ApplyInfo,
            UBPayUtility.SerializerOptions)!;

        var createWalletResponse = await uBPayClient.CreatePlayerWalletAsync(
            settings.ApiToken,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            cancellationToken).ConfigureAwait(false);

        var order = new DepositOrder(
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            createWalletResponse.FirmUniqueId,
            settings.Currency);

        var depositResponse = await uBPayClient.DepositAsync(
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

        await paymentClient.ScheduleAsync(
            "tgs.payment.ubpay.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(61D),
            cancellationToken).ConfigureAwait(false);

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
        };
    }
}
