using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.UBPay.MessageQueue;

internal class DepositTimeoutCheckHandler : IMessageHandler
{
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly UBPayClient _uBPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly ILogger<DepositTimeoutCheckHandler> _logger;

    public DepositTimeoutCheckHandler(
        IPaymentServiceClient paymentServiceClient,
        UBPayClient uBPayClient,
        IPaymentClient paymentClient,
        ILogger<DepositTimeoutCheckHandler> logger)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
        _uBPayClient = uBPayClient ?? throw new ArgumentNullException(nameof(uBPayClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = ScheduleRequest.Parser.ParseFrom(data.Span);

        _logger.LogInformation(
            "Deposit Schedule Handler Receive request Form Id:{request.FormId.ToGuid()}, "
                + "Schedule Time:{request.ScheduleTime.ToDateTime():yyyy/MM/dd HH:mm:ss}",
            request.FormId.ToGuid(),
            request.ScheduleTime.ToDateTime());

        var formId = request.FormId.ToGuid();

        try
        {
            var depositForm = await _paymentServiceClient
                .GetDepositFormAsync(formId, cancellationToken)
                .ConfigureAwait(false);

            if (depositForm is null)
            {
                _logger.LogWarning("Deposit Form {request.FormId.ToGuid()} not found.", request.FormId.ToGuid());
                return;
            }

            var brandId = depositForm.BrandId;

            _logger.LogInformation(
                "GetDepositFormAsync Form Id:{depositForm.Id}, Brand Id:{brandId}, Status:{depositForm.Status}",
            depositForm.Id,
            brandId,
            depositForm.Status);

            if (depositForm.Status >= PaymentFlowState.Reject)
                return;
            var applyInfo = JsonSerializer.Deserialize<UBPayDepositApplyInfo>(depositForm.ApplyInfo!, UBPayUtility.SerializerOptions)!;
            var result = await _uBPayClient.QueryDepositFormStatusAsync(
                applyInfo.Settings.ApiToken,
                formId,
                cancellationToken).ConfigureAwait(false);

            var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

            if (result.State is "timeout")
            {
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await _paymentClient
                        .RejectDepositAsync(formId, brandId, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else if (result?.State is "processing")
                await _paymentClient.ScheduleAsync(
                    "tgs.payment.ubpay.deposit.schedule.reject",
                    formId,
                    DateTime.UtcNow.AddMinutes(10D),
                    cancellationToken).ConfigureAwait(false);
            else
                _logger.LogWarning("Query Deposit Form Status formId:{formId}, state:{state}", formId, result?.State);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Process deposit timeout error!");

            await _paymentClient.ScheduleAsync(
                "tgs.payment.ubpay.deposit.schedule.reject",
                formId,
                DateTime.UtcNow.AddMinutes(10D),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
