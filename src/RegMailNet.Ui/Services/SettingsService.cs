using System.Net.Http.Json;
using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly HttpClient _http;

    public SettingsService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        var result = await _http.GetFromJsonAsync<AppSettings>("api/settings");
        return result ?? new AppSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var response = await _http.PutAsJsonAsync("api/settings", new { Settings = settings });
        response.EnsureSuccessStatusCode();
    }
}
