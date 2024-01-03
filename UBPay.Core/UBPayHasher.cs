using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.UBPay;
public class UBPayHasher
{
    private readonly ILogger<UBPayHasher> _logger;

    public UBPayHasher(ILogger<UBPayHasher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ComputeHash(string apiToken, string notiyfyToken, IEnumerable<KeyValuePair<string, string?>> dict)
    {
        var content = string.Join(
            "&",
            dict.Where(kvp => string.IsNullOrWhiteSpace(kvp.Value) is false && kvp.Key != "sign")
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}"));

        var fullText = $"{content}{apiToken}{notiyfyToken}";

        _logger.LogTrace("FullText: {fullText}", fullText);

        var data = MD5.HashData(Encoding.UTF8.GetBytes(fullText));

        return Convert.ToHexString(data).ToLower();
    }
}
