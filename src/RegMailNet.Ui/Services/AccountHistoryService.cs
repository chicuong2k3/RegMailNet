using System.Net.Http.Json;
using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public sealed class AccountHistoryService : IAccountHistoryService
{
    private readonly HttpClient _http;

    public AccountHistoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<AccountHistoryEntry>> GetHistoryAsync()
    {
        var result = await _http.GetFromJsonAsync<HistoryResponse>("api/history");
        return result?.Entries ?? [];
    }

    public async Task AddAsync(AccountHistoryEntry entry)
    {
        var response = await _http.PostAsJsonAsync("api/history", new { Entry = entry });
        response.EnsureSuccessStatusCode();
    }

    private class HistoryResponse
    {
        public List<AccountHistoryEntry> Entries { get; set; } = [];
    }
}
