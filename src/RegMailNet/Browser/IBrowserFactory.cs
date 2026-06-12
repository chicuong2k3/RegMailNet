using Microsoft.Playwright;

namespace RegMailNet.Browser;

/// <summary>
/// Creates and configures browser instances via CamoufoxNet (anti-detect Firefox + Playwright).
/// </summary>
public interface IBrowserFactory
{
    /// <summary>
    /// Ensures the underlying browser runtime is installed and ready.
    /// Safe to call multiple times. No-op if already installed.
    /// </summary>
    Task EnsureInstalledAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Launches a new Camoufox browser context and returns a fresh page.
    /// </summary>
    /// <param name="headless">Run in headless mode (default: true).</param>
    /// <param name="proxy">Optional proxy URL, e.g. "http://user:pass@host:port".</param>
    /// <param name="humanize">Enable human-like mouse movement.</param>
    /// <param name="blockWebRtc">Block WebRTC to prevent IP leaks.</param>
    Task<BrowserPage> CreatePageAsync(
        bool headless = true,
        string? proxy = null,
        bool humanize = true,
        bool blockWebRtc = true);
}

/// <summary>
/// Wraps a Playwright page and its owning browser context for proper cleanup.
/// </summary>
public sealed class BrowserPage : IAsyncDisposable
{
    public IPage Page { get; }
    public IBrowserContext Context { get; }
    private readonly IAsyncDisposable? _browser;

    public BrowserPage(IPage page, IBrowserContext context, IAsyncDisposable? browser = null)
    {
        Page = page;
        Context = context;
        _browser = browser;
    }

    public async ValueTask DisposeAsync()
    {
        await Context.CloseAsync();
        if (_browser != null)
            await _browser.DisposeAsync();
    }
}
