using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Moq;
using RegMailNet.Browser;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;
using Xunit;

namespace RegMailNet.Tests;

public class RegMailNetManagerTests
{
    private readonly Mock<ISmsServiceFactory> _smsServiceFactoryMock;
    private readonly Mock<IBrowserFactory> _browserFactoryMock;
    private readonly Mock<OutlookProvider> _outlookProviderMock;
    private readonly Mock<GmailProvider> _gmailProviderMock;
    private readonly Mock<YahooProvider> _yahooProviderMock;
    private readonly Mock<IPage> _pageMock;
    private readonly Mock<IBrowserContext> _contextMock;
    private readonly Mock<IFreeProxyService> _freeProxyServiceMock;
    private readonly RegMailNetOptions _options;

    public RegMailNetManagerTests()
    {
        _smsServiceFactoryMock = new Mock<ISmsServiceFactory>();
        _browserFactoryMock = new Mock<IBrowserFactory>();
        _pageMock = new Mock<IPage>();
        _contextMock = new Mock<IBrowserContext>();
        _freeProxyServiceMock = new Mock<IFreeProxyService>();
        _outlookProviderMock = new Mock<OutlookProvider>(NullLogger<OutlookProvider>.Instance);
        _gmailProviderMock = new Mock<GmailProvider>(_smsServiceFactoryMock.Object, NullLogger<GmailProvider>.Instance);
        _yahooProviderMock = new Mock<YahooProvider>(_smsServiceFactoryMock.Object, NullLogger<YahooProvider>.Instance);
        _options = new RegMailNetOptions
        {
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

        _browserFactoryMock
            .Setup(f => f.CreatePageAsync(It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new BrowserPage(_pageMock.Object, _contextMock.Object));
    }

    // ── Constructor validation ────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullBrowserFactory_ThrowsArgumentNullException()
    {
        var act = () => new RegMailNetManager(
            browserFactory: null!,
            smsServiceFactory: _smsServiceFactoryMock.Object,
            options: Options.Create(_options),
            logger: NullLogger<RegMailNetManager>.Instance);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullSmsServiceFactory_ThrowsArgumentNullException()
    {
        var act = () => new RegMailNetManager(
            browserFactory: _browserFactoryMock.Object,
            smsServiceFactory: null!,
            options: Options.Create(_options),
            logger: NullLogger<RegMailNetManager>.Instance);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Captcha key resolution ────────────────────────────────────────────────

    [Fact]
    public void GetCaptchaKey_NoKeyProvided_ThrowsArgumentException()
    {
        var manager = CreateManager();
        var act = () => manager.CreateOutlookAccountAsync();
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*No captcha key*");
    }

    [Fact]
    public void GetCaptchaKey_ProviderWithNoSolverMapping_ThrowsArgumentException()
    {
        var manager = CreateManager(captchaKeys: new Dictionary<string, string> { ["unknown_solver"] = "key" });
        var act = () => manager.CreateOutlookAccountAsync();
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*No captcha key*");
    }

    // ── SMS key resolution ────────────────────────────────────────────────────

    [Fact]
    public void GetSmsKey_NoKeysProvided_ThrowsArgumentException()
    {
        var manager = CreateManager();
        var act = () => manager.CreateGmailAccountAsync();
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*No SMS API keys*");
    }

    // ── Proxy handling ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGmailAccount_WithProvidedProxy_PassesProxyToFactory()
    {
        var manager = CreateManager(
            proxies: new List<string> { "http://myproxy:8080" },
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCreationResult("test@gmail.com", "pass"));

        await manager.CreateGmailAccountAsync();

        _browserFactoryMock.Verify(
            f => f.CreatePageAsync(true, "http://myproxy:8080", true, true),
            Times.Once);
    }

    [Fact]
    public async Task CreateGmailAccount_UseProxyFalse_PassesNullProxy()
    {
        var manager = CreateManager(
            proxies: new List<string> { "http://myproxy:8080" },
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCreationResult("test@gmail.com", "pass"));

        await manager.CreateGmailAccountAsync(useProxy: false);

        _browserFactoryMock.Verify(
            f => f.CreatePageAsync(true, null, true, true),
            Times.Once);
    }

    [Fact]
    public async Task CreateGmailAccount_AutoProxy_CallsFreeProxyService()
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
            .Setup(p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCreationResult("test@gmail.com", "pass"));

        await manager.CreateGmailAccountAsync();

        _freeProxyServiceMock.Verify(s => s.GetProxyAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        _browserFactoryMock.Verify(
            f => f.CreatePageAsync(true, "http://freeproxy:3128", true, true),
            Times.Once);
    }

    [Fact]
    public async Task CreateGmailAccount_AutoProxyNoFreeProxy_PassesNullProxy()
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
            .Setup(p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCreationResult("test@gmail.com", "pass"));

        await manager.CreateGmailAccountAsync();

        _browserFactoryMock.Verify(
            f => f.CreatePageAsync(true, null, true, true),
            Times.Once);
    }

    // ── Account creation ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOutlookAccount_WithValidKeys_CallsProvider()
    {
        var manager = CreateManager(
            captchaKeys: new Dictionary<string, string> { ["capsolver"] = "token" });

        _outlookProviderMock
            .Setup(p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCreationResult("test@outlook.com", "pass"));

        var result = await manager.CreateOutlookAccountAsync(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            country: "US", birthdate: "1-1-1990");

        result.Email.Should().Be("test@outlook.com");
        result.Password.Should().Be("pass");
    }

    [Fact]
    public async Task CreateOutlookAccount_NoInfo_GeneratesAndCallsProvider()
    {
        var manager = CreateManager(
            captchaKeys: new Dictionary<string, string> { ["capsolver"] = "token" });

        _outlookProviderMock
            .Setup(p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCreationResult("generated@outlook.com", "generated"));

        var result = await manager.CreateOutlookAccountAsync();

        result.Email.Should().Be("generated@outlook.com");
    }

    [Fact]
    public async Task CreateGmailAccount_WithValidKeys_CallsProvider()
    {
        var manager = CreateManager(
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCreationResult("test@gmail.com", "pass"));

        var result = await manager.CreateGmailAccountAsync(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            birthdate: "1-1-1990");

        result.Email.Should().Be("test@gmail.com");
    }

    [Fact]
    public async Task CreateYahooAccount_WithValidKeys_CallsProvider()
    {
        var manager = CreateManager(
            captchaKeys: new Dictionary<string, string> { ["capsolver"] = "token" },
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        _yahooProviderMock
            .Setup(p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCreationResult("test@yahoo.com", "pass"));

        var result = await manager.CreateYahooAccountAsync(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            birthdate: "1-1-1990");

        result.Email.Should().Be("test@yahoo.com");
    }

    [Fact]
    public async Task CreateYahooAccount_NoCaptchaKey_ThrowsArgumentException()
    {
        var manager = CreateManager(
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" }
            });

        var act = () => manager.CreateYahooAccountAsync(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            birthdate: "1-1-1990");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*No captcha key*");
    }

    [Fact]
    public async Task CreateYahooAccount_NoSmsKey_ThrowsArgumentException()
    {
        var manager = CreateManager(
            captchaKeys: new Dictionary<string, string> { ["capsolver"] = "token" });

        var act = () => manager.CreateYahooAccountAsync(
            username: "testuser", password: "P@ss123",
            firstName: "John", lastName: "Doe",
            birthdate: "1-1-1990");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*No SMS API keys*");
    }

    // ── SMS key selection ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSmsKey_DefaultServicePresent_UsesDefault()
    {
        var manager = CreateManager(
            smsKeys: new Dictionary<string, Dictionary<string, string>>
            {
                ["smspool"] = new() { ["token"] = "abc" },
                ["5sim"] = new() { ["token"] = "def" }
            });

        _gmailProviderMock
            .Setup(p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<ISmsServiceFactory>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCreationResult("test@gmail.com", "pass"));

        await manager.CreateGmailAccountAsync();

        _gmailProviderMock.Verify(
            p => p.CreateAccountAsync(It.IsAny<IPage>(), It.IsAny<ISmsServiceFactory>(),
                It.Is<Dictionary<string, string>>(d => d["name"] == "smspool"),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private RegMailNetManager CreateManager(
        Dictionary<string, string>? captchaKeys = null,
        Dictionary<string, Dictionary<string, string>>? smsKeys = null,
        List<string>? proxies = null,
        bool autoProxy = false,
        bool headless = true,
        IBrowserFactory? browserFactory = null,
        ISmsServiceFactory? smsServiceFactory = null,
        IFreeProxyService? freeProxyService = null)
    {
        return new RegMailNetManager(
            captchaKeys: captchaKeys,
            smsKeys: smsKeys,
            proxies: proxies,
            autoProxy: autoProxy,
            headless: headless,
            browserFactory: browserFactory ?? _browserFactoryMock.Object,
            smsServiceFactory: smsServiceFactory ?? _smsServiceFactoryMock.Object,
            freeProxyService: freeProxyService,
            outlookProvider: _outlookProviderMock.Object,
            gmailProvider: _gmailProviderMock.Object,
            yahooProvider: _yahooProviderMock.Object,
            dataGenerator: new DataGenerator(),
            options: Options.Create(_options),
            logger: NullLogger<RegMailNetManager>.Instance);
    }
}
