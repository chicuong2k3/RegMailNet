using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace RegMailNet.WebDriver;

public class WebDriverFactory : IWebDriverFactory
{
    private readonly IProxyAuthExtensionBuilder _proxyExtBuilder;
    private readonly ILogger<WebDriverFactory> _logger;

    public WebDriverFactory(IProxyAuthExtensionBuilder proxyExtBuilder, ILogger<WebDriverFactory> logger)
    {
        _proxyExtBuilder = proxyExtBuilder;
        _logger = logger;
    }

    public IWebDriver CreateDriver(string browser, bool captchaExtension = false, string? proxy = null, CaptchaKeyInfo? captchaKey = null)
    {
        string? host = null; int? port = null; string? username = null; string? password = null;
        if (proxy != null)
        {
            var parsed = ParseProxy(proxy);
            host = parsed.host; port = parsed.port; username = parsed.username; password = parsed.password;
        }

        return browser switch
        {
            "firefox" => CreateFirefoxDriver(host, port, username, password, captchaExtension, captchaKey),
            "chrome" => CreateChromeDriver(host, port, username, password, captchaExtension, captchaKey),
            "undetected-chrome" => CreateUndetectedChromeDriver(host, port, username, password, captchaExtension, captchaKey),
            _ => throw new ArgumentException($"Unsupported browser: {browser}")
        };
    }

    private IWebDriver CreateFirefoxDriver(string? host, int? port, string? username, string? password, bool captchaExtension, CaptchaKeyInfo? captchaKey)
    {
        new DriverManager().SetUpDriver(new FirefoxConfig());

        var profile = new FirefoxProfile();
        profile.SetPreference("extensions.ui.developer_mode", true);
        profile.SetPreference("intl.accept_languages", "en-us");

        var options = new FirefoxOptions();
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--headless");
        options.Profile = profile;

        if (host != null && port != null)
        {
            options.SetPreference("network.proxy.type", 1);
            options.SetPreference("network.proxy.http", host);
            options.SetPreference("network.proxy.http_port", port.Value);
            options.SetPreference("network.proxy.socks", host);
            options.SetPreference("network.proxy.socks_port", port.Value);
            options.SetPreference("network.proxy.socks_remote_dns", false);
            options.SetPreference("network.proxy.ssl", host);
            options.SetPreference("network.proxy.ssl_port", port.Value);
        }

        var driver = new FirefoxDriver(options);

        if (captchaExtension && captchaKey != null)
        {
            var extensionPath = GetCaptchaExtensionPath();
            ConfigureCaptchaSolverFirefox(driver, captchaKey, extensionPath);
        }

        return driver;
    }

    private IWebDriver CreateChromeDriver(string? host, int? port, string? username, string? password, bool captchaExtension, CaptchaKeyInfo? captchaKey)
    {
        new DriverManager().SetUpDriver(new ChromeConfig());

        var options = new ChromeOptions();
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--headless=new");
        options.AddAdditionalOption("excludeSwitches", new[] { "enable-logging" });
        options.AddUserProfilePreference("intl.accept_languages", "en-us");

        string? proxyExt = null;
        if (host != null && port != null)
        {
            if (username != null && password != null)
            {
                proxyExt = _proxyExtBuilder.BuildExtension(host, port.Value, username, password);
                if (!captchaExtension)
                    options.AddArgument($"--load-extension={proxyExt}");
            }
            else
            {
                options.AddArgument($"--proxy-server={host}:{port}");
            }
        }

        if (captchaExtension && captchaKey != null)
        {
            var extensionPath = GetCaptchaExtensionPath();
            ConfigureCaptchaSolverChrome(options, captchaKey, extensionPath, proxyExt);
        }

        var driver = new ChromeDriver(options);

        if (captchaKey?.Name == "nopecha")
        {
            driver.Navigate().GoToUrl($"https://nopecha.com/setup#{captchaKey.Key}");
        }

        return driver;
    }

    private IWebDriver CreateUndetectedChromeDriver(string? host, int? port, string? username, string? password, bool captchaExtension, CaptchaKeyInfo? captchaKey)
    {
        var options = new ChromeOptions();
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");
        options.AddUserProfilePreference("intl.accept_languages", "en-us");

        string? proxyExt = null;
        if (host != null && port != null)
        {
            if (username != null && password != null)
            {
                proxyExt = _proxyExtBuilder.BuildExtension(host, port.Value, username, password);
                if (!captchaExtension)
                    options.AddArgument($"--load-extension={proxyExt}");
            }
            else
            {
                options.AddArgument($"--proxy-server={host}:{port}");
            }
        }

        if (captchaExtension && captchaKey != null)
        {
            var extensionPath = GetCaptchaExtensionPath();
            ConfigureCaptchaSolverChrome(options, captchaKey, extensionPath, proxyExt);
        }

        var driver = SeleniumUndetectedChromeDriver.UndetectedChromeDriver.Create(options);

        if (captchaKey?.Name == "nopecha")
        {
            driver.Navigate().GoToUrl($"https://nopecha.com/setup#{captchaKey.Key}");
        }

        return driver;
    }

    private void ConfigureCaptchaSolverChrome(ChromeOptions options, CaptchaKeyInfo captchaKey, string extensionPath, string? proxyExt)
    {
        if (captchaKey.Name == "capsolver")
        {
            var configPath = Path.Combine(extensionPath, "capsolver-chrome-extension", "assets", "config.js");
            if (File.Exists(configPath))
            {
                var content = File.ReadAllText(configPath);
                var updated = System.Text.RegularExpressions.Regex.Replace(content, @"apiKey:\s*'[^']*'", $"apiKey: '{captchaKey.Key}'");
                File.WriteAllText(configPath, updated);
            }
            var extPath = Path.Combine(extensionPath, "capsolver-chrome-extension");
            options.AddArgument(proxyExt != null ? $"--load-extension={extPath},{proxyExt}" : $"--load-extension={extPath}");
        }
        else if (captchaKey.Name == "nopecha")
        {
            var extPath = Path.Combine(extensionPath, "NopeCHA-CAPTCHA-Solver");
            options.AddArgument(proxyExt != null ? $"--load-extension={extPath},{proxyExt}" : $"--load-extension={extPath}");
        }
    }

    private void ConfigureCaptchaSolverFirefox(FirefoxDriver driver, CaptchaKeyInfo captchaKey, string extensionPath)
    {
        if (captchaKey.Name == "capsolver")
        {
            var xpiPath = Path.Combine(extensionPath, "capsolver_captcha_solver-1.10.4.xpi");
            driver.InstallAddOn(xpiPath);
            driver.Navigate().GoToUrl("https://www.google.com");
            var capsolverSrc = driver.FindElement(OpenQA.Selenium.By.XPath("/html/script[2]")).GetAttribute("src");
            var capsolverExtId = capsolverSrc.Split('/')[2];
            driver.Navigate().GoToUrl($"moz-extension://{capsolverExtId}/www/index.html#/popup");
            Thread.Sleep(5000);
            var apiKeyInput = driver.FindElement(OpenQA.Selenium.By.XPath("//input[@placeholder=\"Please input your API key\"]"));
            apiKeyInput.SendKeys(captchaKey.Key);
            driver.FindElement(OpenQA.Selenium.By.Id("q-app")).Click();
        }
        else if (captchaKey.Name == "nopecha")
        {
            var xpiPath = Path.Combine(extensionPath, "noptcha-0.4.9.xpi");
            driver.InstallAddOn(xpiPath);
            driver.Navigate().GoToUrl($"https://nopecha.com/setup#{captchaKey.Key}");
        }
    }

    private static string GetCaptchaExtensionPath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captcha_solvers");
    }

    private static (string host, int port, string? username, string? password) ParseProxy(string proxy)
    {
        var uri = new Uri(proxy);
        return (uri.Host, uri.Port, uri.UserInfo.Split(':').FirstOrDefault(), uri.UserInfo.Split(':').Skip(1).FirstOrDefault());
    }
}
