using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RegMailNet.Browser;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

namespace RegMailNet.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRegMailNet(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RegMailNetOptions>(configuration.GetSection(RegMailNetOptions.SectionName));

        services.AddSingleton<DataGenerator>();
        services.AddSingleton<IBrowserFactory, CamoufoxBrowserFactory>();
        services.AddSingleton<IFreeProxyService, FreeProxyService>();

        services.AddSingleton<OutlookProvider>();
        services.AddSingleton<GmailProvider>();
        services.AddSingleton<YahooProvider>();

        services.AddSingleton<ISmsServiceFactory, SmsServiceFactory>();

        return services;
    }
}
