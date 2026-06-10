using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RegMailNet.Utilities;

namespace RegMailNet.EmailProviders;

public class OutlookProvider : IEmailProvider
{
    private static class Sel
    {
        public const string EmailSwitch = "#liveSwitch";
        public const string UsernameInput = "#usernameInput";
        public const string DomainSelect = "#domainSelect";
        public const string NextButton = "#nextButton";
        public const string ShowPassword = "#ShowHidePasswordCheckbox";
        public const string OptinEmail = "#iOptinEmail";
        public const string PasswordInput = "#Password";
        public const string FirstNameInput = "#firstNameInput";
        public const string LastNameInput = "#lastNameInput";
        public const string CountrySelect = "#countryRegionDropdown";
        public const string BirthMonth = "#BirthMonth";
        public const string BirthDay = "#BirthDay";
        public const string BirthYear = "#BirthYear";
        public const string CaptchaFrame = "#enforcementFrame";
        public const string SuccessMessage = "span:has-text('A quick note about your Microsoft account')";
        public const string OkButton = "#id__0";
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
        return await CreateAccountAsync(page, username, password, firstName, lastName, "", month, day, year, false, cancellationToken);
    }

    public virtual async Task<AccountCreationResult> CreateAccountAsync(
        IPage page,
        string username,
        string password,
        string firstName,
        string lastName,
        string country,
        string month,
        string day,
        string year,
        bool hotmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Microsoft account creation process");
            await page.GotoAsync("https://signup.live.com/signup");

            await WebHelpers.ClickAsync(page, Sel.EmailSwitch);
            await Task.Delay(2000, cancellationToken);
            await WebHelpers.FillAsync(page, Sel.UsernameInput, username);

            if (hotmail)
                await WebHelpers.SelectByIndexAsync(page, Sel.DomainSelect, 1);

            await WebHelpers.ClickAsync(page, Sel.NextButton);

            try
            {
                await WebHelpers.ClickAsync(page, Sel.ShowPassword, 5);
                await WebHelpers.ClickAsync(page, Sel.OptinEmail, 5);
            }
            catch (TimeoutException)
            {
                _logger.LogDebug("Optional password visibility elements not found");
            }

            await WebHelpers.FillAsync(page, Sel.PasswordInput, password);
            await WebHelpers.ClickAsync(page, Sel.NextButton);

            await WebHelpers.FillAsync(page, Sel.FirstNameInput, firstName);
            await WebHelpers.FillAsync(page, Sel.LastNameInput, lastName);
            await WebHelpers.ClickAsync(page, Sel.NextButton);

            await WebHelpers.SelectByTextAsync(page, Sel.CountrySelect, country);
            await WebHelpers.SelectByIndexAsync(page, Sel.BirthMonth, int.Parse(month));
            await WebHelpers.SelectByIndexAsync(page, Sel.BirthDay, int.Parse(day));
            await WebHelpers.FillAsync(page, Sel.BirthYear, year);
            await WebHelpers.ClickAsync(page, Sel.NextButton);

            await HandleCaptchaAsync(page);

            if (!await VerifyAccountCreationAsync(page))
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
    }

    private async Task HandleCaptchaAsync(IPage page)
    {
        try
        {
            // Switch to captcha iframe chain
            var captchaFrame = page.FrameLocator(Sel.CaptchaFrame);
            var innerFrame = captchaFrame.FrameLocator("iframe");
            var gameFrame = innerFrame.FrameLocator("#game-core-frame");

            await gameFrame.Locator("div#root > div > div > button").ClickAsync();

            var success = false;
            for (var i = 0; i < MaxCaptchaRetries; i++)
            {
                try
                {
                    await page.WaitForURLAsync("**/*privacynotice**", new PageWaitForURLOptions
                    {
                        Timeout = CaptchaRetryDelay * 1000,
                    });
                    success = true;
                    break;
                }
                catch (TimeoutException)
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
    }

    private async Task<bool> VerifyAccountCreationAsync(IPage page)
    {
        try
        {
            var successMessage = page.Locator(Sel.SuccessMessage);
            await successMessage.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = WaitTimeout * 1000,
            });
            await WebHelpers.ClickAsync(page, Sel.OkButton);
            return true;
        }
        catch (TimeoutException)
        {
            _logger.LogError("Account creation verification timeout");
            return false;
        }
    }
}
