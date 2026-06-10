using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

namespace RegMailNet.EmailProviders;

public class GmailProvider : IEmailProvider
{
    private static class Selectors
    {
        public static readonly By FirstName = By.Id("firstName");
        public static readonly By LastName = By.Id("lastName");
        public static readonly By Month = By.Id("month");
        public static readonly By Day = By.XPath("//input[@name=\"day\"]");
        public static readonly By Year = By.Id("year");
        public static readonly By Gender = By.Id("gender");
        public static readonly By Username = By.Name("Username");
        public static readonly By Password = By.Name("Passwd");
        public static readonly By PasswordConfirm = By.Name("PasswdAgain");
        public static readonly By PhoneInput = By.Id("phoneNumberId");
        public static readonly By VerificationCode = By.Id("code");
        public static readonly By ErrorMessage = By.XPath("//div[contains(text(), 'Sorry, we could not create your Google Account.')]");
        public static readonly By PhoneError = By.XPath("//div[@class='Ekjuhf Jj6Lae']");
    }

    private static readonly By[] NextButtonSelectors =
    [
        By.XPath("//span[contains(text(),'Skip')]"),
        By.CssSelector("div.VfPpkd-RLmnJb"),
        By.CssSelector("div.VfPpkd-Jh9lGc"),
        By.CssSelector("span.VfPpkd-vQzf8d"),
        By.XPath("//span[contains(text(), 'Next')]"),
        By.XPath("//span[contains(text(),'I agree')]"),
        By.XPath("//div[contains(text(),'I agree')]"),
        By.ClassName("VfPpkd-LgbsSe"),
        By.XPath("//button[contains(text(),'Next')]"),
        By.XPath("//button[contains(text(),'I agree')]")
    ];

    private const int WaitTimeout = 10;

    private readonly ISmsServiceFactory _smsServiceFactory;
    private readonly ILogger<GmailProvider> _logger;

    public string ProviderName => "gmail";

    public GmailProvider(ISmsServiceFactory smsServiceFactory, ILogger<GmailProvider> logger)
    {
        _smsServiceFactory = smsServiceFactory;
        _logger = logger;
    }

    public AccountCreationResult CreateAccount(
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
            _logger.LogInformation("Starting Gmail account creation process");
            driver.Navigate().GoToUrl("https://accounts.google.com/signup/v2/createaccount?flowName=GlifWebSignIn&flowEntry=SignUp");

            FillPersonalInfo(driver, firstName, lastName);
            FillBirthdate(driver, month, day, year);

            SetHowToSetUsername(driver);
            WebHelpers.SetInputValue(driver, Selectors.Username, username);
            NextButton(driver);
            WebHelpers.TypeInto(driver, Selectors.Password, password);
            WebHelpers.TypeInto(driver, Selectors.PasswordConfirm, password);
            NextButton(driver);

            HandleErrors(driver);

            Thread.Sleep(2000);
            if (driver.FindElements(Selectors.PhoneInput).Count > 0)
            {
                var smsProvider = smsServiceFactory.Create(smsKey, "google");
                var phoneInfo = HandlePhoneVerification(driver, smsKey, smsProvider);
                HandleSmsCode(driver, smsKey, smsProvider, phoneInfo);
            }

            for (var i = 0; i < 3; i++)
            {
                NextButton(driver);
                Thread.Sleep(2000);
            }

            ConfirmAlert(driver);

            var agreeButton = new WebDriverWait(driver, TimeSpan.FromSeconds(5))
                .Until(d => d.FindElement(By.CssSelector("button span.VfPpkd-vQzf8d")));
            agreeButton.Click();

            _logger.LogInformation("Gmail account created successfully");
            return new AccountCreationResult($"{username}@gmail.com", password);
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Account creation failed");
            throw new AccountCreationException("Account creation process failed", ex);
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

    private void NextButton(IWebDriver driver)
    {
        foreach (var selector in NextButtonSelectors)
        {
            try
            {
                var currentUrl = driver.Url;
                WebHelpers.WaitAndClick(driver, selector, 5);
                Thread.Sleep(1000);
                if (currentUrl != driver.Url) return;
            }
            catch (WebDriverException)
            {
                continue;
            }
        }
        throw new AccountCreationException("Failed to find next button");
    }

    private void SetHowToSetUsername(IWebDriver driver)
    {
        try
        {
            var element = new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
                .Until(d => d.FindElement(By.Id("selectionc22")));
            element.Click();
        }
        catch (WebDriverException)
        {
            // Optional element
        }
    }

    private void HandleErrors(IWebDriver driver)
    {
        try
        {
            var errorElement = new WebDriverWait(driver, TimeSpan.FromSeconds(5))
                .Until(d => d.FindElement(Selectors.ErrorMessage));
            _logger.LogError("Google account creation failed: {Error}", errorElement.Text);
            throw new AccountCreationException($"Google error: {errorElement.Text}");
        }
        catch (WebDriverException) { }
    }

    private void FillPersonalInfo(IWebDriver driver, string firstName, string lastName)
    {
        try
        {
            WebHelpers.SetInputValue(driver, Selectors.FirstName, firstName);
            WebHelpers.SetInputValue(driver, Selectors.LastName, lastName);
            NextButton(driver);
        }
        catch (WebDriverException)
        {
            _logger.LogError("Failed to fill personal info");
            throw new AccountCreationException("Personal info section timed out");
        }
    }

    private void FillBirthdate(IWebDriver driver, string month, string day, string year)
    {
        try
        {
            WebHelpers.TypeInto(driver, Selectors.Day, day);
            WebHelpers.TypeInto(driver, Selectors.Year, year);

            var monthSelect = new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
                .Until(d => d.FindElement(Selectors.Month));
            WebHelpers.ActionChainClick(driver, monthSelect);

            var monthName = MonthsMapping.GetMonthName(month);
            var monthElement = monthSelect.FindElement(By.XPath($"//span[text()='{monthName}']"));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", monthElement);
            WebHelpers.ActionChainClick(driver, monthElement);

            var genderSelect = new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
                .Until(d => d.FindElement(Selectors.Gender));
            WebHelpers.ActionChainClick(driver, genderSelect);
            var genderElement = genderSelect.FindElement(By.XPath("//span[text()='Rather not say']"));
            WebHelpers.ActionChainClick(driver, genderElement);

            NextButton(driver);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to fill birthdate info");
            throw new AccountCreationException("Birthdate section failed", ex);
        }
    }

    private Dictionary<string, string> HandlePhoneVerification(IWebDriver driver, Dictionary<string, string> smsKey, ISmsService smsProvider)
    {
        try
        {
            var phoneInfo = new Dictionary<string, string>();
            var phoneResult = smsProvider.GetPhoneAsync(sendPrefix: true).GetAwaiter().GetResult();
            phoneInfo["phone"] = phoneResult.PhoneNumber;
            if (phoneResult.OrderId != null)
                phoneInfo["order_id"] = phoneResult.OrderId;

            var phoneInput = new WebDriverWait(driver, TimeSpan.FromSeconds(WaitTimeout))
                .Until(d => d.FindElement(Selectors.PhoneInput));
            phoneInput.SendKeys("+" + phoneResult.PhoneNumber + Keys.Enter);

            try
            {
                var errorElement = new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                    .Until(d => d.FindElement(Selectors.PhoneError));
                _logger.LogError("Phone number rejected: {Error}", errorElement.Text);
                throw new AccountCreationException($"Phone rejected: {errorElement.Text}");
            }
            catch (WebDriverException) { }

            return phoneInfo;
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Phone verification failed");
            throw new AccountCreationException("Phone verification step failed", ex);
        }
    }

    private void HandleSmsCode(IWebDriver driver, Dictionary<string, string> smsKey, ISmsService smsProvider, Dictionary<string, string> phoneInfo)
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
            Thread.Sleep(2000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS code entry failed");
            throw new AccountCreationException("SMS verification failed", ex);
        }
    }

    private void ConfirmAlert(IWebDriver driver)
    {
        try
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(d => { try { return d.SwitchTo().Alert(); } catch (NoAlertPresentException) { return null; } });
            driver.SwitchTo().Alert().Accept();
        }
        catch (NoAlertPresentException)
        {
            _logger.LogInformation("No alert present");
        }
    }
}
