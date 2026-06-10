using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RegMailNet.SmsServices;

public class FiveSimService : ISmsService
{
    private static readonly Dictionary<string, string> Prefixes = new()
    {
        ["usa"] = "1"
    };

    private readonly string _token;
    private readonly string _service;
    private readonly string _country;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FiveSimService> _logger;
    private readonly string _apiUrl = "https://5sim.net/v1/user/";

    public FiveSimService(string service, string token, string country, HttpClient httpClient, ILogger<FiveSimService> logger)
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

        var cmd = $"buy/activation/{_country}/any/{_service}";
        var data = await RequestAsync(cmd, cancellationToken);

        var phoneNumber = data.GetProperty("phone").GetString()!.TrimStart('+');
        var orderId = data.GetProperty("id").GetInt64().ToString();

        _logger.LogInformation("Got phone: {Phone}", phoneNumber);

        if (!sendPrefix && Prefixes.TryGetValue(_country, out var prefix))
        {
            phoneNumber = phoneNumber.TrimStart(prefix.ToCharArray());
        }

        return new PhoneResult(phoneNumber, orderId);
    }

    public async Task<string> GetCodeAsync(string orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting the verification code");

        var cmd = $"/check/{orderId}";
        while (true)
        {
            var res = await RequestAsync(cmd, cancellationToken);

            var sms = res.GetProperty("sms");
            if (sms.ValueKind == JsonValueKind.Array && sms.GetArrayLength() > 0)
            {
                var code = sms[0].GetProperty("code").GetString()!;
                _logger.LogInformation("Got code {Code}", code);
                return code;
            }

            var status = res.GetProperty("status").GetString();
            if (status is "CANCELED" or "TIMEOUT" or "BANNED")
            {
                throw new SmsServiceApiException($"Error getting verification code, order status: {status}");
            }

            _logger.LogInformation("Retrying...");
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private async Task<JsonElement> RequestAsync(string cmd, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _apiUrl + cmd);
        request.Headers.Add("Authorization", $"Bearer {_token}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (text == "no free phones")
            throw new SmsServiceApiException("5Sim has no free phones");
        if (text == "not enough user balance")
            throw new SmsServiceApiException("Not enough balance");

        return JsonSerializer.Deserialize<JsonElement>(text);
    }
}
