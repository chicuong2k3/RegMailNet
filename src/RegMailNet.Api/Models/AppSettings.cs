namespace RegMailNet.Api.Models;

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
    public List<string> Proxies { get; set; } = [];
}
