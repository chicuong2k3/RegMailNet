using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace RegMailNet.CaptchaSolvers;

public class NopechaExtension : ICaptchaSolver
{
    public string Name => "nopecha";

    public void ConfigureChromeExtension(ChromeOptions options, string extensionBasePath)
    {
        var extPath = Path.Combine(extensionBasePath, "NopeCHA-CAPTCHA-Solver");
        options.AddArgument($"--load-extension={extPath}");
    }

    public void ConfigureFirefoxExtension(FirefoxDriver driver, string extensionBasePath)
    {
        var xpiPath = Path.Combine(extensionBasePath, "noptcha-0.4.9.xpi");
        driver.InstallAddOn(xpiPath);
    }

    public void ConfigureApiKey(string extensionBasePath, string apiKey)
    {
        // No-op for nopecha (key set via URL)
    }

    public void PostDriverInit(IWebDriver driver, string apiKey)
    {
        driver.Navigate().GoToUrl($"https://nopecha.com/setup#{apiKey}");
    }
}
