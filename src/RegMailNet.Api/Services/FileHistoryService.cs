using System.Text.Json;
using RegMailNet.Api.Models;

namespace RegMailNet.Api.Services;

public class FileHistoryService
{
    private readonly string _filePath = Path.Combine("data", "history.json");
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<List<AccountHistoryEntry>> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<AccountHistoryEntry>>(json) ?? [];
    }

    public async Task AddAsync(AccountHistoryEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await LoadAsync();
            entries.Insert(0, entry);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var json = JsonSerializer.Serialize(entries, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _lock.Release();
        }
    }
}
