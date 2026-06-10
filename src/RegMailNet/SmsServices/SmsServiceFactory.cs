using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RegMailNet.SmsServices;

public class SmsServiceFactory : ISmsServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmsServiceFactory> _logger;

    public SmsServiceFactory(IServiceProvider serviceProvider, ILogger<SmsServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ISmsService Create(Dictionary<string, string> smsData, string emailProvider)
    {
        var serviceName = smsData["name"];
        var data = smsData["data"];

        // Parse the data string as key=value pairs
        var dataDict = ParseDataString(data);

        var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();

        return serviceName switch
        {
            "getsmscode" => CreateGetsmsCode(dataDict, emailProvider, httpClient),
            "smspool" => CreateSmsPool(dataDict, emailProvider, httpClient),
            "5sim" => CreateFiveSim(dataDict, emailProvider, httpClient),
            _ => throw new ArgumentException($"Unsupported SMS service: {serviceName}")
        };
    }

    private GetsmsCodeService CreateGetsmsCode(Dictionary<string, string> data, string emailProvider, HttpClient httpClient)
    {
        var project = emailProvider == "yahoo" ? "15" : "1";
        var logger = _serviceProvider.GetRequiredService<ILogger<GetsmsCodeService>>();
        return new GetsmsCodeService(
            project,
            data["user"],
            data["token"],
            data.GetValueOrDefault("country", "us"),
            httpClient,
            logger);
    }

    private SmsPoolService CreateSmsPool(Dictionary<string, string> data, string emailProvider, HttpClient httpClient)
    {
        var service = emailProvider == "yahoo" ? "1034" : "395";
        var logger = _serviceProvider.GetRequiredService<ILogger<SmsPoolService>>();
        return new SmsPoolService(
            service,
            data["token"],
            data.GetValueOrDefault("country", "1"),
            httpClient,
            logger);
    }

    private FiveSimService CreateFiveSim(Dictionary<string, string> data, string emailProvider, HttpClient httpClient)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<FiveSimService>>();
        return new FiveSimService(
            emailProvider,
            data["token"],
            data.GetValueOrDefault("country", "usa"),
            httpClient,
            logger);
    }

    private static Dictionary<string, string> ParseDataString(string data)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(data)) return result;

        foreach (var pair in data.Split(','))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                result[parts[0].Trim()] = parts[1].Trim();
            }
        }

        return result;
    }
}
