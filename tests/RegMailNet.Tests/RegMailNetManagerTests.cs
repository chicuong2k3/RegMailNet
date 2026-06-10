using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OpenQA.Selenium;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;
using RegMailNet.WebDriver;
using Xunit;

namespace RegMailNet.Tests;

public class RegMailNetManagerTests
{
    private readonly Mock<ISmsServiceFactory> _smsServiceFactoryMock;
    private readonly Mock<IWebDriverFactory> _webDriverFactoryMock;
    private readonly Mock<OutlookProvider> _outlookProviderMock;
    private readonly Mock<GmailProvider> _gmailProviderMock;
    private readonly Mock<YahooProvider> _yahooProviderMock;
    private readonly Mock<IWebDriver> _webDriverMock;
    private readonly Mock<IFreeProxyService> _freeProxyServiceMock;
    private readonly RegMailNetOptions _options;

    public RegMailNetManagerTests()
    {
        _smsServiceFactoryMock = new Mock<ISmsServiceFactory>();
        _webDriverFactoryMock = new Mock<IWebDriverFactory>();
        _webDriverMock = new Mock<IWebDriver>();
        _freeProxyServiceMock = new Mock<IFreeProxyService>();
        _outlookProviderMock = new Mock<OutlookProvider>(NullLogger<OutlookProvider>.Instance);
        _gmailProviderMock = new Mock<GmailProvider>(_smsServiceFactoryMock.Object, NullLogger<GmailProvider>.Instance);
        _yahooProviderMock = new Mock<YahooProvider>(_smsServiceFactoryMock.Object, NullLogger<YahooProvider>.Instance);
        _options = new RegMailNetOptions
        {
            SupportedBrowsers = new List<string> { "firefox", "chrome", "undetected-chrome" },
            CaptchaServicesSupported = new List<string> { "capsolver", "nopecha" },
            DefaultCaptchaService = "capsolver",
            SmsServicesSupported = new List<string> { "getsmscode", "smspool", "5sim" },
            DefaultSmsService = "smspool",
            SupportedSolversByEmail = new List<CaptchaSolverMapping>
            {
                new() { EmailService = "outlook", Solvers = new List<string> { "capsolver", "nopecha" } },
                new() { EmailService = "yahoo", Solvers = new List<string> { "capsolver", "nopecha" } }
            }
        };

        _webDriverFactoryMock
            .Setup(f => f.CreateDriver(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CaptchaKeyInfo?>()))
            .Returns(_webDriverMock.Object);
    }

    // ── Constructor validation ────────────────────────────────────────────────

    [Fact]
    public void Constructor_InvalidBrowser_ThrowsArgumentException()
    {
        var act = () => CreateManager(browser: "safari");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unsupported browser*");
    }

    [Theory]
    [InlineData("firefox")]
    [InlineData("chrome")]
    [InlineData("undetected-chrome")]
    public void Constructor_ValidBrowser_DoesNotThrow(string browser)
    {
        var act = () => CreateManager(browser: browser);
        act.Should().NotThrow();
    }

    // ── Captcha key resolution ────────────────────────────────────────────────

    [Fact]
    public void GetCaptchaKey_NoKeyProvided_ThrowsArgumentException()
    {
        var manager = CreateManager();
        var act = () => manager.CreateOutlookAccount();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*No captcha key*");
    }

    [Fact]
    public void GetCaptchaKey_ProviderWithNoSolverMapping_ThrowsArgumentException()
    {
        // Gmail has no captcha solver mapping in config, so it won't throw for captcha
        // but Outlook with no matching key should throw
        var manager = CreateManager(captchaKeys: new Dictionary<string, string> { ["unknown_solver"] = "key" });
        var act = () => manager.CreateOutlookAccount();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*No captcha key*");
    }

    // ── SMS key resolution ────────────────────────────────────────────────────

    [Fact]
    public void GetSmsKey_NoKeysProvided_ThrowsArgumentException()
    {
        var manager = CreateManager();
        var act = () => manager.CreateGmailAccount();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*No SMS API keys*");
    }

    // ── Proxy handling ────────────────────────────────────────────────────────

    [Fact]
    public void CreateGmailAccount_WithProvidedProxy_PassesProxyToDriverFactory()
    {
        var manager = CreateManager(
            proxies: new List<string> { "http://myproxy:8080" },
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new AccountCreationResult("test@gmail.com", "pass"));

        manager.CreateGmailAccount();

        _webDriverFactoryMock.Verify(
            f => f.CreateDriver("firefox", false, "http://myproxy:8080", It.IsAny<CaptchaKeyInfo?>()),
            Times.Once);
    }

    [Fact]
    public void CreateGmailAccount_UseProxyFalse_PassesNullProxy()
    {
        var manager = CreateManager(
            proxies: new List<string> { "http://myproxy:8080" },
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new AccountCreationResult("test@gmail.com", "pass"));

        manager.CreateGmailAccount(useProxy: false);

        _webDriverFactoryMock.Verify(
            f => f.CreateDriver("firefox", false, null, It.IsAny<CaptchaKeyInfo?>()),
            Times.Once);
    }

    [Fact]
    public void CreateGmailAccount_AutoProxy_CallsFreeProxyService()
    {
        _freeProxyServiceMock
            .Setup(s => s.GetProxyAsync(It.IsAny<string[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("http://freeproxy:3128");

        var manager = CreateManager(
            autoProxy: true,
            freeProxyService: _freeProxyServiceMock.Object,
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new AccountCreationResult("test@gmail.com", "pass"));

        manager.CreateGmailAccount();

        _freeProxyServiceMock.Verify(s => s.GetProxyAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        _webDriverFactoryMock.Verify(
            f => f.CreateDriver("firefox", false, "http://freeproxy:3128", It.IsAny<CaptchaKeyInfo?>()),
            Times.Once);
    }

    [Fact]
    public void CreateGmailAccount_AutoProxyNoFreeProxy_PassesNullProxy()
    {
        _freeProxyServiceMock
            .Setup(s => s.GetProxyAsync(It.IsAny<string[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var manager = CreateManager(
            autoProxy: true,
            freeProxyService: _freeProxyServiceMock.Object,
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new AccountCreationResult("test@gmail.com", "pass"));

        manager.CreateGmailAccount();

        _webDriverFactoryMock.Verify(
            f => f.CreateDriver("firefox", false, null, It.IsAny<CaptchaKeyInfo?>()),
            Times.Once);
    }

    // ── Account creation ──────────────────────────────────────────────────────

    [Fact]
    public void CreateOutlookAccount_WithValidKeys_CallsProvider()
    {
        var manager = CreateManager(
            captchaKeys: new Dictionary<string, string> { ["capsolver"] = "token" });

        _outlookProviderMock
            .Setup(p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new AccountCreationResult("test@outlook.com", "pass"));

        var result = manager.CreateOutlookAccount(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            country: "US", birthdate: "1-1-1990");

        result.Email.Should().Be("test@outlook.com");
        result.Password.Should().Be("pass");
    }

    [Fact]
    public void CreateOutlookAccount_NoInfo_GeneratesAndCallsProvider()
    {
        var manager = CreateManager(
            captchaKeys: new Dictionary<string, string> { ["capsolver"] = "token" });

        _outlookProviderMock
            .Setup(p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new AccountCreationResult("generated@outlook.com", "generated"));

        var result = manager.CreateOutlookAccount();

        result.Email.Should().Be("generated@outlook.com");
    }

    [Fact]
    public void CreateGmailAccount_WithValidKeys_CallsProvider()
    {
        var manager = CreateManager(
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new AccountCreationResult("test@gmail.com", "pass"));

        var result = manager.CreateGmailAccount(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            birthdate: "1-1-1990");

        result.Email.Should().Be("test@gmail.com");
    }

    [Fact]
    public void CreateYahooAccount_WithValidKeys_CallsProvider()
    {
        var manager = CreateManager(
            captchaKeys: new Dictionary<string, string> { ["capsolver"] = "token" },
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _yahooProviderMock
            .Setup(p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new AccountCreationResult("test@yahoo.com", "pass"));

        var result = manager.CreateYahooAccount(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            birthdate: "1-1-1990");

        result.Email.Should().Be("test@yahoo.com");
    }

    [Fact]
    public void CreateYahooAccount_NoCaptchaKey_ThrowsArgumentException()
    {
        var manager = CreateManager(
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        var act = () => manager.CreateYahooAccount(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            birthdate: "1-1-1990");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*No captcha key*");
    }

    [Fact]
    public void CreateYahooAccount_NoSmsKey_ThrowsArgumentException()
    {
        var manager = CreateManager(
            captchaKeys: new Dictionary<string, string> { ["capsolver"] = "token" });

        var act = () => manager.CreateYahooAccount(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            birthdate: "1-1-1990");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*No SMS API keys*");
    }

    // ── SMS key selection ─────────────────────────────────────────────────────

    [Fact]
    public void GetSmsKey_DefaultServicePresent_UsesDefault()
    {
        var manager = CreateManager(
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" },
                ["5sim"] = new() { ["token"] = "def" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new AccountCreationResult("test@gmail.com", "pass"));

        manager.CreateGmailAccount();

        _gmailProviderMock.Verify(
            p => p.CreateAccount(It.IsAny<IWebDriver>(), It.IsAny<ISmsServiceFactory>(),
                It.Is<Dictionary<string, string>>(d => d["name"] == "smspool"),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private RegMailNetManager CreateManager(
        string browser = "firefox",
        Dictionary<string, string>? captchaKeys = null,
        Dictionary<string, Dictionary<string, string>>? smsKeys = null,
        List<string>? proxies = null,
        bool autoProxy = false,
        IFreeProxyService? freeProxyService = null)
    {
        return new RegMailNetManager(
            browser: browser,
            captchaKeys: captchaKeys,
            smsKeys: smsKeys,
            proxies: proxies,
            autoProxy: autoProxy,
            webDriverFactory: _webDriverFactoryMock.Object,
            smsServiceFactory: _smsServiceFactoryMock.Object,
            freeProxyService: freeProxyService,
            outlookProvider: _outlookProviderMock.Object,
            gmailProvider: _gmailProviderMock.Object,
            yahooProvider: _yahooProviderMock.Object,
            dataGenerator: new DataGenerator(),
            options: Options.Create(_options),
            logger: NullLogger<RegMailNetManager>.Instance);
    }
}
