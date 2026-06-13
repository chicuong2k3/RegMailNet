using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace RegMailNet.CaptchaSolvers;

/// <summary>
/// Client for px-solver REST API that solves PerimeterX (Human Security) captcha.
/// Returns valid _px3 cookies that can be set in the browser.
/// </summary>
public class PxSolverService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PxSolverService> _logger;
    private readonly string _apiUrl;
    private readonly string _apiKey;

    public PxSolverService(string apiUrl, string apiKey, HttpClient httpClient, ILogger<PxSolverService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiUrl = apiUrl.TrimEnd('/');
        _apiKey = apiKey;
    }

    /// <summary>
    /// Solve PerimeterX captcha for a given URL and return cookies.
    /// </summary>
    public async Task<PxSolverResult?> SolveAsync(string url, string? proxy = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Solving PerimeterX captcha for {Url}", url);

            var request = new PxSolverRequest { Url = url, Proxy = proxy };
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/v1/solve");
            httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PxSolverResponse>(cancellationToken: cancellationToken);

            if (result?.Status == "solved" && result.Data != null)
            {
                _logger.LogInformation("PerimeterX captcha solved in {Ms}ms", result.Data.SolveMs);
                return result.Data;
            }

            _logger.LogWarning("px-solver returned status: {Status}", result?.Status);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to solve PerimeterX captcha");
            return null;
        }
    }
}

public class PxSolverRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("proxy")]
    public string? Proxy { get; set; }
}

public class PxSolverResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public PxSolverResult? Data { get; set; }
}

public class PxSolverResult
{
    [JsonPropertyName("user_agent")]
    public string UserAgent { get; set; } = string.Empty;

    [JsonPropertyName("solve_ms")]
    public int SolveMs { get; set; }

    [JsonPropertyName("cache_hit")]
    public bool CacheHit { get; set; }

    [JsonPropertyName("handler")]
    public string Handler { get; set; } = string.Empty;

    [JsonPropertyName("cookies")]
    public List<PxCookie> Cookies { get; set; } = [];

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }
}

public class PxCookie
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = "/";
}
