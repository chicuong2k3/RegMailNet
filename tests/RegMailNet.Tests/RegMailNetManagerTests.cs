using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
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
    private readonly RegMailNetOptions _options;

    public RegMailNetManagerTests()
    {
        _smsServiceFactoryMock = new Mock<ISmsServiceFactory>();
        _webDriverFactoryMock = new Mock<IWebDriverFactory>();
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
    }

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

    [Fact]
    public void GetCaptchaKey_NoKeyProvided_ThrowsArgumentException()
    {
        var manager = CreateManager();
        var act = () => manager.CreateOutlookAccount();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*No captcha key*");
    }

    [Fact]
    public void GetSmsKey_NoKeysProvided_ThrowsArgumentException()
    {
        var manager = CreateManager();
        var act = () => manager.CreateGmailAccount();
        act.Should().Throw<ArgumentException>()
            .WithMessage("*No SMS API keys*");
    }

    private RegMailNetManager CreateManager(
        string browser = "firefox",
        Dictionary<string, string>? captchaKeys = null,
        Dictionary<string, Dictionary<string, string>>? smsKeys = null)
    {
        return new RegMailNetManager(
            browser: browser,
            captchaKeys: captchaKeys,
            smsKeys: smsKeys,
            webDriverFactory: _webDriverFactoryMock.Object,
            smsServiceFactory: _smsServiceFactoryMock.Object,
            outlookProvider: _outlookProviderMock.Object,
            gmailProvider: _gmailProviderMock.Object,
            yahooProvider: _yahooProviderMock.Object,
            dataGenerator: new DataGenerator(),
            options: Options.Create(_options),
            logger: NullLogger<RegMailNetManager>.Instance);
    }
}
