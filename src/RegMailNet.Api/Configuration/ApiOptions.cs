namespace RegMailNet.Api.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public Dictionary<string, string> CaptchaKeys { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> SmsKeys { get; set; } = new();
    public List<string> Proxies { get; set; } = new();
    public bool AutoProxy { get; set; }
    public bool Headless { get; set; } = true;
}
