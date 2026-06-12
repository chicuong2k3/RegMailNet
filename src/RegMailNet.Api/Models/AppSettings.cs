using RegMailNet;

namespace RegMailNet.Api.Models;

public class AppSettings
{
    public int Id { get; set; }
    public string Browser { get; set; } = "firefox";
    public string DefaultSmsService { get; set; } = SmsServiceConstants.SmsPool;
    public string CapsolverKey { get; set; } = string.Empty;
    public string NopechaKey { get; set; } = string.Empty;
    public string SmsPoolToken { get; set; } = string.Empty;
    public string FiveSimToken { get; set; } = string.Empty;
    public string GetsmsCodeUser { get; set; } = string.Empty;
    public string GetsmsCodeToken { get; set; } = string.Empty;
    public List<string> Proxies { get; set; } = [];
}
