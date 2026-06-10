using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

namespace RegMailNet.EmailProviders;

public class YahooProvider : IEmailProvider
{
    private static class Selectors
    {
        public static readonly By Username = By.Id("usernamereg-userId");
        public static readonly By Password = By.Id("usernamereg-password");
        public static readonly By FirstName = By.Id("usernamereg-firstName");
        public static readonly By LastName = By.Id("usernamereg-lastName");
        public static readonly By BirthMonth = By.Id("usernamereg-month");
        public static readonly By BirthDay = By.Id("usernamereg-day");
        public static readonly By BirthYear = By.Id("usernamereg-year");
        public static readonly By SubmitButton = By.Id("reg-submit-button");
        public static readonly By PhoneInput = By.Id("usernamereg-phone");
        public static readonly By RecaptchaFrame = By.Id("recaptcha-iframe");
        public static readonly By FuncaptchaFrame = By.Id("arkose-iframe");
        public static readonly By VerificationCode = By.Id("verification-code-field");
    }

    private const string Url = "https://login.yahoo.com/account/create";
    private const int WaitTimeout = 25;
    private const int CaptchaSolveTimeout = 120;

    private readonly ISmsServiceFactory _smsServiceFactory;
    private readonly ILogger<YahooProvider> _logger;

    public string ProviderName => "yahoo";

    public YahooProvider(ISmsServiceFactory smsServiceFactory, ILogger<YahooProvider> logger)
    {
        _smsServiceFactory = smsServiceFactory;
        _logger = logger;
    }

    public virtual AccountCreationResult CreateAccount(
        IWebDriver driver,
        ISmsServiceFactory smsServiceFactory,
        Dictionary<string, string> smsKey,
        string username,
        string password,
        string firstName,
        string lastName,
        string month,
        string day,
        string year)
    {
        try
        {
            _logger.LogInformation("Starting Yahoo account creation process");
            driver.Navigate().GoToUrl(Url);

            WebHelpers.SetInputValue(driver, Selectors.Username, username);
            WebHelpers.SetInputValue(driver, Selectors.Password, password);
            WebHelpers.SetInputValue(driver, Selectors.FirstName, firstName);
            WebHelpers.SetInputValue(driver, Selectors.LastName, lastName);

            var monthSelect = new OpenQA.Selenium.Support.UI.SelectElement(
                new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
                    .Until(d => d.FindElement(Selectors.BirthMonth)));
            monthSelect.SelectByIndex(int.Parse(month));

            WebHelpers.SetInputValue(driver, Selectors.BirthDay, day);
            WebHelpers.SetInputValue(driver, Selectors.BirthYear, year);

            WebHelpers.WaitAndClick(driver, Selectors.SubmitButton);

            var smsProvider = smsServiceFactory.Create(smsKey, "yahoo");
            var phoneInfo = HandlePhoneSubmission(driver, smsKey, smsProvider);

            if (!driver.Url.Contains("phone-verify"))
                HandleCaptcha(driver);

            VerifyPhone(driver, smsKey, smsProvider, phoneInfo);

            new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
                .Until(d => d.Url.Contains("create/success") || d.Url.Contains("account/upsell/webauth"));

            _logger.LogInformation("Yahoo account created successfully");
            return new AccountCreationResult($"{username}@yahoo.com", password);
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Yahoo account creation failed");
            throw new AccountCreationException("Yahoo account creation failed", ex);
        }
        finally
        {
            driver.Quit();
        }
    }

    public Task<AccountCreationResult> CreateAccountAsync(IWebDriver driver, string username, string password, string firstName, string lastName, string month, string day, string year, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Use the overload with ISmsServiceFactory and smsKey");
    }

    private void HandleCaptcha(IWebDriver driver)
    {
        try
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(CaptchaSolveTimeout))
                .Until(d => { d.SwitchTo().Frame(driver.FindElement(Selectors.RecaptchaFrame)); return true; });
            try
            {
                var completeBtn = new WebDriverWait(driver, TimeSpan.FromSeconds(CaptchaSolveTimeout))
                    .Until(d => d.FindElement(By.Id("recaptcha-submit")));
                WebHelpers.SafeClick(completeBtn);
            }
            finally
            {
                driver.SwitchTo().DefaultContent();
            }
        }
        catch (WebDriverException)
        {
            try
            {
                new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
                    .Until(d => { d.SwitchTo().Frame(driver.FindElement(Selectors.FuncaptchaFrame)); return true; });
                new WebDriverWait(driver, TimeSpan.FromSeconds(CaptchaSolveTimeout))
                    .Until(d => d.FindElement(By.XPath("//h2[contains(., 'Security check complete')]")).Displayed);
                WebHelpers.SafeClick(driver.FindElement(By.Id("arkose-submit")));
            }
            finally
            {
                driver.SwitchTo().DefaultContent();
            }
        }
    }

    private Dictionary<string, string> HandlePhoneSubmission(IWebDriver driver, Dictionary<string, string> smsKey, ISmsService smsProvider)
    {
        var phoneInfo = new Dictionary<string, string>();
        try
        {
            var phoneResult = smsProvider.GetPhoneAsync(sendPrefix: false).GetAwaiter().GetResult();
            phoneInfo["phone"] = phoneResult.PhoneNumber;
            if (phoneResult.OrderId != null)
                phoneInfo["order_id"] = phoneResult.OrderId;

            WebHelpers.SetInputValue(driver, Selectors.PhoneInput, phoneResult.PhoneNumber);

            By[] nextButtonSelectors = [By.Id("reg-sms-button"), By.Id("reg-submit-button")];
            foreach (var selector in nextButtonSelectors)
            {
                try
                {
                    WebHelpers.WaitAndClick(driver, selector, 10);
                    return phoneInfo;
                }
                catch (WebDriverException)
                {
                    continue;
                }
            }

            throw new WebDriverException("No valid next button found after phone entry");
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Phone submission failed");
            throw new AccountCreationException("Phone verification step failed", ex);
        }
    }

    private void VerifyPhone(IWebDriver driver, Dictionary<string, string> smsKey, ISmsService smsProvider, Dictionary<string, string> phoneInfo)
    {
        try
        {
            var orderId = phoneInfo.GetValueOrDefault("order_id");
            var phone = phoneInfo.GetValueOrDefault("phone");
            var code = orderId != null
                ? smsProvider.GetCodeAsync(orderId).GetAwaiter().GetResult()
                : smsProvider.GetCodeAsync(phone!).GetAwaiter().GetResult();

            var codeInput = new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
                .Until(d => d.FindElement(Selectors.VerificationCode));
            codeInput.SendKeys(code + Keys.Enter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS verification failed");
            throw new AccountCreationException("Phone verification failed", ex);
        }
    }
}
