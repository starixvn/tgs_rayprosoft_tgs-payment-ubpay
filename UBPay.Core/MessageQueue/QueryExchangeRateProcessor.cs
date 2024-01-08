using System.Text.Json;
using Google.Protobuf;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.UBPay.MessageQueue;

internal class QueryExchangeRateProcessor : IMessageProcessor
{
    private readonly UBPayClient _uBPayClient;

    public QueryExchangeRateProcessor(
        UBPayClient uBPayClient)
    {
        _uBPayClient = uBPayClient ?? throw new ArgumentNullException(nameof(uBPayClient));
    }

    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = GeneralRequest.Parser.ParseFrom(data.Span);

        var settings = JsonSerializer.Deserialize<UBPayQueryExchangeRateSettings>(
            request.Content,
            UBPayUtility.SerializerOptions)!;

        var rateResponse = await _uBPayClient
            .QueryBlockChainRateV1Async(settings.ApiToken, settings.Currency, cancellationToken)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var rate = rateResponse.FirstOrDefault(x => x.Platform == settings.Platform);

        if (rate == null)
        {
            var response = new UBPayQueryExchangeRateResponse();

            var applyInfo = new UBPayQueryExchangeRateApplyInfo(settings, response);

            var msg = new GeneralResponse
            {
                Result = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
            };

            return msg.ToByteArray();
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

            var msg = new GeneralResponse
            {
                Result = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
            };

            return msg.ToByteArray();
        }
    }
}
