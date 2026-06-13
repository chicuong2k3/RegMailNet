using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RegMailNet.Browser;
using RegMailNet.CaptchaSolvers;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

namespace RegMailNet.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRegMailNet(this IServiceCollection services, IConfiguration configuration, Action<RegMailNetOptions>? configureDefaults = null)
    {
        services.Configure<RegMailNetOptions>(options =>
        {
            configuration.GetSection(RegMailNetOptions.SectionName).Bind(options);
            configureDefaults?.Invoke(options);
        });

        services.AddHttpClient();
        services.AddSingleton<DataGenerator>();
        services.AddSingleton<IBrowserFactory, CamoufoxBrowserFactory>();
        services.AddSingleton<IFreeProxyService, FreeProxyService>();

        // Register PxSolverService if configured
        services.AddSingleton<PxSolverService>(sp =>
        {
            var apiUrl = Environment.GetEnvironmentVariable("PX_SOLVER_URL");
            var apiKey = Environment.GetEnvironmentVariable("PX_SOLVER_API_KEY");

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
            {
                // Return a disabled instance
                return new PxSolverService("http://localhost:0", "disabled",
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<PxSolverService>.Instance);
            }

            return new PxSolverService(apiUrl, apiKey,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PxSolverService>.Instance);
        });

        services.AddSingleton<OutlookProvider>(sp =>
        {
            var pxSolver = sp.GetRequiredService<PxSolverService>();
            return new OutlookProvider(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<OutlookProvider>.Instance,
                pxSolver);
        });

        services.AddSingleton<GmailProvider>();
        services.AddSingleton<YahooProvider>();

        services.AddSingleton<ISmsServiceFactory, SmsServiceFactory>();

        return services;
    }
}
