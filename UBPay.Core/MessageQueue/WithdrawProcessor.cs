using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.UBPay.MessageQueue;

internal class WithdrawProcessor : IMessageProcessor
{
    private readonly UBPayClient _uBPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly UBPayOptions _options;

    public WithdrawProcessor(
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
        var request = WithdrawRequest.Parser.ParseFrom(data.Span);
        var settings = JsonSerializer.Deserialize<UBPayWithdrawSettings>(request.ApplyInfo, UBPayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(request.Amount) * settings.ExchangeRate,
            new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"),
            settings.Address);

        var withdrawResponse = await _uBPayClient.WithdrawAsync(
            settings.ApiToken,
            settings.NotifyToken,
            order,
            cancellationToken).ConfigureAwait(false);
        var applyInfo = new UBPayWithdrawApplyInfo(
            settings,
            new UBPayWithdrawResponse(withdrawResponse.FormId));

        await _paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            settings.FormId,
            withdrawResponse.ThirdPartyOrderNo,
            cancellationToken);

        var msg = new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}