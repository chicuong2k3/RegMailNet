using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit;

namespace RegMailNet.Tests.Ui;

/// <summary>
/// Playwright UI tests for the Blazor WASM app.
/// Requires the app to be running on https://localhost:5001.
/// Start with: dotnet run --project src/RegMailNet.Ui
/// </summary>
public class PlaywrightTests : PageTest
{
    private const string BaseUrl = "https://localhost:5001";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new()
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true
        };
    }

    private async Task GotoAndWaitForBlazor(string path)
    {
        await Page.GotoAsync($"{BaseUrl}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForTimeoutAsync(1000);
    }

    // ──────────────────────────────────────────────
    // Home / Dashboard Page
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Home_ShowsDashboardTitle()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Dashboard");
    }

    [Fact]
    public async Task Home_ShowsStatsCards()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.GetByText("Total")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Outlook")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Gmail")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Yahoo")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Success Rate")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Home_ShowsServicesSection()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.GetByText("Services")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Capsolver")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Nopecha")).ToBeVisibleAsync();
        await Expect(Page.GetByText("SmsPool")).ToBeVisibleAsync();
        await Expect(Page.GetByText("5Sim")).ToBeVisibleAsync();
        await Expect(Page.GetByText("GetsmsCode")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Home_ShowsRecentSection()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.GetByText("Recent")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Home_ShowsActivityChart()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.GetByText("Activity")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Last 7 days")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Home_ShowsProvidersSection()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.GetByText("Providers")).ToBeVisibleAsync();
    }

    // ──────────────────────────────────────────────
    // Create Account Page
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAccount_ShowsPageTitle()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Create Accounts");
    }

    [Fact]
    public async Task CreateAccount_ShowsProviderDropdown()
    {
        await GotoAndWaitForBlazor("/create");

        var dropdown = Page.Locator("select#provider");
        await Expect(dropdown).ToBeVisibleAsync();
        await Expect(dropdown.Locator("option")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task CreateAccount_SelectOutlookProvider()
    {
        await GotoAndWaitForBlazor("/create");

        var dropdown = Page.Locator("select#provider");
        await dropdown.SelectOptionAsync("outlook");

        await Expect(Page.GetByText("Captcha solver required")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Easiest")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_SelectGmailProvider()
    {
        await GotoAndWaitForBlazor("/create");

        var dropdown = Page.Locator("select#provider");
        await dropdown.SelectOptionAsync("gmail");

        await Expect(Page.GetByText("SMS verification required")).ToBeVisibleAsync();
        await Expect(Page.GetByText("SMS Required")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_SelectYahooProvider()
    {
        await GotoAndWaitForBlazor("/create");

        var dropdown = Page.Locator("select#provider");
        await dropdown.SelectOptionAsync("yahoo");

        await Expect(Page.GetByText("Captcha + SMS required")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Hardest")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_ShowsProxyCheckbox()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("Use proxy")).ToBeVisibleAsync();
        await Expect(Page.Locator("#useProxy")).ToBeCheckedAsync();
    }

    [Fact]
    public async Task CreateAccount_CanToggleProxy()
    {
        await GotoAndWaitForBlazor("/create");

        var checkbox = Page.Locator("#useProxy");
        await Expect(checkbox).ToBeCheckedAsync();

        await checkbox.UncheckAsync();
        await Expect(checkbox).Not.ToBeCheckedAsync();

        await checkbox.CheckAsync();
        await Expect(checkbox).ToBeCheckedAsync();
    }

    [Fact]
    public async Task CreateAccount_ButtonDisabledWhenNoProviderSelected()
    {
        await GotoAndWaitForBlazor("/create");

        var dropdown = Page.Locator("select#provider");
        await dropdown.SelectOptionAsync("");

        var button = Page.Locator("button").Filter(new() { HasText = "Account" });
        await Expect(button).ToBeDisabledAsync();
    }

    [Fact]
    public async Task CreateAccount_ButtonEnabledAfterSelectingProvider()
    {
        await GotoAndWaitForBlazor("/create");

        var dropdown = Page.Locator("select#provider");
        await dropdown.SelectOptionAsync("outlook");

        var button = Page.Locator("button").Filter(new() { HasText = "Create" });
        await Expect(button).ToBeEnabledAsync();
    }

    [Fact]
    public async Task CreateAccount_ShowsProviderDescriptions()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("Captcha solver required")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_OptionsSectionVisible()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("Options", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_ShowsHeadlessCheckbox()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("Headless")).ToBeVisibleAsync();
        await Expect(Page.Locator("#headless")).ToBeCheckedAsync();
    }

    [Fact]
    public async Task CreateAccount_ShowsQuantityInput()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("Quantity")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_ShowsBatchLabelInput()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("Batch Label")).ToBeVisibleAsync();
    }

    // ──────────────────────────────────────────────
    // History Page
    // ──────────────────────────────────────────────

    [Fact]
    public async Task History_ShowsPageTitle()
    {
        await GotoAndWaitForBlazor("/history");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("History");
    }

    [Fact]
    public async Task History_ShowsFilterControls()
    {
        await GotoAndWaitForBlazor("/history");

        await Expect(Page.GetByText("results")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Today")).ToBeVisibleAsync();
        await Expect(Page.GetByText("All", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task History_ShowsProviderFilterDropdown()
    {
        await GotoAndWaitForBlazor("/history");

        var dropdown = Page.GetByText("All Providers").Locator("xpath=ancestor::select");
        await Expect(dropdown).ToBeVisibleAsync();
    }

    [Fact]
    public async Task History_FilterByProvider()
    {
        await GotoAndWaitForBlazor("/history");

        var dropdown = Page.Locator("select").Filter(new() { HasText = "All Providers" });
        await dropdown.SelectOptionAsync("Outlook");
        await Expect(dropdown).ToHaveValueAsync("Outlook");

        await dropdown.SelectOptionAsync("Gmail");
        await Expect(dropdown).ToHaveValueAsync("Gmail");

        await dropdown.SelectOptionAsync("");
        await Expect(dropdown).ToHaveValueAsync("");
    }

    [Fact]
    public async Task History_ShowsTableOrEmptyState()
    {
        await GotoAndWaitForBlazor("/history");

        var table = Page.Locator("table");
        var emptyMessage = Page.GetByText("No accounts in history");

        var tableVisible = await table.IsVisibleAsync();
        var emptyVisible = await emptyMessage.IsVisibleAsync();
        Assert.True(tableVisible || emptyVisible, "Either table or empty message should be shown");
    }

    [Fact]
    public async Task History_ShowsSearchInput()
    {
        await GotoAndWaitForBlazor("/history");

        var searchInput = Page.Locator("input[placeholder='Search email...']");
        await Expect(searchInput).ToBeVisibleAsync();
    }

    [Fact]
    public async Task History_ShowsExportButton()
    {
        await GotoAndWaitForBlazor("/history");

        await Expect(Page.GetByText("Export CSV")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task History_ShowsPaginationControls()
    {
        await GotoAndWaitForBlazor("/history");

        await Expect(Page.GetByText("Per page:")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task History_TableHasCorrectColumns()
    {
        await GotoAndWaitForBlazor("/history");

        var table = Page.Locator("table");
        if (await table.IsVisibleAsync())
        {
            await Expect(table.Locator("th")).ToHaveCountAsync(7);
        }
    }

    // ──────────────────────────────────────────────
    // Proxies Page
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Proxies_ShowsPageTitle()
    {
        await GotoAndWaitForBlazor("/proxies");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Proxies");
    }

    [Fact]
    public async Task Proxies_ShowsAddProxySection()
    {
        await GotoAndWaitForBlazor("/proxies");

        await Expect(Page.GetByText("Add Proxy", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("http://user:pass@host:port")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Proxies_ShowsInputField()
    {
        await GotoAndWaitForBlazor("/proxies");

        var input = Page.Locator("input[placeholder='http://user:pass@host:port']");
        await Expect(input).ToBeVisibleAsync();
        await Expect(input).ToBeEmptyAsync();
    }

    [Fact]
    public async Task Proxies_AddButtonDisabledWhenEmpty()
    {
        await GotoAndWaitForBlazor("/proxies");

        var addButton = Page.Locator("button").Filter(new() { HasText = "Add" });
        await Expect(addButton).ToBeDisabledAsync();
    }

    [Fact]
    public async Task Proxies_AddButtonEnabledWhenInputHasValue()
    {
        await GotoAndWaitForBlazor("/proxies");

        var input = Page.Locator("input[placeholder='http://user:pass@host:port']");
        await input.FillAsync("http://test:pass@proxy.example.com:8080");

        var addButton = Page.Locator("button").Filter(new() { HasText = "Add" });
        await Expect(addButton).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Proxies_ShowsConfiguredProxiesSection()
    {
        await GotoAndWaitForBlazor("/proxies");

        await Expect(Page.GetByText("Configured Proxies")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Proxies_CanTypeInInputField()
    {
        await GotoAndWaitForBlazor("/proxies");

        var input = Page.Locator("input[placeholder='http://user:pass@host:port']");
        var testProxy = "http://user:pass@192.168.1.1:3128";
        await input.FillAsync(testProxy);

        await Expect(input).ToHaveValueAsync(testProxy);
    }

    [Fact]
    public async Task Proxies_ShowsEmptyState()
    {
        await GotoAndWaitForBlazor("/proxies");

        var emptyMsg = Page.GetByText("No proxies configured");
        if (await emptyMsg.IsVisibleAsync())
        {
            await Expect(Page.GetByText("Add one above")).ToBeVisibleAsync();
        }
    }

    // ──────────────────────────────────────────────
    // Settings Page
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Settings_ShowsPageTitle()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Settings");
    }

    [Fact]
    public async Task Settings_ShowsBrowserSection()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Browser", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Default browser for account creation.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_BrowserDropdownHasOptions()
    {
        await GotoAndWaitForBlazor("/settings");

        var browserCard = Page.Locator("text=Browser").Locator("xpath=ancestor::div[contains(@class,'space-y')][1]");
        var browserDropdown = browserCard.Locator("select");
        await Expect(browserDropdown.Locator("option")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task Settings_CanSelectBrowser()
    {
        await GotoAndWaitForBlazor("/settings");

        var browserCard = Page.Locator("text=Default browser for account creation.").Locator("xpath=ancestor::div[contains(@class,'space-y')][1]");
        var browserDropdown = browserCard.Locator("select");

        await browserDropdown.SelectOptionAsync("chrome");
        await Expect(browserDropdown).ToHaveValueAsync("chrome");

        await browserDropdown.SelectOptionAsync("undetected-chrome");
        await Expect(browserDropdown).ToHaveValueAsync("undetected-chrome");

        await browserDropdown.SelectOptionAsync("firefox");
        await Expect(browserDropdown).ToHaveValueAsync("firefox");
    }

    [Fact]
    public async Task Settings_ShowsCaptchaServicesSection()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Captcha Services", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Select a captcha solving service")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_CaptchaServiceDropdownExists()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Captcha Service")).ToBeVisibleAsync();

        var captchaDropdown = Page.Locator("select").Filter(new() { HasText = "Capsolver" });
        await Expect(captchaDropdown).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_SelectingCapsolverShowsKeyInput()
    {
        await GotoAndWaitForBlazor("/settings");

        var captchaDropdown = Page.Locator("select").Filter(new() { HasText = "Capsolver" });
        await captchaDropdown.SelectOptionAsync("capsolver");

        await Expect(Page.GetByText("Capsolver API Key")).ToBeVisibleAsync();
        var input = Page.Locator("input[placeholder*='Capsolver API key']");
        await Expect(input).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_SelectingNopechaShowsKeyInput()
    {
        await GotoAndWaitForBlazor("/settings");

        var captchaDropdown = Page.Locator("select").Filter(new() { HasText = "Capsolver" });
        await captchaDropdown.SelectOptionAsync("nopecha");

        await Expect(Page.GetByText("Nopecha API Key")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_ShowsSmsServicesSection()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("SMS Services", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Select an SMS verification service")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_SmsServiceDropdownExists()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Default SMS Service")).ToBeVisibleAsync();

        var smsDropdown = Page.Locator("select").Filter(new() { HasText = "SmsPool" });
        await Expect(smsDropdown).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_SelectingSmsPoolShowsTokenInput()
    {
        await GotoAndWaitForBlazor("/settings");

        var smsDropdown = Page.Locator("select").Filter(new() { HasText = "SmsPool" });
        await smsDropdown.SelectOptionAsync("smspool");

        await Expect(Page.GetByText("SmsPool Token")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_Selecting5SimShowsTokenInput()
    {
        await GotoAndWaitForBlazor("/settings");

        var smsDropdown = Page.Locator("select").Filter(new() { HasText = "SmsPool" });
        await smsDropdown.SelectOptionAsync("5sim");

        await Expect(Page.GetByText("5Sim API Key")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_SelectingGetsmsCodeShowsInputs()
    {
        await GotoAndWaitForBlazor("/settings");

        var smsDropdown = Page.Locator("select").Filter(new() { HasText = "SmsPool" });
        await smsDropdown.SelectOptionAsync("getsmscode");

        await Expect(Page.GetByText("GetsmsCode Username")).ToBeVisibleAsync();
        await Expect(Page.GetByText("GetsmsCode Token")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_GetsmsCodeUsernameIsNotPassword()
    {
        await GotoAndWaitForBlazor("/settings");

        var smsDropdown = Page.Locator("select").Filter(new() { HasText = "SmsPool" });
        await smsDropdown.SelectOptionAsync("getsmscode");

        var usernameInput = Page.Locator("input[placeholder='Enter username...']");
        await Expect(usernameInput).ToBeVisibleAsync();
        await Expect(usernameInput).Not.ToHaveAttributeAsync("type", "password");
    }

    [Fact]
    public async Task Settings_SaveButtonExists()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Save Settings")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Save Settings")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Settings_CanFillCapsolverKey()
    {
        await GotoAndWaitForBlazor("/settings");

        var captchaDropdown = Page.Locator("select").Filter(new() { HasText = "Capsolver" });
        await captchaDropdown.SelectOptionAsync("capsolver");

        var input = Page.Locator("input[placeholder*='Capsolver API key']");
        await input.FillAsync("test-api-key-12345");
        await Expect(input).ToHaveValueAsync("test-api-key-12345");
    }

    [Fact]
    public async Task Settings_ShowsAppearanceSection()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Appearance")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Compact Mode")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_ShowsBackupSection()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Backup & Restore")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Export Settings")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Import Settings")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_ShowsDangerZone()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Danger Zone")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Reset all settings")).ToBeVisibleAsync();
    }

    // ──────────────────────────────────────────────
    // Navigation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Navigation_CanNavigateToAllPages()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Dashboard");

        await Page.Locator("nav a").Filter(new() { HasText = "Create Account" }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Create Accounts");

        await Page.Locator("nav a").Filter(new() { HasText = "History" }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("History");

        await Page.Locator("nav a").Filter(new() { HasText = "Proxies" }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Proxies");

        await Page.Locator("nav a").Filter(new() { HasText = "Settings" }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Settings");

        await Page.Locator("nav a").Filter(new() { HasText = "Dashboard" }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Dashboard");
    }

    [Fact]
    public async Task Navigation_TestPageWorks()
    {
        await GotoAndWaitForBlazor("/test");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Test Page");
        await Expect(Page.GetByText("If you can see this, routing works.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Navigation_TestPageGoHomeButton()
    {
        await GotoAndWaitForBlazor("/test");

        await Page.GetByText("Go Home").ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Dashboard");
    }

    // ──────────────────────────────────────────────
    // Not Found Page
    // ──────────────────────────────────────────────

    [Fact]
    public async Task NotFound_ShowsNotFoundPage()
    {
        await GotoAndWaitForBlazor("/this-page-does-not-exist");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Not Found");
    }

    [Fact]
    public async Task NotFound_ShowsMessage()
    {
        await GotoAndWaitForBlazor("/nonexistent");

        await Expect(Page.GetByText("The page you requested could not be found.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task NotFound_GoToDashboardButton()
    {
        await GotoAndWaitForBlazor("/some/random/path");

        await Expect(Page.GetByText("Go to Dashboard")).ToBeVisibleAsync();
        await Page.GetByText("Go to Dashboard").ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Dashboard");
    }

    // ──────────────────────────────────────────────
    // Layout
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Layout_NavMenuHasAllLinks()
    {
        await GotoAndWaitForBlazor("/");

        var nav = Page.Locator("nav");
        await Expect(nav).ToBeVisibleAsync();

        await Expect(nav.GetByText("Dashboard")).ToBeVisibleAsync();
        await Expect(nav.GetByText("Create Account")).ToBeVisibleAsync();
        await Expect(nav.GetByText("History")).ToBeVisibleAsync();
        await Expect(nav.GetByText("Proxies")).ToBeVisibleAsync();
        await Expect(nav.GetByText("Settings")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Layout_NavMenuHasCorrectLinkCount()
    {
        await GotoAndWaitForBlazor("/");

        var navLinks = Page.Locator("nav a");
        await Expect(navLinks).ToHaveCountAsync(5);
    }

    [Fact]
    public async Task Layout_ShowsSidebar()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.Locator("aside")).ToBeVisibleAsync();
        await Expect(Page.GetByText("RegMailNet")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Layout_ShowsQuickNavShortcuts()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.GetByText("Quick nav:")).ToBeVisibleAsync();
        await Expect(Page.Locator("kbd")).ToHaveCountAsync(5);
    }
}
