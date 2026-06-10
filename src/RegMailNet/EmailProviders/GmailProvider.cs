using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

namespace RegMailNet.EmailProviders;

public class GmailProvider : IEmailProvider
{
    private static class Sel
    {
        public const string FirstName = "#firstName";
        public const string LastName = "#lastName";
        public const string Month = "#month";
        public const string Day = "input[name=\"day\"]";
        public const string Year = "#year";
        public const string Gender = "#gender";
        public const string Username = "input[name=\"Username\"]";
        public const string Password = "input[name=\"Passwd\"]";
        public const string PasswordConfirm = "input[name=\"PasswdAgain\"]";
        public const string PhoneInput = "#phoneNumberId";
        public const string VerificationCode = "#code";
        public const string ErrorMessage = "div:has-text('Sorry, we could not create your Google Account.')";
        public const string PhoneError = "div.Ekjuhf.Jj6Lae";
    }

    private static readonly string[] NextButtonSelectors =
    [
        "span:has-text('Skip')",
        "div.VfPpkd-RLmnJb",
        "div.VfPpkd-Jh9lGc",
        "span.VfPpkd-vQzf8d",
        "span:has-text('Next')",
        "span:has-text('I agree')",
        "div:has-text('I agree')",
        "button.VfPpkd-LgbsSe",
        "button:has-text('Next')",
        "button:has-text('I agree')",
    ];

    private const int WaitTimeout = 10;
    private const int WaitTimeoutMs = WaitTimeout * 1000;

    private readonly ISmsServiceFactory _smsServiceFactory;
    private readonly ILogger<GmailProvider> _logger;

    public string ProviderName => "gmail";

    public GmailProvider(ISmsServiceFactory smsServiceFactory, ILogger<GmailProvider> logger)
    {
        _smsServiceFactory = smsServiceFactory;
        _logger = logger;
    }

    public virtual async Task<AccountCreationResult> CreateAccountAsync(
        IPage page,
        string username,
        string password,
        string firstName,
        string lastName,
        string month,
        string day,
        string year,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Use the overload with ISmsServiceFactory and smsKey");
    }

    public virtual async Task<AccountCreationResult> CreateAccountAsync(
        IPage page,
        ISmsServiceFactory smsServiceFactory,
        Dictionary<string, string> smsKey,
        string username,
        string password,
        string firstName,
        string lastName,
        string month,
        string day,
        string year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Gmail account creation process");
            await page.GotoAsync("https://accounts.google.com/signup/v2/createaccount?flowName=GlifWebSignIn&flowEntry=SignUp");

            await FillPersonalInfoAsync(page, firstName, lastName);
            await FillBirthdateAsync(page, month, day, year);

            await SetHowToSetUsernameAsync(page);
            await WebHelpers.FillAsync(page, Sel.Username, username);
            await NextButtonAsync(page);
            await WebHelpers.TypeIntoAsync(page, Sel.Password, password);
            await WebHelpers.TypeIntoAsync(page, Sel.PasswordConfirm, password);
            await NextButtonAsync(page);

            await HandleErrorsAsync(page);

            await Task.Delay(2000, cancellationToken);
            if (await WebHelpers.ElementExistsAsync(page, Sel.PhoneInput))
            {
                var smsProvider = smsServiceFactory.Create(smsKey, "google");
                var phoneInfo = await HandlePhoneVerificationAsync(page, smsProvider);
                await HandleSmsCodeAsync(page, smsProvider, phoneInfo);
            }

            for (var i = 0; i < 3; i++)
            {
                await NextButtonAsync(page);
                await Task.Delay(2000, cancellationToken);
            }

            await ConfirmAlertAsync(page);

            var agreeButton = page.Locator("button span.VfPpkd-vQzf8d");
            await agreeButton.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000,
            });
            await agreeButton.ClickAsync();

            _logger.LogInformation("Gmail account created successfully");
            return new AccountCreationResult($"{username}@gmail.com", password);
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Account creation failed");
            throw new AccountCreationException("Account creation process failed", ex);
        }
    }

    private async Task NextButtonAsync(IPage page)
    {
        foreach (var selector in NextButtonSelectors)
        {
            try
            {
                var currentUrl = page.Url;
                await WebHelpers.ClickAsync(page, selector, 5);
                await Task.Delay(1000);
                if (currentUrl != page.Url) return;
            }
            catch (TimeoutException)
            {
                continue;
            }
        }
        throw new AccountCreationException("Failed to find next button");
    }

    private async Task SetHowToSetUsernameAsync(IPage page)
    {
        try
        {
            var locator = page.Locator("#selectionc22");
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = WaitTimeoutMs,
            });
            await locator.ClickAsync();
        }
        catch (TimeoutException)
        {
            // Optional element
        }
    }

    private async Task HandleErrorsAsync(IPage page)
    {
        try
        {
            var errorElement = page.Locator(Sel.ErrorMessage);
            await errorElement.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000,
            });
            var text = await errorElement.TextContentAsync();
            _logger.LogError("Google account creation failed: {Error}", text);
            throw new AccountCreationException($"Google error: {text}");
        }
        catch (TimeoutException) { }
    }

    private async Task FillPersonalInfoAsync(IPage page, string firstName, string lastName)
    {
        try
        {
            await WebHelpers.FillAsync(page, Sel.FirstName, firstName);
            await WebHelpers.FillAsync(page, Sel.LastName, lastName);
            await NextButtonAsync(page);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to fill personal info");
            throw new AccountCreationException("Personal info section timed out", ex);
        }
    }

    private async Task FillBirthdateAsync(IPage page, string month, string day, string year)
    {
        try
        {
            await WebHelpers.TypeIntoAsync(page, Sel.Day, day);
            await WebHelpers.TypeIntoAsync(page, Sel.Year, year);

            // Select month
            var monthSelect = page.Locator(Sel.Month);
            await monthSelect.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = WaitTimeoutMs,
            });
            await monthSelect.ClickAsync();

            var monthName = MonthsMapping.GetMonthName(month);
            var monthElement = page.Locator($"span:has-text('{monthName}')").First;
            await monthElement.ScrollIntoViewIfNeededAsync();
            await monthElement.ClickAsync();

            // Select gender
            var genderSelect = page.Locator(Sel.Gender);
            await genderSelect.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = WaitTimeoutMs,
            });
            await genderSelect.ClickAsync();

            var genderElement = page.Locator("span:has-text('Rather not say')").First;
            await genderElement.ClickAsync();

            await NextButtonAsync(page);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to fill birthdate info");
            throw new AccountCreationException("Birthdate section failed", ex);
        }
    }

    private async Task<Dictionary<string, string>> HandlePhoneVerificationAsync(IPage page, ISmsService smsProvider)
    {
        try
        {
            var phoneInfo = new Dictionary<string, string>();
            var phoneResult = await smsProvider.GetPhoneAsync(sendPrefix: true);
            phoneInfo["phone"] = phoneResult.PhoneNumber;
            if (phoneResult.OrderId != null)
                phoneInfo["order_id"] = phoneResult.OrderId;

            var phoneInput = page.Locator(Sel.PhoneInput);
            await phoneInput.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = WaitTimeoutMs,
            });
            await phoneInput.FillAsync("+" + phoneResult.PhoneNumber);
            await page.Keyboard.PressAsync("Enter");

            try
            {
                var errorElement = page.Locator(Sel.PhoneError);
                await errorElement.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 10000,
                });
                var text = await errorElement.TextContentAsync();
                _logger.LogError("Phone number rejected: {Error}", text);
                throw new AccountCreationException($"Phone rejected: {text}");
            }
            catch (TimeoutException) { }

            return phoneInfo;
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Phone verification failed");
            throw new AccountCreationException("Phone verification step failed", ex);
        }
    }

    private async Task HandleSmsCodeAsync(IPage page, ISmsService smsProvider, Dictionary<string, string> phoneInfo)
    {
        try
        {
            var orderId = phoneInfo.GetValueOrDefault("order_id");
            var phone = phoneInfo.GetValueOrDefault("phone");
            var code = orderId != null
                ? await smsProvider.GetCodeAsync(orderId)
                : await smsProvider.GetCodeAsync(phone!);

            var codeInput = page.Locator(Sel.VerificationCode);
            await codeInput.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = WaitTimeoutMs,
            });
            await codeInput.FillAsync(code);
            await page.Keyboard.PressAsync("Enter");
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS code entry failed");
            throw new AccountCreationException("SMS verification failed", ex);
        }
    }

    private async Task ConfirmAlertAsync(IPage page)
    {
        try
        {
            page.Dialog += async (_, dialog) =>
            {
                await dialog.AcceptAsync();
            };
            await Task.Delay(500);
        }
        catch (Exception)
        {
            _logger.LogInformation("No alert present");
        }
    }
}
