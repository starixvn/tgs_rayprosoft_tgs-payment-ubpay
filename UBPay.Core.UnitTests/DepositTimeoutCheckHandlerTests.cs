using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.UBPay;
using Tgs.Payment.UBPay.Client;
using Tgs.Payment.UBPay.MessageQueue;

namespace UBPay.Core.UnitTests;

public class DepositTimeoutCheckHandlerTests
{
    [Fact]
    public async Task 檢查已經申請六十一分鐘的單子是否已經完成()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://ubtxn.com");
        var fakeUBPayClient = new UBPayClient(httpClient, new UBPayHasher(NullLogger<UBPayHasher>.Instance), NullLogger<UBPayClient>.Instance);
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        var sut = new DepositTimeoutCheckHandler(fakePaymentServiceClient, fakeUBPayClient, fakePaymentClient, NullLogger<DepositTimeoutCheckHandler>.Instance);

        var formId = Guid.Parse("5c224598-32d5-4db7-8a21-0f06862925bc");
        var brandId = Guid.Parse("4e86719f-243f-46f6-b010-5c758b7285af");
        var request = new ScheduleRequest
        {
            FormId = formId,
            Subject = "tgs.payment.ubpay.deposit.schedule.reject",
            ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var applyInfo = new UBPayDepositApplyInfo(
        new UBPayDepositSettings(
                "TMF23yDXxe9AkGVsXNUDhgXJALRBVcsz4s6WLT1Kkt2yA8Xj4hUDFrwHxqzZ",
                "TcBD1d4QPKNUppj0ncLLEEQCHXc60M7kOEfVb0S2fHziesGGYV1fTTjqsu81",
                formId,
                null),
            new UBPayDepositResponse(
                200000m,
                formId,
                new Uri("https://ubtxn.com/r/t/0d1dc4bb-8f1f-48e5-b241-395a1cb61baf"),
                "0d1dc4bb-8f1f-48e5-b241-395a1cb61baf"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new DepositFormInfo
            {
                Id = formId,
                Amount = 200000m,
                BrandId = brandId,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, UBPayUtility.SerializerOptions)
            });

        _ = mockHttp.When($"https://ubtxn.com/api/transaction/{formId}")
            .Respond(new StringContent("""
				{
				    "data": {
				        "trade_no": "0d1dc4bb-8f1f-48e5-b241-395a1cb61baf",
				        "out_trade_no": "5c224598-32d5-4db7-8a21-0f06862925bc",
				        "amount": 200000,
				        "request_amount": 200000,
				        "state": "timeout"
				    },
				    "success": true
				}
				"""));

        // Act
        await sut.HandleAsync(request.ToByteArray());

        // Assert
        _ = fakePaymentClient
            .Received(1)
            .RejectDepositAsync(Arg.Is(formId), Arg.Is(brandId));
    }
}
