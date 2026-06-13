using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RegMailNet.Utilities;

namespace RegMailNet.EmailProviders;

public class OutlookProvider : IEmailProvider
{
    private static class Sel
    {
        // Step 1: Email entry
        public const string EmailInput = "input[type='email']";

        // Step 3: Password entry
        public const string PasswordInput = "input[type='password']";

        // Step 4: Birthdate (custom dropdowns + number input)
        public const string CountryDropdown = "#countryDropdownId";
        public const string BirthMonthDropdown = "#BirthMonthDropdown";
        public const string BirthDayDropdown = "#BirthDayDropdown";
        public const string BirthYearInput = "input[name='BirthYear']";

        // Step 5: Name entry
        public const string FirstNameInput = "#firstNameInput";
        public const string LastNameInput = "#lastNameInput";

        // Success
        public const string SuccessMessage = "span:has-text('A quick note about your Microsoft account')";
        public const string OkButton = "#id__0";
    }

    private const int WaitTimeout = 5;

    private readonly ILogger<OutlookProvider> _logger;

    public string ProviderName => EmailProvider.Outlook.ToValue();

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
        return await CreateAccountAsync(page, username, password, firstName, lastName, string.Empty, month, day, year, false, cancellationToken);
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

            // Step 1: Enter full email address
            var domain = hotmail ? "hotmail.com" : "outlook.com";
            var fullEmail = $"{username}@{domain}";
            _logger.LogInformation("Entering email: {Email}", fullEmail);
            await WebHelpers.FillAsync(page, Sel.EmailInput, fullEmail);
            await ClickNextButton(page);

            // Step 2: Confirm username (click Next again)
            _logger.LogInformation("Confirming username");
            await Task.Delay(2000, cancellationToken);
            await ClickNextButton(page);

            // Step 3: Enter password
            _logger.LogInformation("Entering password");
            await Task.Delay(2000, cancellationToken);
            await WebHelpers.FillAsync(page, Sel.PasswordInput, password);
            await ClickNextButton(page);

            // Step 4: Enter birthdate and country
            _logger.LogInformation("Entering birthdate and country");
            await Task.Delay(2000, cancellationToken);

            // Select country via custom dropdown
            if (!string.IsNullOrEmpty(country))
                await SelectCustomDropdownByText(page, Sel.CountryDropdown, country);

            // Select birth month via custom dropdown
            await SelectCustomDropdownByIndex(page, Sel.BirthMonthDropdown, int.Parse(month) - 1);

            // Select birth day via custom dropdown
            await SelectCustomDropdownByIndex(page, Sel.BirthDayDropdown, int.Parse(day) - 1);

            // Fill birth year
            await WebHelpers.FillAsync(page, Sel.BirthYearInput, year);
            await ClickNextButton(page);

            // Step 5: Enter name (Microsoft now asks for name after birthdate)
            _logger.LogInformation("Entering name");
            await Task.Delay(3000, cancellationToken);
            await WebHelpers.FillAsync(page, Sel.FirstNameInput, firstName);
            await WebHelpers.FillAsync(page, Sel.LastNameInput, lastName);
            await ClickNextButton(page);

            // Step 6: Handle human verification captcha
            _logger.LogInformation("Checking for human verification captcha");
            await Task.Delay(5000, cancellationToken);
            await HandleHumanVerificationAsync(page, cancellationToken);

            // Verify account creation
            await Task.Delay(3000, cancellationToken);

            // Check if account creation was blocked
            if (await IsAccountBlockedAsync(page))
                throw new AccountCreationException("Microsoft blocked account creation: unusual activity detected");

            if (!await VerifyAccountCreationAsync(page))
                throw new AccountCreationException("Account creation verification failed");

            _logger.LogInformation("{Provider} account created successfully: {Email}", hotmail ? "Hotmail" : "Outlook", fullEmail);
            return new AccountCreationResult(fullEmail, password);
        }
        catch (Exception ex) when (ex is not AccountCreationException)
        {
            _logger.LogError(ex, "Account creation failed");
            throw new AccountCreationException("Microsoft account creation process failed", ex);
        }
    }

    /// <summary>
    /// Click the "Next" button by its text content.
    /// </summary>
    private static async Task ClickNextButton(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
    }

    /// <summary>
    /// Select an option from a custom dropdown by clicking it and then clicking the option by index.
    /// </summary>
    private async Task SelectCustomDropdownByIndex(IPage page, string dropdownSelector, int index)
    {
        _logger.LogDebug("Selecting index {Index} from dropdown {Selector}", index, dropdownSelector);
        await page.Locator(dropdownSelector).ClickAsync(new LocatorClickOptions { Force = true });
        await Task.Delay(500);

        // Click the option at the given index within the dropdown listbox
        var options = page.Locator("[role='option'], [role='listbox'] li");
        var count = await options.CountAsync();
        if (index >= count)
        {
            _logger.LogWarning("Index {Index} out of range (0-{MaxIndex}) for dropdown {Selector}", index, count - 1, dropdownSelector);
            index = count - 1;
        }
        await options.Nth(index).ClickAsync();
        await Task.Delay(300);
    }

    /// <summary>
    /// Select an option from a custom dropdown by its text content.
    /// </summary>
    private async Task SelectCustomDropdownByText(IPage page, string dropdownSelector, string text)
    {
        _logger.LogDebug("Selecting '{Text}' from dropdown {Selector}", text, dropdownSelector);
        await page.Locator(dropdownSelector).ClickAsync(new LocatorClickOptions { Force = true });
        await Task.Delay(500);

        var option = page.GetByRole(AriaRole.Option, new() { Name = text });
        if (await option.CountAsync() == 0)
        {
            // Fallback: try matching by text content
            option = page.Locator($"[role='option']:has-text('{text}'), li:has-text('{text}')");
        }
        await option.First.ClickAsync();
        await Task.Delay(300);
    }

    private async Task HandleHumanVerificationAsync(IPage page, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if "Press and hold" captcha is present
            var humanVerifyText = page.GetByText("Press and hold");
            if (await humanVerifyText.CountAsync() == 0)
            {
                _logger.LogDebug("No human verification captcha found");
                return;
            }

            _logger.LogInformation("Human verification captcha detected, attempting to solve");

            // Find the captcha button in the hsprotect iframe
            var hsprotectFrame = page.FrameLocator("iframe[title='Human Iframe']");
            var captchaButton = hsprotectFrame.Locator("#px-captcha");

            if (await captchaButton.CountAsync() == 0)
            {
                _logger.LogWarning("Captcha button not found in iframe");
                return;
            }

            // Simulate press and hold (mousedown, wait, mouseup)
            var box = await captchaButton.BoundingBoxAsync();
            if (box != null)
            {
                var x = box.X + box.Width / 2;
                var y = box.Y + box.Height / 2;

                await page.Mouse.MoveAsync(x, y);
                await page.Mouse.DownAsync();
                await Task.Delay(10000); // Hold for 10 seconds
                await page.Mouse.UpAsync();

                _logger.LogInformation("Human verification captcha interaction completed");
            }

            // Wait for page to update
            await Task.Delay(5000, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Human verification handling failed (non-fatal)");
        }
    }

    private async Task<bool> IsAccountBlockedAsync(IPage page)
    {
        try
        {
            var blockedText = page.GetByText("Account creation has been blocked");
            return await blockedText.CountAsync() > 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> VerifyAccountCreationAsync(IPage page)
    {
        try
        {
            _logger.LogInformation("Verifying account creation, current URL: {Url}", page.Url);

            // Check for multiple success indicators
            var successSelectors = new[]
            {
                Sel.SuccessMessage,
                "span:has-text('quick note')",
                "span:has-text('Microsoft account')",
                "h1:has-text('Welcome')",
                "div:has-text('account has been created')",
                "#id__0"
            };

            foreach (var selector in successSelectors)
            {
                try
                {
                    var element = page.Locator(selector);
                    if (await element.CountAsync() > 0)
                    {
                        _logger.LogInformation("Found success indicator: {Selector}", selector);

                        // Try to click OK button if present
                        try
                        {
                            await WebHelpers.ClickAsync(page, Sel.OkButton, 3);
                        }
                        catch (TimeoutException)
                        {
                            // OK button might not exist
                        }

                        return true;
                    }
                }
                catch (TimeoutException)
                {
                    continue;
                }
            }

            // Check if URL changed to success page
            if (page.Url.Contains("privacynotice") || page.Url.Contains("success") || page.Url.Contains("upsell"))
            {
                _logger.LogInformation("Account creation succeeded (URL: {Url})", page.Url);
                return true;
            }

            // Log current page content for debugging
            var bodyText = await page.Locator("body").InnerTextAsync();
            _logger.LogWarning("Account verification failed. Page content: {Content}", bodyText.Substring(0, Math.Min(500, bodyText.Length)));

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account creation verification failed");
            return false;
        }
    }
}
