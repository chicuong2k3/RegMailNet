using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace RegMailNet.CaptchaSolvers;

public class CapsolverExtension : ICaptchaSolver
{
    public string Name => "capsolver";

    public void ConfigureChromeExtension(ChromeOptions options, string extensionBasePath)
    {
        var extPath = Path.Combine(extensionBasePath, "capsolver-chrome-extension");
        options.AddArgument($"--load-extension={extPath}");
    }

    public void ConfigureFirefoxExtension(FirefoxDriver driver, string extensionBasePath)
    {
        var xpiPath = Path.Combine(extensionBasePath, "capsolver_captcha_solver-1.10.4.xpi");
        driver.InstallAddOn(xpiPath);

        driver.Navigate().GoToUrl("https://www.google.com");
        var capsolverSrc = driver.FindElement(By.XPath("/html/script[2]")).GetAttribute("src");
        var capsolverExtId = capsolverSrc.Split('/')[2];
        driver.Navigate().GoToUrl($"moz-extension://{capsolverExtId}/www/index.html#/popup");
        Thread.Sleep(5000);

        var apiKeyInput = driver.FindElement(By.XPath("//input[@placeholder=\"Please input your API key\"]"));
        apiKeyInput.SendKeys(driver.ToString() ?? string.Empty);
    }

    public void ConfigureApiKey(string extensionBasePath, string apiKey)
    {
        var configPath = Path.Combine(extensionBasePath, "capsolver-chrome-extension", "assets", "config.js");
        if (!File.Exists(configPath)) return;

        var content = File.ReadAllText(configPath);
        var updated = Regex.Replace(content, @"apiKey:\s*'[^']*'", $"apiKey: '{apiKey}'");
        File.WriteAllText(configPath, updated);
    }

    public void PostDriverInit(IWebDriver driver, string apiKey)
    {
        // No-op for capsolver
    }
}
