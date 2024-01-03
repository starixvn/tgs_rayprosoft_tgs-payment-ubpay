using System.Reflection;
using Tgs.Payment.UBPay;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUBPay(
        this IServiceCollection services,
        Action<UBPayOptions> configureOptions,
        Action<IHttpClientBuilder> ConfigureAiClient)
    {
        ConfigureAiClient?.Invoke(services.AddHttpClient<UBPayClient>());

        return services
            .AddOptions<UBPayOptions>()
            .Configure(configureOptions)
            .Services
            .RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
            .AddSingleton<UBPayHasher>();
    }
}