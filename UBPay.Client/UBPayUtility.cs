using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.UBPay.Client;

public static class UBPayUtility
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = {
        }
    };
}