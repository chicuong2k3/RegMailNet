using Microsoft.Playwright;

namespace RegMailNet.CaptchaSolvers;

/// <summary>
/// Configures a captcha-solving browser extension on a Playwright browser context.
/// </summary>
public interface ICaptchaSolver
{
    string Name { get; }

    /// <summary>
    /// Install and configure the captcha extension on the given browser context.
    /// Must be called before navigating to pages that need captcha solving.
    /// </summary>
    Task ConfigureAsync(IBrowserContext context, string extensionBasePath, string apiKey);
}
