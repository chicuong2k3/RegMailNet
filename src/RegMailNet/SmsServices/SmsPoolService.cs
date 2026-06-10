using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RegMailNet.SmsServices;

public class SmsPoolService : ISmsService
{
    private readonly string _token;
    private readonly string _service;
    private readonly string _country;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SmsPoolService> _logger;
    private readonly string _apiUrl = "http://api.smspool.net/";

    public SmsPoolService(string service, string token, string country, HttpClient httpClient, ILogger<SmsPoolService> logger)
    {
        _token = token;
        _service = service;
        _country = country;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PhoneResult> GetPhoneAsync(bool sendPrefix = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting a phone number");

        var data = await RequestAsync("purchase/sms", new Dictionary<string, string>
        {
            ["country"] = _country,
            ["service"] = _service,
            ["pricing_option"] = "0"
        }, cancellationToken);

        var phoneNumber = sendPrefix
            ? data.GetProperty("number").GetString()!
            : data.GetProperty("phonenumber").GetString()!;
        var orderId = data.GetProperty("order_id").GetString()!;

        _logger.LogInformation("Got phone: {Phone}", phoneNumber);

        return new PhoneResult(phoneNumber, orderId);
    }

    public async Task<string> GetCodeAsync(string orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting the verification code");

        while (true)
        {
            try
            {
                var res = await RequestAsync("sms/check", new Dictionary<string, string>
                {
                    ["orderid"] = orderId
                }, cancellationToken);

                if (res.TryGetProperty("sms", out var sms) && sms.ValueKind == JsonValueKind.String)
                {
                    var code = sms.GetString()!;
                    _logger.LogInformation("Got code {Code}", code);
                    return code;
                }
            }
            catch (SmsServiceApiException)
            {
                _logger.LogInformation("Retrying...");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private async Task<JsonElement> RequestAsync(string cmd, Dictionary<string, string> kwargs, CancellationToken cancellationToken)
    {
        var queryParams = new Dictionary<string, string> { ["key"] = _token };
        foreach (var kv in kwargs)
        {
            queryParams[kv.Key] = kv.Value;
        }

        var url = _apiUrl + cmd + "?" + string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(cancellationToken));

        if (json.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False)
        {
            var message = json.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
            throw new SmsServiceApiException(message!);
        }

        if (json.TryGetProperty("status", out var status) && status.GetInt32() != 3)
        {
            var message = json.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
            throw new SmsServiceApiException(message!);
        }

        return json;
    }
}
