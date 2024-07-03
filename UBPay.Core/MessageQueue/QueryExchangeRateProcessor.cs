using System.Text.Json;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.UBPay.MessageQueue;

internal class QueryExchangeRateProcessor(
    UBPayClient uBPayClient)
    : IMessageProcessor<GeneralRequest, GeneralResponse>
{
    public async ValueTask<GeneralResponse> HandleAsync(
        string subject,
        GeneralRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<UBPayQueryExchangeRateSettings>(
            data.Content,
            UBPayUtility.SerializerOptions)!;

        var rateResponse = await uBPayClient
            .QueryBlockChainRateV1Async(settings.ApiToken, settings.Currency, cancellationToken)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var rate = rateResponse.FirstOrDefault(x => x.Platform == settings.Platform);

        if (rate == null)
        {
            var response = new UBPayQueryExchangeRateResponse();

            var applyInfo = new UBPayQueryExchangeRateApplyInfo(settings, response);

            return new GeneralResponse
            {
                Result = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
            };
        }
        else
        {
            var response = new UBPayQueryExchangeRateResponse
            {
                Symbol = rate.Symbol,
                Platform = rate.Platform,
                Rate = rate.Rate / settings.ExchangeRate
            };

            var applyInfo = new UBPayQueryExchangeRateApplyInfo(settings, response);

            return new GeneralResponse
            {
                Result = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
            };
        }
    }
}
