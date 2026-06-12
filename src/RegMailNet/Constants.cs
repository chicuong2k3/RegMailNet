using System.ComponentModel;

namespace RegMailNet;

/// <summary>
/// Supported email providers.
/// </summary>
public enum EmailProvider
{
    [Description("outlook")]
    Outlook,

    [Description("gmail")]
    Gmail,

    [Description("yahoo")]
    Yahoo,

    /// <summary>
    /// Used for Google SMS service lookup (SmsServiceFactory).
    /// </summary>
    [Description("google")]
    Google
}

/// <summary>
/// Supported captcha solving services.
/// </summary>
public enum CaptchaSolver
{
    [Description("capsolver")]
    Capsolver,

    [Description("nopecha")]
    Nopecha
}

/// <summary>
/// Supported SMS verification services.
/// </summary>
public enum SmsService
{
    [Description("smspool")]
    SmsPool,

    [Description("getsmscode")]
    GetsmsCode,

    [Description("5sim")]
    FiveSim
}

/// <summary>
/// Account creation status.
/// </summary>
public enum AccountStatus
{
    Created,
    Failed
}

/// <summary>
/// String constants for SMS service names (usable in switch cases).
/// </summary>
public static class SmsServiceConstants
{
    public const string SmsPool = "smspool";
    public const string GetsmsCode = "getsmscode";
    public const string FiveSim = "5sim";
}

/// <summary>
/// String constants for email provider names (usable in switch cases).
/// </summary>
public static class EmailProviderConstants
{
    public const string Outlook = "outlook";
    public const string Gmail = "gmail";
    public const string Yahoo = "yahoo";
    public const string Google = "google";
}

/// <summary>
/// String constants for captcha solver names (usable in switch cases).
/// </summary>
public static class CaptchaSolverConstants
{
    public const string Capsolver = "capsolver";
    public const string Nopecha = "nopecha";
}

public static class EnumExtensions
{
    public static string ToValue(this EmailProvider provider) => provider switch
    {
        EmailProvider.Outlook => "outlook",
        EmailProvider.Gmail => "gmail",
        EmailProvider.Yahoo => "yahoo",
        EmailProvider.Google => "google",
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };

    public static string ToValue(this CaptchaSolver solver) => solver switch
    {
        CaptchaSolver.Capsolver => "capsolver",
        CaptchaSolver.Nopecha => "nopecha",
        _ => throw new ArgumentOutOfRangeException(nameof(solver))
    };

    public static string ToValue(this SmsService service) => service switch
    {
        SmsService.SmsPool => "smspool",
        SmsService.GetsmsCode => "getsmscode",
        SmsService.FiveSim => "5sim",
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    public static string ToValue(this AccountStatus status) => status switch
    {
        AccountStatus.Created => "Created",
        AccountStatus.Failed => "Failed",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
