using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.UBPay;

public class UBPayClient
{
    private readonly HttpClient _httpClient;
    private readonly UBPayHasher _hasher;
    private readonly ILogger<UBPayClient> _logger;

    public UBPayClient(
        HttpClient httpClient,
        UBPayHasher hasher,
        ILogger<UBPayClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<UBPayClientDepositResponse> DepositAsync(string apiToken, DepositOrder order, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var dict = new Dictionary<string, string?>
        {
            ["amount"] = order.Amount.ToString(),
            ["callback_url"] = order.CallbackUrl.AbsoluteUri,
            ["out_trade_no"] = order.FormId.ToString(),
            ["firm_unique_id"] = order.FirmUniqueId,
        };

        if (order.Currency is not null)
            dict.Add("currency", order.Currency);

        var response = await _httpClient
            .PostAsJsonAsync("/api/transaction", dict, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("UBPayClient Deposit Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Deposit response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException(string.Format("message:{0}, errors:{1}", node["message"]!.ToString(), node["errors"]!.ToString()));

        var data = node["data"]!;

        return new UBPayClientDepositResponse(
            Amount: data["amount"]!.GetValue<decimal>(),
            FormId: data["out_trade_no"]!.GetValue<Guid>(),
            ThirdPartyOrderNo: data["trade_no"]!.GetValue<string>(),
            RedirectUrl: new Uri(data["uri"]!.GetValue<string>()));
    }

    public async ValueTask<UBPayClientQueryDepositResponse> QueryDepositFormStatusAsync(string apiToken, Guid formId, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var response = await _httpClient
            .GetAsync($"/api/transaction/{formId}", cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("UBPayClient Query Deposit Form Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Deposit response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException(node["message"]!.ToString());

        var data = node["data"]!;

        return new UBPayClientQueryDepositResponse(
            State: data["state"]!.GetValue<string>());
    }

    public async ValueTask<UBPayClientWithdrawResponse> WithdrawAsync(string apiToken, string notifyToken, WithdrawOrder order, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var dict = new Dictionary<string, string?>
        {
            ["out_trade_no"] = order.FormId.ToString(),
            ["amount"] = order.Amount.ToString(),
            ["callback_url"] = order.CallbackUrl.AbsoluteUri,
            ["address"] = order.Address,
        };

        if (order.Currency is not null)
            dict.Add("currency", order.Currency);

        dict["sign"] = _hasher.ComputeHash(apiToken, notifyToken, dict);

        var response = await _httpClient
            .PostAsJsonAsync("/api/payment", dict, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("UBPayClient Withdraw Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException(string.Format("message:{0}, errors:{1}", node["message"]!.ToString(), node["errors"]!.ToString()));

        var data = node["data"]!;

        return new UBPayClientWithdrawResponse(
            FormId: data["out_trade_no"]!.GetValue<Guid>(),
            ThirdPartyOrderNo: data["trade_no"]!.GetValue<string>(),
            Amount: data["amount"]!.GetValue<decimal>());
    }

    public async ValueTask<decimal> QueryBalanceAsync(string apiToken, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var response = await _httpClient
            .GetAsync("/api/balance/inquiry", cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("UBPayClient Query Merchant Balance Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Merchant Balance response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException("Query Merchant Balance failed");

        var data = node["data"]!;

        return decimal.Parse(data["balance"]!.GetValue<string>());
    }

    public async ValueTask<UBPayClientCreateWalletResponse> CreatePlayerWalletAsync(string apiToken, Guid FormId, Uri CallbackUrl, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var dict = new Dictionary<string, string?>
        {
            ["firm_unique_id"] = FormId.ToString(),
            ["callback_url"] = CallbackUrl.AbsoluteUri,
        };

        var response = await _httpClient
            .PostAsJsonAsync("/api/player-wallet/register", dict, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("UBPayClient Create Player Wallet Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Create Player Wallet response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException(string.Format("message:{0}, errors:{1}", node["message"]!.ToString(), node["errors"]!.ToString()));

        var data = node["data"]!;

        return new UBPayClientCreateWalletResponse(
            FirmUniqueId: data["firm_unique_id"]!.GetValue<string>(),
            WalletAddress: data["wallet_address"]!.GetValue<string>());
    }
}
