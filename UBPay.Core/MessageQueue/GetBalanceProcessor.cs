using System.Text.Json;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.UBPay.MessageQueue;

internal class GetBalanceProcessor(
    UBPayClient uBPayClient)
    : IMessageProcessor<PaymentBalanceRequest, PaymentBalanceResponse>
{
    public async ValueTask<PaymentBalanceResponse> HandleAsync(
        string subject,
        PaymentBalanceRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<BrandPaymentMerchantInfo>(
            data.BrandPaymentSettings,
            UBPayUtility.SerializerOptions)!;

        var result = await uBPayClient.QueryBalanceAsync(
            settings.ApiToken,
            cancellationToken).ConfigureAwait(false);

        return new PaymentBalanceResponse
        {
            Balance = result
        };
    }
}
