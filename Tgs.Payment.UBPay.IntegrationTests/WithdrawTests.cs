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

public class WithdrawTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public WithdrawTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代付回調_訂單狀態成功()
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

        var formId = Guid.Parse("1c4e5fd7-460f-4d18-882c-c16046d693dc");
        var brandId = Guid.NewGuid();
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var settings = new UBPayWithdrawSettings(
            apiToken,
            notifyToken,
            formId,
            "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ",
            ExchangeRate: 1m);
        var withdrawResponse = new UBPayWithdrawResponse(formId);

        var applyInfo = new UBPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
            });

        var request = """
            {
                "trade_no": "9c61fe85-19e7-4e53-922c-f3c96b9097f7",
                "amount": 100,
                "out_trade_no": "1c4e5fd7-460f-4d18-882c-c16046d693dc",
                "state": "completed",
                "sign": "4afc59327f96a1799c46ff5339a9efc6"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        var paymentReturn = new UBPayWithdrawReturn()
        {
            FormId = formId,
            Amount = 100m,
            Sign = "4afc59327f96a1799c46ff5339a9efc6",
            ThirdPartyOrderNo = "9c61fe85-19e7-4e53-922c-f3c96b9097f7",
            Status = "completed",
            AfterExchangeRateAmount = 100m
        };

        Assert.Equal("ok", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, UBPayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .PaidWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態失敗()
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

        var formId = Guid.Parse("1011c749-d2f9-4e3c-8aa1-221b4718c63a");
        var brandId = Guid.NewGuid();
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var settings = new UBPayWithdrawSettings(
            apiToken,
            notifyToken,
            formId,
            "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ",
            ExchangeRate: 1m);
        var withdrawResponse = new UBPayWithdrawResponse(formId);

        var applyInfo = new UBPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
            });

        var request = """
            {
                "trade_no": "78330832-b465-41c1-8e59-3ce2f6e79ae9",
                "amount": 100,
                "out_trade_no": "1011c749-d2f9-4e3c-8aa1-221b4718c63a",
                "state": "failed",
                "sign": "6fe27cf80a7b0e082fe28d72359a149a"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        var paymentReturn = new UBPayWithdrawReturn()
        {
            FormId = formId,
            Amount = 100m,
            Sign = "6fe27cf80a7b0e082fe28d72359a149a",
            ThirdPartyOrderNo = "78330832-b465-41c1-8e59-3ce2f6e79ae9",
            Status = "failed",
            AfterExchangeRateAmount = 100m
        };

        Assert.Equal("ok", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, UBPayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態沖回()
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

        var formId = Guid.Parse("b5c10c19-f7dc-4bb9-adab-90242fa8287c");
        var brandId = Guid.NewGuid();
        var apiToken = "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ";
        var notifyToken = "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81";

        var settings = new UBPayWithdrawSettings(
            apiToken,
            notifyToken,
            formId,
            "TQN6nn7vzWUaV7HpR8FoABiCPUAN6qaDGQ",
            ExchangeRate: 1m);
        var withdrawResponse = new UBPayWithdrawResponse(formId);

        var applyInfo = new UBPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
            });

        var request = """
            {
                "trade_no": "300adb28-9aea-4861-8240-70acdda414e7",
                "amount": 100,
                "out_trade_no": "b5c10c19-f7dc-4bb9-adab-90242fa8287c",
                "state": "refund",
                "sign": "ac5731fd60adfe42df77efe29d7b606f"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        var paymentReturn = new UBPayWithdrawReturn()
        {
            FormId = formId,
            Amount = 100m,
            Sign = "ac5731fd60adfe42df77efe29d7b606f",
            ThirdPartyOrderNo = "300adb28-9aea-4861-8240-70acdda414e7",
            Status = "refund",
            AfterExchangeRateAmount = 100m
        };

        Assert.Equal("ok", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, UBPayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }
}
