using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.UBPay.MessageQueue;

internal class DepositTimeoutCheckHandler(
    IPaymentServiceClient paymentServiceClient,
    UBPayClient uBPayClient,
    IPaymentClient paymentClient,
    ILogger<DepositTimeoutCheckHandler> logger)
        : IMessageHandler<ScheduleRequest>
{
    public async ValueTask HandleAsync(
        string subject,
        ScheduleRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Deposit Schedule Handler Receive request Form Id:{request.FormId.ToGuid()}, "
                + "Schedule Time:{request.ScheduleTime.ToDateTime():yyyy/MM/dd HH:mm:ss}",
            data.FormId.ToGuid(),
            data.ScheduleTime.ToDateTime());

        var formId = data.FormId.ToGuid();

        try
        {
            var depositForm = await paymentServiceClient
                .GetDepositFormAsync(formId, cancellationToken)
                .ConfigureAwait(false);

            if (depositForm is null)
            {
                logger.LogWarning("Deposit Form {request.FormId.ToGuid()} not found.", data.FormId.ToGuid());
                return;
            }

            var brandId = depositForm.BrandId;

            logger.LogInformation(
                "GetDepositFormAsync Form Id:{depositForm.Id}, Brand Id:{brandId}, Status:{depositForm.Status}",
            depositForm.Id,
            brandId,
            depositForm.Status);

            if (depositForm.Status >= PaymentFlowState.Reject)
                return;
            var applyInfo = JsonSerializer.Deserialize<UBPayDepositApplyInfo>(depositForm.ApplyInfo!, UBPayUtility.SerializerOptions)!;
            var result = await uBPayClient.QueryDepositFormStatusAsync(
                applyInfo.Settings.ApiToken,
                formId,
                cancellationToken).ConfigureAwait(false);

            var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

            if (result.State is "timeout")
            {
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentClient
                        .RejectDepositAsync(formId, brandId, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else if (result?.State is "processing")
                await paymentClient.ScheduleAsync(
                    "tgs.payment.ubpay.deposit.schedule.reject",
                    formId,
                    DateTime.UtcNow.AddMinutes(10D),
                    cancellationToken).ConfigureAwait(false);
            else
                logger.LogWarning("Query Deposit Form Status formId:{formId}, state:{state}", formId, result?.State);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Process deposit timeout error!");

            await paymentClient.ScheduleAsync(
                "tgs.payment.ubpay.deposit.schedule.reject",
                formId,
                DateTime.UtcNow.AddMinutes(10D),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
