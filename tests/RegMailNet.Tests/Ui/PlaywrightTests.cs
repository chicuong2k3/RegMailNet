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

        await Expect(Page.GetByText("Total Created")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Outlook")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Gmail")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Yahoo")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Home_ShowsServiceStatusSection()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.GetByText("Service Status")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Capsolver —")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Nopecha —")).ToBeVisibleAsync();
        await Expect(Page.GetByText("SmsPool —")).ToBeVisibleAsync();
        await Expect(Page.GetByText("5Sim —")).ToBeVisibleAsync();
        await Expect(Page.GetByText("GetsmsCode —")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Home_ShowsRecentActivitySection()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.GetByText("Recent Activity")).ToBeVisibleAsync();
    }

    // ──────────────────────────────────────────────
    // Create Account Page
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAccount_ShowsPageTitle()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Create Account");
    }

    [Fact]
    public async Task CreateAccount_ShowsAllProviderCards()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("@outlook.com")).ToBeVisibleAsync();
        await Expect(Page.GetByText("@gmail.com")).ToBeVisibleAsync();
        await Expect(Page.GetByText("@yahoo.com")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_SelectOutlookProvider()
    {
        await GotoAndWaitForBlazor("/create");

        await Page.Locator(".cursor-pointer").First.ClickAsync();

        await Expect(Page.GetByText("Create OUTLOOK Account")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_SelectGmailProvider()
    {
        await GotoAndWaitForBlazor("/create");

        await Page.Locator(".cursor-pointer").Nth(1).ClickAsync();

        await Expect(Page.GetByText("Create GMAIL Account")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_SelectYahooProvider()
    {
        await GotoAndWaitForBlazor("/create");

        await Page.Locator(".cursor-pointer").Nth(2).ClickAsync();

        await Expect(Page.GetByText("Create YAHOO Account")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_ProviderSelectionShowsRing()
    {
        await GotoAndWaitForBlazor("/create");

        var outlookCard = Page.Locator(".cursor-pointer").First;
        await outlookCard.ClickAsync();

        await Expect(outlookCard).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("ring-2"));
    }

    [Fact]
    public async Task CreateAccount_ShowsProxyCheckbox()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("Use proxy")).ToBeVisibleAsync();
        await Expect(Page.Locator("input[type='checkbox']")).ToBeCheckedAsync();
    }

    [Fact]
    public async Task CreateAccount_CanToggleProxy()
    {
        await GotoAndWaitForBlazor("/create");

        var checkbox = Page.Locator("input[type='checkbox']");
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

        var button = Page.Locator("button").Filter(new() { HasText = "Account" });
        await Expect(button).ToBeDisabledAsync();
    }

    [Fact]
    public async Task CreateAccount_ButtonEnabledAfterSelectingProvider()
    {
        await GotoAndWaitForBlazor("/create");

        await Page.Locator(".cursor-pointer").First.ClickAsync();

        await Expect(Page.Locator("button").Filter(new() { HasText = "Create OUTLOOK Account" })).ToBeEnabledAsync();
    }

    [Fact]
    public async Task CreateAccount_ShowsProviderDescriptions()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("Captcha solver required")).ToBeVisibleAsync();
        await Expect(Page.GetByText("SMS verification required")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Captcha + SMS required")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateAccount_OptionsSectionVisible()
    {
        await GotoAndWaitForBlazor("/create");

        await Expect(Page.GetByText("Options", new() { Exact = true })).ToBeVisibleAsync();
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
    public async Task History_ShowsAccountsCard()
    {
        await GotoAndWaitForBlazor("/history");

        await Expect(Page.GetByText("Accounts", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task History_ShowsProviderFilterDropdown()
    {
        await GotoAndWaitForBlazor("/history");

        var dropdown = Page.Locator("select");
        await Expect(dropdown).ToBeVisibleAsync();
        await Expect(dropdown.Locator("option")).ToHaveCountAsync(4);
    }

    [Fact]
    public async Task History_FilterByProvider()
    {
        await GotoAndWaitForBlazor("/history");

        var dropdown = Page.Locator("select");

        await dropdown.SelectOptionAsync("Outlook");
        await Expect(dropdown).ToHaveValueAsync("Outlook");

        await dropdown.SelectOptionAsync("Gmail");
        await Expect(dropdown).ToHaveValueAsync("Gmail");

        await dropdown.SelectOptionAsync("Yahoo");
        await Expect(dropdown).ToHaveValueAsync("Yahoo");

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
    public async Task History_TableHasCorrectColumns()
    {
        await GotoAndWaitForBlazor("/history");

        var table = Page.Locator("table");
        if (await table.IsVisibleAsync())
        {
            await Expect(table.Locator("th")).ToHaveCountAsync(5);
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

        await Expect(Page.GetByText("Add", new() { Exact = true })).ToBeDisabledAsync();
    }

    [Fact]
    public async Task Proxies_AddButtonEnabledWhenInputHasValue()
    {
        await GotoAndWaitForBlazor("/proxies");

        var input = Page.Locator("input[placeholder='http://user:pass@host:port']");
        await input.FillAsync("http://test:pass@proxy.example.com:8080");

        await Expect(Page.GetByText("Add", new() { Exact = true })).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Proxies_ShowsConfiguredProxiesSection()
    {
        await GotoAndWaitForBlazor("/proxies");

        await Expect(Page.GetByText("Configured Proxies")).ToBeVisibleAsync();
        await Expect(Page.GetByText("proxy(ies) configured")).ToBeVisibleAsync();
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
        await Expect(Page.GetByText("Default browser for account creation")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_BrowserDropdownHasOptions()
    {
        await GotoAndWaitForBlazor("/settings");

        var browserDropdown = Page.Locator("select").First;
        await Expect(browserDropdown.Locator("option")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task Settings_CanSelectBrowser()
    {
        await GotoAndWaitForBlazor("/settings");

        var browserDropdown = Page.Locator("select").First;

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
        await Expect(Page.GetByText("API keys for captcha solving services")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_CapsolverKeyInputExists()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Capsolver API Key")).ToBeVisibleAsync();

        var input = Page.Locator("input[type='password']").First;
        await Expect(input).ToBeVisibleAsync();
        await Expect(input).ToHaveAttributeAsync("placeholder", "Enter key...");
    }

    [Fact]
    public async Task Settings_NopechaKeyInputExists()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Nopecha API Key")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_ShowsSmsServicesSection()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("SMS Services", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("API keys for phone verification services")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_DefaultSmsServiceDropdownHasOptions()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("Default SMS Service")).ToBeVisibleAsync();

        var smsDropdown = Page.Locator("select").Nth(1);
        await Expect(smsDropdown.Locator("option")).ToHaveCountAsync(3);
    }

    [Fact]
    public async Task Settings_CanSelectSmsService()
    {
        await GotoAndWaitForBlazor("/settings");

        var smsDropdown = Page.Locator("select").Nth(1);

        await smsDropdown.SelectOptionAsync("5sim");
        await Expect(smsDropdown).ToHaveValueAsync("5sim");

        await smsDropdown.SelectOptionAsync("getsmscode");
        await Expect(smsDropdown).ToHaveValueAsync("getsmscode");

        await smsDropdown.SelectOptionAsync("smspool");
        await Expect(smsDropdown).ToHaveValueAsync("smspool");
    }

    [Fact]
    public async Task Settings_SmsInputsExist()
    {
        await GotoAndWaitForBlazor("/settings");

        await Expect(Page.GetByText("SmsPool Token")).ToBeVisibleAsync();
        await Expect(Page.GetByText("5Sim Token")).ToBeVisibleAsync();
        await Expect(Page.GetByText("GetsmsCode Username")).ToBeVisibleAsync();
        await Expect(Page.GetByText("GetsmsCode Token")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Settings_GetsmsCodeUsernameIsNotPassword()
    {
        await GotoAndWaitForBlazor("/settings");

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

        var input = Page.Locator("input[type='password']").First;
        await input.FillAsync("test-api-key-12345");
        await Expect(input).ToHaveValueAsync("test-api-key-12345");
    }

    // ──────────────────────────────────────────────
    // Navigation
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Navigation_CanNavigateToAllPages()
    {
        await GotoAndWaitForBlazor("/");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Dashboard");

        await Page.GetByText("Create Account", new() { Exact = true }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Create Account");

        await Page.GetByText("History", new() { Exact = true }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("History");

        await Page.GetByText("Proxies", new() { Exact = true }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Proxies");

        await Page.GetByText("Settings", new() { Exact = true }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Settings");

        await Page.GetByText("Dashboard", new() { Exact = true }).ClickAsync();
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
        await Expect(nav.GetByText("TEST PAGE")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Layout_NavMenuHasCorrectButtonCount()
    {
        await GotoAndWaitForBlazor("/");

        var navButtons = Page.Locator("nav button");
        await Expect(navButtons).ToHaveCountAsync(6);
    }
}
