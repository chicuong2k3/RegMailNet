using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public interface ISettingsService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}
