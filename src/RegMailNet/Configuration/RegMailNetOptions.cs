namespace RegMailNet.Configuration;

public class RegMailNetOptions
{
    public const string SectionName = "RegMailNet";

    public List<string> CaptchaServicesSupported { get; set; } = new();
    public string DefaultCaptchaService { get; set; } = string.Empty;
    public List<string> SmsServicesSupported { get; set; } = new();
    public string DefaultSmsService { get; set; } = string.Empty;
    public List<string> SupportedBrowsers { get; set; } = new();
    public List<CaptchaSolverMapping> SupportedSolversByEmail { get; set; } = new();
}

public class CaptchaSolverMapping
{
    public string EmailService { get; set; } = string.Empty;
    public List<string> Solvers { get; set; } = new();
}
