using System.Text.Json;
using RegMailNet.Api.Models;

namespace RegMailNet.Api.Services;

public class FileSettingsService
{
    private readonly string _filePath = Path.Combine("data", "settings.json");
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
