using OpenQA.Selenium;

namespace RegMailNet.WebDriver;

public interface IWebDriverFactory
{
    IWebDriver CreateDriver(
        string browser,
        bool captchaExtension = false,
        string? proxy = null,
        CaptchaKeyInfo? captchaKey = null);
}

public record CaptchaKeyInfo(string Name, string Key);
