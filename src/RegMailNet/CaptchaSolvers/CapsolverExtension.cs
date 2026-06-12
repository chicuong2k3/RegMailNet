using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace RegMailNet.CaptchaSolvers;

public class CapsolverExtension : ICaptchaSolver
{
    public string Name => CaptchaSolver.Capsolver.ToValue();

    public async Task ConfigureAsync(IBrowserContext context, string extensionBasePath, string apiKey)
    {
        var xpiPath = Path.Combine(extensionBasePath, "capsolver_captcha_solver-1.10.4.xpi");
        if (!File.Exists(xpiPath))
            throw new FileNotFoundException($"Capsolver extension not found: {xpiPath}");

        await context.AddInitScriptAsync($@"
            // Pre-configure capsolver API key
            window.addEventListener('load', () => {{
                const input = document.querySelector('input[placeholder=""Please input your API key""]');
                if (input) {{
                    input.value = '{apiKey}';
                    input.dispatchEvent(new Event('input', {{ bubbles: true }}));
                }}
            }});
        ");

        // Note: Firefox .xpi installation is handled at browser launch time via args,
        // not at context level. The extension path should be passed to the browser factory.
        // For now, we configure via init script as a fallback.
    }
}
