using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RegMailNet.Utilities;

namespace RegMailNet.EmailProviders;

public class OutlookProvider : IEmailProvider
{
    private static class Selectors
    {
        public static readonly By EmailSwitch = By.Id("liveSwitch");
        public static readonly By UsernameInput = By.Id("usernameInput");
        public static readonly By DomainSelect = By.Id("domainSelect");
        public static readonly By NextButton = By.Id("nextButton");
        public static readonly By ShowPassword = By.Id("ShowHidePasswordCheckbox");
        public static readonly By OptinEmail = By.Id("iOptinEmail");
        public static readonly By PasswordInput = By.Id("Password");
        public static readonly By FirstNameInput = By.Id("firstNameInput");
        public static readonly By LastNameInput = By.Id("lastNameInput");
        public static readonly By CountrySelect = By.Id("countryRegionDropdown");
        public static readonly By BirthMonth = By.Id("BirthMonth");
        public static readonly By BirthDay = By.Id("BirthDay");
        public static readonly By BirthYear = By.Id("BirthYear");
        public static readonly By CaptchaFrame = By.Id("enforcementFrame");
        public static readonly By SuccessMessage = By.XPath("//span[contains(text(), 'A quick note about your Microsoft account')]");
        public static readonly By OkButton = By.Id("id__0");
    }

    private const int WaitTimeout = 10;
    private const int MaxCaptchaRetries = 3;
    private const int CaptchaRetryDelay = 60;

    private readonly ILogger<OutlookProvider> _logger;

    public string ProviderName => "outlook";

    public OutlookProvider(ILogger<OutlookProvider> logger)
    {
        _logger = logger;
    }

    public AccountCreationResult CreateAccount(
        IWebDriver driver,
        string username,
        string password,
        string firstName,
        string lastName,
        string country,
        string month,
        string day,
        string year,
        bool hotmail)
    {
        try
        {
            _logger.LogInformation("Starting Microsoft account creation process");
            driver.Navigate().GoToUrl("https://signup.live.com/signup");

            WebHelpers.WaitAndClick(driver, Selectors.EmailSwitch);
            Thread.Sleep(2000);
            WebHelpers.SetInputValue(driver, Selectors.UsernameInput, username);

            if (hotmail)
                SelectDropdownByIndex(driver, Selectors.DomainSelect, 1);

            WebHelpers.WaitAndClick(driver, Selectors.NextButton);

            try
            {
                WebHelpers.WaitAndClick(driver, Selectors.ShowPassword);
                WebHelpers.WaitAndClick(driver, Selectors.OptinEmail);
            }
            catch (WebDriverException)
            {
                _logger.LogDebug("Optional password visibility elements not found");
            }

            WebHelpers.SetInputValue(driver, Selectors.PasswordInput, password);
            WebHelpers.WaitAndClick(driver, Selectors.NextButton);

            WebHelpers.SetInputValue(driver, Selectors.FirstNameInput, firstName);
            WebHelpers.SetInputValue(driver, Selectors.LastNameInput, lastName);
            WebHelpers.WaitAndClick(driver, Selectors.NextButton);

            SelectDropdown(driver, Selectors.CountrySelect, country);
            SelectDropdownByIndex(driver, Selectors.BirthMonth, int.Parse(month));
            SelectDropdownByIndex(driver, Selectors.BirthDay, int.Parse(day));
            WebHelpers.SetInputValue(driver, Selectors.BirthYear, year);
            WebHelpers.WaitAndClick(driver, Selectors.NextButton);

            HandleCaptcha(driver);

            if (!VerifyAccountCreation(driver))
                throw new AccountCreationException("Account creation verification failed");

            var domain = hotmail ? "hotmail" : "outlook";
            _logger.LogInformation("{Provider} account created successfully", hotmail ? "Hotmail" : "Outlook");
            return new AccountCreationResult($"{username}@{domain}.com", password);
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Account creation failed");
            throw new AccountCreationException("Microsoft account creation process failed", ex);
        }
        finally
        {
            driver.Quit();
        }
    }

    public Task<AccountCreationResult> CreateAccountAsync(IWebDriver driver, string username, string password, string firstName, string lastName, string month, string day, string year, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateAccount(driver, username, password, firstName, lastName, "", month, day, year, false));
    }

    private void SelectDropdown(IWebDriver driver, By by, string value)
    {
        var element = new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
            .Until(d => d.FindElement(by));
        new OpenQA.Selenium.Support.UI.SelectElement(element).SelectByText(value);
    }

    private void SelectDropdownByIndex(IWebDriver driver, By by, int index)
    {
        var element = new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
            .Until(d => d.FindElement(by));
        new OpenQA.Selenium.Support.UI.SelectElement(element).SelectByIndex(index);
    }

    private void HandleCaptcha(IWebDriver driver)
    {
        var success = false;
        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout));
            wait.Until(d => d.SwitchTo().Frame(driver.FindElement(Selectors.CaptchaFrame)));
            wait.Until(d => { d.SwitchTo().Frame(d.FindElement(By.TagName("iframe"))); return true; });
            wait.Until(d => { d.SwitchTo().Frame(d.FindElement(By.Id("game-core-frame"))); return true; });

            WebHelpers.WaitAndClick(driver, By.CssSelector("div#root > div > div > button"));

            for (var i = 0; i < MaxCaptchaRetries; i++)
            {
                try
                {
                    new WebDriverWait(driver, TimeSpan.FromSeconds(CaptchaRetryDelay))
                        .Until(d => d.Url.Contains("privacynotice"));
                    if (driver.Url.Contains("privacynotice"))
                    {
                        success = true;
                        break;
                    }
                }
                catch (WebDriverException)
                {
                    continue;
                }
            }

            if (!success)
                throw new AccountCreationException("The captcha was not solved");
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Captcha handling failed");
            throw new AccountCreationException("Captcha challenge failed", ex);
        }
        finally
        {
            driver.SwitchTo().DefaultContent();
        }
    }

    private bool VerifyAccountCreation(IWebDriver driver)
    {
        try
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
                .Until(d => d.FindElement(Selectors.SuccessMessage).Displayed);
            WebHelpers.WaitAndClick(driver, Selectors.OkButton);
            return true;
        }
        catch (WebDriverException)
        {
            _logger.LogError("Account creation verification timeout");
            return false;
        }
    }
}
