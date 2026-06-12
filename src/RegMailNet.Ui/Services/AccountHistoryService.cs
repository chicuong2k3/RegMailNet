using System.Net.Http.Json;
using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public sealed class AccountHistoryService : IAccountHistoryService
{
    private readonly HttpClient _http;
    private readonly List<AccountHistoryEntry> _entries = [];

    public AccountHistoryService(HttpClient http)
    {
        _http = http;
    }

    public List<AccountHistoryEntry> Entries => _entries;

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

    public void Delete(int index)
    {
        if (index >= 0 && index < _entries.Count)
            _entries.RemoveAt(index);
    }

    public void ClearAll()
    {
        _entries.Clear();
    }

    public string ExportToCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Email,Password,Provider,Created,Status");
        foreach (var entry in _entries)
        {
            sb.AppendLine($"\"{entry.Email}\",\"{entry.Password}\",\"{entry.Provider}\",\"{entry.CreatedAt:yyyy-MM-dd HH:mm}\",\"{entry.Status}\"");
        }
        return sb.ToString();
    }

    public void Add(AccountCreatedResult result, string provider)
    {
        _entries.Add(new AccountHistoryEntry
        {
            Email = result.Email,
            Password = result.Password,
            Provider = provider,
            CreatedAt = result.CreatedAt,
            Status = result.Success ? "Created" : "Failed"
        });
    }

    public void Add(RegMailNet.EmailProviders.AccountCreationResult result, string provider)
    {
        _entries.Add(new AccountHistoryEntry
        {
            Email = result.Email,
            Password = result.Password,
            Provider = provider,
            CreatedAt = DateTime.Now,
            Status = "Created"
        });
    }

    public void AddFailure(string provider, string error)
    {
        _entries.Add(new AccountHistoryEntry
        {
            Provider = provider,
            CreatedAt = DateTime.Now,
            Status = $"Failed: {error}"
        });
    }
}
