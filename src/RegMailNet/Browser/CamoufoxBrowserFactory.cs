using System.Diagnostics;
using CamoufoxNet;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace RegMailNet.Browser;

/// <summary>
/// Creates anti-detect browser instances using CamoufoxNet.
/// Camoufox patches fingerprints at the C++ level (Firefox build), providing
/// stronger anti-detection than JS-injection approaches.
/// </summary>
public class CamoufoxBrowserFactory : IBrowserFactory
{
    private readonly ILogger<CamoufoxBrowserFactory> _logger;
    private static bool _ensured;

    public CamoufoxBrowserFactory(ILogger<CamoufoxBrowserFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ensures the Python camoufox package is installed and the browser binary is fetched.
    /// Safe to call multiple times — only runs once per process.
    /// </summary>
    public async Task EnsureInstalledAsync(CancellationToken cancellationToken = default)
    {
        if (_ensured) return;

        try
        {
            // Check if camoufox is already importable
            var check = await RunProcessAsync("python", "-c \"import camoufox\"", cancellationToken);
            if (check.ExitCode == 0)
            {
                _logger.LogInformation("camoufox Python package is already installed");
                _ensured = true;
                return;
            }
        }
        catch { /* python not found or other error — fall through to install */ }

        _logger.LogInformation("Installing camoufox Python package...");
        var install = await RunProcessAsync("pip", "install camoufox", cancellationToken);
        if (install.ExitCode != 0)
        {
            _logger.LogError("Failed to install camoufox: {Error}", install.Error);
            throw new InvalidOperationException(
                $"Failed to install camoufox Python package. Install manually with: pip install camoufox\n{install.Error}");
        }

        _logger.LogInformation("Fetching camoufox browser binary...");
        var fetch = await RunProcessAsync("python", "-m camoufox fetch", cancellationToken);
        if (fetch.ExitCode != 0)
        {
            _logger.LogError("Failed to fetch camoufox browser: {Error}", fetch.Error);
            throw new InvalidOperationException(
                $"Failed to fetch camoufox browser binary. Run manually: camoufox fetch\n{fetch.Error}");
        }

        _logger.LogInformation("camoufox installed and browser fetched successfully");
        _ensured = true;
    }

    public async Task<BrowserPage> CreatePageAsync(
        bool headless = true,
        string? proxy = null,
        bool humanize = true,
        bool blockWebRtc = true,
        string? captchaApiKey = null)
    {
        await EnsureInstalledAsync();

        _logger.LogInformation("Launching Camoufox browser (headless={Headless}, proxy={Proxy}, humanize={Humanize})",
            headless, proxy ?? "none", humanize);

        ProxyOptions? proxyOptions = null;
        if (!string.IsNullOrEmpty(proxy))
        {
            var parsed = ParseProxy(proxy);
            proxyOptions = new ProxyOptions
            {
                Server = $"{parsed.Scheme}://{parsed.Host}:{parsed.Port}",
                Username = parsed.Username,
                Password = parsed.Password,
            };
        }

        // Install Nopecha extension if API key is provided
        List<string>? addons = null;
        if (!string.IsNullOrEmpty(captchaApiKey))
        {
            // Camoufox requires extracted addon directory with manifest.json
            var extensionDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captcha_solvers", "nopecha");
            if (Directory.Exists(extensionDir) && File.Exists(Path.Combine(extensionDir, "manifest.json")))
            {
                _logger.LogInformation("Installing Nopecha extension from {Path}", extensionDir);
                addons = [extensionDir];
            }
            else
            {
                _logger.LogWarning("Nopecha extension not found at: {Path}", extensionDir);
            }
        }

        var options = new CamoufoxOptions
        {
            Headless = headless,
            Humanize = humanize,
            BlockWebRtc = blockWebRtc,
            BlockImages = false,
            Window = (1280, 900),
            Locales = ["en-US", "en"],
            Proxy = proxyOptions,
            Addons = addons,
        };

        var browser = await Camoufox.CreateAsync(options);
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // Configure Nopecha extension via setup page
        if (!string.IsNullOrEmpty(captchaApiKey))
        {
            try
            {
                _logger.LogInformation("Configuring Nopecha extension with API key");
                await page.GotoAsync($"https://nopecha.com/setup#{captchaApiKey}", new PageGotoOptions { Timeout = 15000 });
                await Task.Delay(3000);
                _logger.LogInformation("Nopecha extension configured");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to configure Nopecha extension (non-fatal)");
            }
        }

        _logger.LogInformation("Camoufox browser launched successfully");
        return new BrowserPage(page, context, browser);
    }

    private static (string Scheme, string Host, int Port, string? Username, string? Password) ParseProxy(string proxy)
    {
        var uri = new Uri(proxy);
        // Handle username:password in UserInfo
        string? username = null;
        string? password = null;
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2); // Split into max 2 parts
            username = parts[0];
            password = parts.Length > 1 ? parts[1] : null;
        }
        return (uri.Scheme, uri.Host, uri.Port, username, password);
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(
        string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, stdout, stderr);
    }
}
