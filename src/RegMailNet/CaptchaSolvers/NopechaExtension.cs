using Microsoft.Playwright;

namespace RegMailNet.CaptchaSolvers;

public class NopechaExtension : ICaptchaSolver
{
    public string Name => CaptchaSolver.Nopecha.ToValue();

    public async Task ConfigureAsync(IBrowserContext context, string extensionBasePath, string apiKey)
    {
        var xpiPath = Path.Combine(extensionBasePath, "noptcha-0.4.9.xpi");
        if (!File.Exists(xpiPath))
            throw new FileNotFoundException($"Nopecha extension not found: {xpiPath}");

        // Nopecha configures via URL navigation to nopecha.com/setup#<apiKey>
        // This needs to happen after the browser is launched and a page is available.
        // The caller should navigate to the setup URL after installing the extension.
    }
}
