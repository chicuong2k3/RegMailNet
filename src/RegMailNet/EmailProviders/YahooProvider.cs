using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;

namespace RegMailNet.EmailProviders;

public class YahooProvider : IEmailProvider
{
    private static class Sel
    {
        public const string Username = "#usernamereg-userId";
        public const string Password = "#usernamereg-password";
        public const string FirstName = "#usernamereg-firstName";
        public const string LastName = "#usernamereg-lastName";
        public const string BirthMonth = "#usernamereg-month";
        public const string BirthDay = "#usernamereg-day";
        public const string BirthYear = "#usernamereg-year";
        public const string SubmitButton = "#reg-submit-button";
        public const string PhoneInput = "#usernamereg-phone";
        public const string RecaptchaFrame = "#recaptcha-iframe";
        public const string FuncaptchaFrame = "#arkose-iframe";
        public const string VerificationCode = "#verification-code-field";
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
            _logger.LogInformation("Starting Yahoo account creation process");
            await page.GotoAsync(Url);

            await WebHelpers.FillAsync(page, Sel.Username, username);
            await WebHelpers.FillAsync(page, Sel.Password, password);
            await WebHelpers.FillAsync(page, Sel.FirstName, firstName);
            await WebHelpers.FillAsync(page, Sel.LastName, lastName);

            await WebHelpers.SelectByIndexAsync(page, Sel.BirthMonth, int.Parse(month));

            await WebHelpers.FillAsync(page, Sel.BirthDay, day);
            await WebHelpers.FillAsync(page, Sel.BirthYear, year);

            await WebHelpers.ClickAsync(page, Sel.SubmitButton);

            var smsProvider = smsServiceFactory.Create(smsKey, "yahoo");
            var phoneInfo = await HandlePhoneSubmissionAsync(page, smsProvider);

            if (!page.Url.Contains("phone-verify"))
                await HandleCaptchaAsync(page);

            await VerifyPhoneAsync(page, smsProvider, phoneInfo);

            await page.WaitForURLAsync("**/*create/success**|**/*account/upsell/webauth**",
                new PageWaitForURLOptions { Timeout = WaitTimeout * 1000 });

            _logger.LogInformation("Yahoo account created successfully");
            return new AccountCreationResult($"{username}@yahoo.com", password);
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Yahoo account creation failed");
            throw new AccountCreationException("Yahoo account creation failed", ex);
        }
    }

    private async Task HandleCaptchaAsync(IPage page)
    {
        try
        {
            var recaptchaFrame = page.FrameLocator(Sel.RecaptchaFrame);
            try
            {
                var completeBtn = recaptchaFrame.Locator("#recaptcha-submit");
                await completeBtn.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = CaptchaSolveTimeout * 1000,
                });
                await completeBtn.ClickAsync();
            }
            catch (TimeoutException)
            {
                // Try funcaptcha fallback
                var funcaptchaFrame = page.FrameLocator(Sel.FuncaptchaFrame);
                await funcaptchaFrame.Locator("h2:has-text('Security check complete')").WaitForAsync(
                    new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = CaptchaSolveTimeout * 1000,
                    });
                await funcaptchaFrame.Locator("#arkose-submit").ClickAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Captcha handling failed");
            throw new AccountCreationException("Captcha challenge failed", ex);
        }
    }

    private async Task<Dictionary<string, string>> HandlePhoneSubmissionAsync(IPage page, ISmsService smsProvider)
    {
        var phoneInfo = new Dictionary<string, string>();
        try
        {
            var phoneResult = await smsProvider.GetPhoneAsync(sendPrefix: false);
            phoneInfo["phone"] = phoneResult.PhoneNumber;
            if (phoneResult.OrderId != null)
                phoneInfo["order_id"] = phoneResult.OrderId;

            await WebHelpers.FillAsync(page, Sel.PhoneInput, phoneResult.PhoneNumber);

            string[] nextButtonSelectors = ["#reg-sms-button", "#reg-submit-button"];
            foreach (var selector in nextButtonSelectors)
            {
                try
                {
                    await WebHelpers.ClickAsync(page, selector, 10);
                    return phoneInfo;
                }
                catch (TimeoutException)
                {
                    continue;
                }
            }

            throw new TimeoutException("No valid next button found after phone entry");
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Phone submission failed");
            throw new AccountCreationException("Phone verification step failed", ex);
        }
    }

    private async Task VerifyPhoneAsync(IPage page, ISmsService smsProvider, Dictionary<string, string> phoneInfo)
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
                Timeout = WaitTimeout * 1000,
            });
            await codeInput.FillAsync(code);
            await page.Keyboard.PressAsync("Enter");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS verification failed");
            throw new AccountCreationException("Phone verification failed", ex);
        }
    }
}
