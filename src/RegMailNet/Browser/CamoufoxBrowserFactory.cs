using CamoufoxNet;
using Microsoft.Extensions.Logging;

namespace RegMailNet.Browser;

/// <summary>
/// Creates anti-detect browser instances using CamoufoxNet.
/// Camoufox patches fingerprints at the C++ level (Firefox build), providing
/// stronger anti-detection than JS-injection approaches.
/// </summary>
public class CamoufoxBrowserFactory : IBrowserFactory
{
    private readonly ILogger<CamoufoxBrowserFactory> _logger;

    public CamoufoxBrowserFactory(ILogger<CamoufoxBrowserFactory> logger)
    {
        _logger = logger;
    }

    public async Task<BrowserPage> CreatePageAsync(
        bool headless = true,
        string? proxy = null,
        bool humanize = true,
        bool blockWebRtc = true)
    {
        _logger.LogInformation("Launching Camoufox browser (headless={Headless}, proxy={Proxy}, humanize={Humanize})",
            headless, proxy ?? "none", humanize);

        ProxyOptions? proxyOptions = null;
        if (!string.IsNullOrEmpty(proxy))
        {
            var parsed = ParseProxy(proxy);
            proxyOptions = new ProxyOptions
            {
                Server = $"{parsed.Scheme}://{parsed.Host}:{parsed.Port}",
                Username = parsed.Username,
                Password = parsed.Password,
            };
        }

        var options = new CamoufoxOptions
        {
            Headless = headless,
            Humanize = humanize,
            BlockWebRtc = blockWebRtc,
            BlockImages = false,
            Window = (1280, 900),
            Locales = ["en-US", "en"],
            Proxy = proxyOptions,
        };

        var browser = await Camoufox.CreateAsync(options);
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        _logger.LogInformation("Camoufox browser launched successfully");
        return new BrowserPage(page, context, browser);
    }

    private static (string Scheme, string Host, int Port, string? Username, string? Password) ParseProxy(string proxy)
    {
        var uri = new Uri(proxy);
        var username = string.IsNullOrEmpty(uri.UserInfo) ? null : uri.UserInfo.Split(':').FirstOrDefault();
        var password = string.IsNullOrEmpty(uri.UserInfo) ? null : uri.UserInfo.Split(':').Skip(1).FirstOrDefault();
        return (uri.Scheme, uri.Host, uri.Port, username, password);
    }
}
