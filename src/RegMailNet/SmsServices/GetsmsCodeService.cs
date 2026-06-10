using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace RegMailNet.SmsServices;

public class GetsmsCodeService : ISmsService
{
    private static readonly Dictionary<string, string> Prefixes = new()
    {
        ["us"] = "1",
        ["hk"] = "852"
    };

    private static readonly Regex CodePattern = new(@"([0-9]{5,6})", RegexOptions.Compiled);

    private readonly string _user;
    private readonly string _token;
    private readonly string _project;
    private readonly string _country;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetsmsCodeService> _logger;
    private readonly string _apiUrl;

    public GetsmsCodeService(string project, string user, string token, string country, HttpClient httpClient, ILogger<GetsmsCodeService> logger)
    {
        _user = user;
        _token = token;
        _project = project;
        _country = country;
        _httpClient = httpClient;
        _logger = logger;
        _apiUrl = $"http://api.getsmscode.com/{GetEndpoint(country)}.php";
    }

    public async Task<PhoneResult> GetPhoneAsync(bool sendPrefix = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting a phone number");

        var data = await RequestAsync(new Dictionary<string, string> { ["action"] = "getmobile" }, cancellationToken);

        _logger.LogInformation("Got phone: {Phone}", data);

        if (!sendPrefix && Prefixes.TryGetValue(_country, out var prefix))
        {
            data = data.TrimStart(prefix.ToCharArray());
        }

        return new PhoneResult(data.Trim());
    }

    public async Task<string> GetCodeAsync(string phone, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting the code with phone {Phone}", phone);

        while (true)
        {
            try
            {
                var text = await RequestAsync(new Dictionary<string, string>
                {
                    ["action"] = "getsms",
                    ["mobile"] = phone
                }, cancellationToken);

                var parts = text.Split('|');
                if (parts.Length > 1)
                {
                    var match = CodePattern.Match(parts[1]);
                    if (match.Success)
                    {
                        _logger.LogInformation("Got code {Code}", match.Groups[1].Value);
                        return match.Groups[1].Value;
                    }
                }
            }
            catch (SmsServiceApiException)
            {
                _logger.LogInformation("Retrying...");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private async Task<string> RequestAsync(Dictionary<string, string> kwargs, CancellationToken cancellationToken)
    {
        var formData = new Dictionary<string, string>
        {
            ["username"] = _user,
            ["token"] = _token,
            ["pid"] = _project
        };

        if (_country is not ("us" or "cn"))
        {
            formData["cocode"] = _country;
        }

        foreach (var kv in kwargs)
        {
            formData[kv.Key] = kv.Value;
        }

        var response = await _httpClient.PostAsync(_apiUrl, new FormUrlEncodedContent(formData), cancellationToken);
        response.EnsureSuccessStatusCode();

        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (text.Contains("Message"))
            throw new SmsServiceApiException(text);

        return text;
    }

    private static string GetEndpoint(string ccode) => ccode switch
    {
        "hk" => "vndo",
        "us" => "usdo",
        _ => "do"
    };
}
