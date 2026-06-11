using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using Moq;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using Xunit;

namespace RegMailNet.Tests.EmailProviders;

public class YahooProviderTests
{
    private readonly Mock<IPage> _pageMock;
    private readonly Mock<ISmsServiceFactory> _smsFactoryMock;
    private readonly YahooProvider _provider;

    public YahooProviderTests()
    {
        _pageMock = new Mock<IPage>();
        _smsFactoryMock = new Mock<ISmsServiceFactory>();
        _provider = new YahooProvider(_smsFactoryMock.Object, NullLogger<YahooProvider>.Instance);

        _pageMock.Setup(p => p.Url).Returns("https://login.yahoo.com/account/create");
    }

    [Fact]
    public void ProviderName_ReturnsYahoo()
    {
        _provider.ProviderName.Should().Be("yahoo");
    }

    [Fact]
    public void ImplementsIEmailProvider()
    {
        _provider.Should().BeAssignableTo<IEmailProvider>();
    }

    [Fact]
    public async Task CreateAccountAsync_9Params_ThrowsNotImplemented()
    {
        var act = () => _provider.CreateAccountAsync(
            _pageMock.Object, "user", "pass", "John", "Doe", "1", "15", "1990");

        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task CreateAccountAsync_11Params_OnFailure_ThrowsAccountCreationException()
    {
        _pageMock.Setup(p => p.GotoAsync(It.IsAny<string>(), null))
            .ThrowsAsync(new Exception("navigation failed"));

        var smsService = new Mock<ISmsService>();
        _smsFactoryMock.Setup(f => f.Create(It.IsAny<Dictionary<string, string>>(), "yahoo"))
            .Returns(smsService.Object);

        var act = () => _provider.CreateAccountAsync(
            _pageMock.Object, _smsFactoryMock.Object,
            new Dictionary<string, string> { ["name"] = "smspool", ["data"] = "token=abc" },
            "testuser", "P@ss123", "John", "Doe", "1", "15", "1990");

        await act.Should().ThrowAsync<AccountCreationException>();
    }

    [Fact]
    public void YahooProvider_HasCorrectSelectors()
    {
        var type = typeof(YahooProvider);
        var nestedType = type.GetNestedType("Sel", System.Reflection.BindingFlags.NonPublic);

        nestedType.Should().NotBeNull();

        var username = nestedType!.GetField("Username", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        username!.GetValue(null).Should().Be("#usernamereg-userId");

        var password = nestedType.GetField("Password", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        password!.GetValue(null).Should().Be("#usernamereg-password");

        var submitBtn = nestedType.GetField("SubmitButton", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        submitBtn!.GetValue(null).Should().Be("#reg-submit-button");

        var recaptchaFrame = nestedType.GetField("RecaptchaFrame", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        recaptchaFrame!.GetValue(null).Should().Be("#recaptcha-iframe");
    }

    [Fact]
    public void YahooProvider_HasDefaultTimeouts()
    {
        var type = typeof(YahooProvider);

        var waitTimeout = type.GetField("WaitTimeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        waitTimeout.Should().NotBeNull();
        waitTimeout!.GetValue(null).Should().Be(25);

        var captchaTimeout = type.GetField("CaptchaSolveTimeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        captchaTimeout.Should().NotBeNull();
        captchaTimeout!.GetValue(null).Should().Be(120);
    }
}
