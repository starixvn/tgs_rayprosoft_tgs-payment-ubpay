using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.UBPay.ApplicationServices;
using Tgs.Payment.UBPay.Client;
using Tgs.Payment.UBPay.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Payment.UBPay.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController : ControllerBase
{
    private readonly ILogger<DepositController> _logger;

    public DepositController(ILogger<DepositController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyDeposit")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Text.Plain)]
    public async Task<string> DepositCallbackAsync(
        [FromServices] UBPayHasher hasher,
        [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromBody] CallbackDepositViewModel source)
    {
        _logger.LogInformation("UBPay Deposit Callback:{source}", source);

        var formId = Guid.Parse(source.OutTradeNo);

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            _logger.LogWarning("Can't found deposit form({id}).", formId);
            return "fail";
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(source))!;

        var sign = source.Sign;
        var hash = hasher.ComputeHash(
            depositForm.ApplyInfo!.Settings.ApiToken,
            depositForm.ApplyInfo!.Settings.NotifyToken,
            dict.Select(kv => new KeyValuePair<string, string?>(
                kv.Key,
                kv.Value!.ToString())));

        _logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            _logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return "fail";
        }

        var paymentReturn = new UBPayDepositReturn()
        {
            Status = source.State,
            FormId = formId,
            ThirdPartyOrderNo = source.TradeNo,
            OrderAmount = source.Amount,
            Sign = sign,
            BlockchainRate = source.BlockchainRate,
        };

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.Status)
        {
            case "completed":
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                    depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            UBPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.ApproveDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        paymentReturn.OrderAmount,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            default:
                _logger.LogWarning("FormId:{formId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{status}",
                    formId,
                    paymentReturn.ThirdPartyOrderNo,
                    paymentReturn.Status);
                return "fail";
        }

        return "ok";
    }
}
