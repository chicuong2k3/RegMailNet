using System.Text.Json;

namespace RegMailNet.Ui.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RegMailNet", "settings.json");

    public AppSettings Settings { get; private set; } = new();

    public void Load()
    {
        if (File.Exists(SettingsPath))
        {
            var json = File.ReadAllText(SettingsPath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}

public class AppSettings
{
    public string Browser { get; set; } = "firefox";
    public string DefaultSmsService { get; set; } = "smspool";
    public string CapsolverKey { get; set; } = "";
    public string NopechaKey { get; set; } = "";
    public string SmsPoolToken { get; set; } = "";
    public string FiveSimToken { get; set; } = "";
    public string GetsmsCodeUser { get; set; } = "";
    public string GetsmsCodeToken { get; set; } = "";
    public List<string> Proxies { get; set; } = new();
}