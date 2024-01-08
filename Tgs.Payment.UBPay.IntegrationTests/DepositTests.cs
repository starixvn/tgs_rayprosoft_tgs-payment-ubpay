using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.UBPay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.UBPay.IntegrationTests;

public class DepositTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DepositTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代收回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = UBPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("5cb4ab68-e705-49ac-84b8-d7ff9abc8a30");
        var brandId = Guid.NewGuid();
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var request = """
            {
                "trade_no": "84e857ca-4d67-495f-a59b-d76a42c87d7a",
                "amount": 200000,
                "out_trade_no": "5cb4ab68-e705-49ac-84b8-d7ff9abc8a30",
                "state": "completed",
                "blockchain_rate": "7.16",
                "sign": "b81d36f6e7baecb3b928d9d8099d86b7"
            }
            """
        ;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var applyInfo = new UBPayDepositApplyInfo(
            new UBPayDepositSettings(
                apiToken,
                notifyToken,
                formId,
                null,
                ExchangeRate: 1m),
             new UBPayDepositResponse(
                200000m,
                formId,
                new Uri("https://ubtxn.com/r/t/84e857ca-4d67-495f-a59b-d76a42c87d7a"),
                "84e857ca-4d67-495f-a59b-d76a42c87d7a"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.WaitPayment,
                Amount = 200000m,
                ThirdPartyOrderNo = "84e857ca-4d67-495f-a59b-d76a42c87d7a",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
            });

        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("ok", actual);

        var paymentReturn = new UBPayDepositReturn()
        {
            Status = "completed",
            FormId = formId,
            ThirdPartyOrderNo = "84e857ca-4d67-495f-a59b-d76a42c87d7a",
            OrderAmount = 200000m,
            Sign = "b81d36f6e7baecb3b928d9d8099d86b7",
            BlockchainRate = "7.16"            
        };

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, UBPayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .ApproveDepositAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Is(200000m),
                Arg.Any<CancellationToken>());
    }
}
