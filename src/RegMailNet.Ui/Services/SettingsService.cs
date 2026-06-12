using System.Net.Http.Json;
using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly HttpClient _http;

    public SettingsService(HttpClient http)
    {
        _http = http;
        Settings = new AppSettings();
    }

    public AppSettings Settings { get; private set; }

    public async Task<AppSettings> GetSettingsAsync()
    {
        var result = await _http.GetFromJsonAsync<AppSettings>("api/settings");
        Settings = result ?? new AppSettings();
        return Settings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        Settings = settings;
        var response = await _http.PutAsJsonAsync("api/settings", new { Settings = settings });
        response.EnsureSuccessStatusCode();
    }

    public async Task Load()
    {
        try
        {
            await GetSettingsAsync();
        }
        catch
        {
            Settings = new AppSettings();
        }
    }

    public async Task Save()
    {
        await SaveSettingsAsync(Settings);
    }
}
