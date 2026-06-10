using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;

namespace RegMailNet.CaptchaSolvers;

public interface ICaptchaSolver
{
    string Name { get; }
    void ConfigureChromeExtension(ChromeOptions options, string extensionBasePath);
    void ConfigureFirefoxExtension(FirefoxDriver driver, string extensionBasePath);
    void ConfigureApiKey(string extensionBasePath, string apiKey);
    void PostDriverInit(IWebDriver driver, string apiKey);
}
