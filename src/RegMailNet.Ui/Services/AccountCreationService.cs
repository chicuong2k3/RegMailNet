using System.Net.Http.Json;

namespace RegMailNet.Ui.Services;

public sealed class AccountCreationService : IAccountCreationService
{
    private readonly HttpClient _http;

    public AccountCreationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AccountCreatedResult> CreateOutlookAsync(bool useProxy)
        => await CreateAccountAsync("api/accounts/outlook", useProxy);

    public async Task<AccountCreatedResult> CreateGmailAsync(bool useProxy)
        => await CreateAccountAsync("api/accounts/gmail", useProxy);

    public async Task<AccountCreatedResult> CreateYahooAsync(bool useProxy)
        => await CreateAccountAsync("api/accounts/yahoo", useProxy);

    private async Task<AccountCreatedResult> CreateAccountAsync(string url, bool useProxy)
    {
        var response = await _http.PostAsJsonAsync(url, new { UseProxy = useProxy });

        if (!response.IsSuccessStatusCode)
        {
            return new AccountCreatedResult
            {
                Provider = url.Split('/').Last(),
                Success = false
            };
        }

        var result = await response.Content.ReadFromJsonAsync<AccountCreatedResult>();
        return result with { Success = true };
    }
}
