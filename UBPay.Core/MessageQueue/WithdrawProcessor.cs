using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.UBPay.MessageQueue;

internal class WithdrawProcessor(
    UBPayClient uBPayClient,
    IPaymentClient paymentClient,
    IOptions<UBPayOptions> optionsAccessor)
    : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly UBPayOptions _options = optionsAccessor.Value;

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<UBPayWithdrawSettings>(data.ApplyInfo, UBPayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"),
            settings.Address,
            settings.Currency);

        var withdrawResponse = await uBPayClient.WithdrawAsync(
            settings.ApiToken,
            settings.NotifyToken,
            order,
            cancellationToken).ConfigureAwait(false);
        var applyInfo = new UBPayWithdrawApplyInfo(
            settings,
            new UBPayWithdrawResponse(withdrawResponse.FormId));

        await paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            settings.FormId,
            withdrawResponse.ThirdPartyOrderNo,
            cancellationToken);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
        };
    }
}
