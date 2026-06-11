using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace RegMailNet.Utilities;

public interface IFreeProxyService
{
    Task<string?> GetProxyAsync(string[]? countryCodes = null, CancellationToken cancellationToken = default);
}

public partial class FreeProxyService : IFreeProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FreeProxyService> _logger;

    // Matches rows in the free-proxy-list.net HTML table
    [GeneratedRegex(@"<tr><td>(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})</td><td>(\d+)</td><td>([^<]*)</td><td class='hm'>([^<]*)</td><td>([^<]*)</td><td class='hm'>([^<]*)</td><td>([^<]*)</td><td class='hx'>([^<]*)</td><td class='hm'>([^<]*)</td></tr>")]
    private static partial Regex ProxyListRegex();

    public FreeProxyService(HttpClient httpClient, ILogger<FreeProxyService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> GetProxyAsync(string[]? countryCodes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching free proxies...");

            var proxies = await FetchProxiesAsync(cancellationToken);
            if (proxies.Count == 0)
            {
                _logger.LogWarning("No free proxies available.");
                return null;
            }

            if (countryCodes is { Length: > 0 })
            {
                var filtered = proxies
                    .Where(p => countryCodes.Contains(p.CountryCode, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (filtered.Count > 0)
                    proxies = filtered;
                else
                    _logger.LogWarning("No proxies found for specified countries, using unfiltered list.");
            }

            var random = proxies[Random.Shared.Next(proxies.Count)];
            var proxyUrl = $"http://{random.Ip}:{random.Port}";
            _logger.LogInformation("Selected proxy: {Proxy}", proxyUrl);
            return proxyUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch free proxies.");
            return null;
        }
    }

    private async Task<List<ProxyEntry>> FetchProxiesAsync(CancellationToken cancellationToken)
    {
        // Primary source: free-proxy-list.net
        var proxies = await FetchFromFreeProxyListAsync(cancellationToken);
        if (proxies.Count > 0) return proxies;

        // Fallback: sslproxies.org
        proxies = await FetchFromSslProxiesAsync(cancellationToken);
        return proxies;
    }

    private async Task<List<ProxyEntry>> FetchFromFreeProxyListAsync(CancellationToken cancellationToken)
    {
        var html = await _httpClient.GetStringAsync("https://free-proxy-list.net/", cancellationToken);
        return ParseProxyTable(html);
    }

    private async Task<List<ProxyEntry>> FetchFromSslProxiesAsync(CancellationToken cancellationToken)
    {
        var html = await _httpClient.GetStringAsync("https://www.sslproxies.org/", cancellationToken);
        return ParseProxyTable(html);
    }

    private static List<ProxyEntry> ParseProxyTable(string html)
    {
        var results = new List<ProxyEntry>();
        var regex = ProxyListRegex();

        foreach (Match match in regex.Matches(html))
        {
            if (match.Groups.Count >= 5
                && int.TryParse(match.Groups[2].Value, out var port))
            {
                results.Add(new ProxyEntry
                {
                    Ip = match.Groups[1].Value,
                    Port = port,
                    CountryCode = match.Groups[3].Value.Trim(),
                    Country = match.Groups[4].Value.Trim(),
                    IsHttps = match.Groups[5].Value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase)
                });
            }
        }

        return results;
    }

    private record ProxyEntry
    {
        public string Ip { get; init; } = "";
        public int Port { get; init; }
        public string CountryCode { get; init; } = "";
        public string Country { get; init; } = "";
        public bool IsHttps { get; init; }
    }
}
