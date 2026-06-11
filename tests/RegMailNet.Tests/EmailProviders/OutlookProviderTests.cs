using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using Moq;
using RegMailNet.EmailProviders;
using Xunit;

namespace RegMailNet.Tests.EmailProviders;

public class OutlookProviderTests
{
    private readonly Mock<IPage> _pageMock;
    private readonly OutlookProvider _provider;

    public OutlookProviderTests()
    {
        _pageMock = new Mock<IPage>();
        _provider = new OutlookProvider(NullLogger<OutlookProvider>.Instance);
    }

    [Fact]
    public void ProviderName_ReturnsOutlook()
    {
        _provider.ProviderName.Should().Be("outlook");
    }

    [Fact]
    public void ImplementsIEmailProvider()
    {
        _provider.Should().BeAssignableTo<IEmailProvider>();
    }

    [Fact]
    public async Task CreateAccountAsync_9Params_NavigatesToSignupUrl()
    {
        _pageMock.Setup(p => p.GotoAsync(It.IsAny<string>(), null))
            .ThrowsAsync(new Exception("nav fail")); // Force early exit

        try
        {
            await _provider.CreateAccountAsync(
                _pageMock.Object, "testuser", "P@ss123", "John", "Doe",
                "1", "15", "1990");
        }
        catch (AccountCreationException) { }

        _pageMock.Verify(p => p.GotoAsync("https://signup.live.com/signup", null), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_10Params_NavigatesToSignupUrl()
    {
        _pageMock.Setup(p => p.GotoAsync(It.IsAny<string>(), null))
            .ThrowsAsync(new Exception("nav fail"));

        try
        {
            await _provider.CreateAccountAsync(
                _pageMock.Object, "testuser", "P@ss123", "John", "Doe",
                "US", "1", "15", "1990", false);
        }
        catch (AccountCreationException) { }

        _pageMock.Verify(p => p.GotoAsync("https://signup.live.com/signup", null), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_NavigationFails_ThrowsAccountCreationException()
    {
        _pageMock.Setup(p => p.GotoAsync(It.IsAny<string>(), null))
            .ThrowsAsync(new Exception("navigation failed"));

        var act = () => _provider.CreateAccountAsync(
            _pageMock.Object, "testuser", "P@ss123", "John", "Doe",
            "US", "1", "15", "1990", false);

        await act.Should().ThrowAsync<AccountCreationException>();
    }

    [Fact]
    public void OutlookProvider_HasCorrectSelectors()
    {
        var type = typeof(OutlookProvider);
        var nestedType = type.GetNestedType("Sel", System.Reflection.BindingFlags.NonPublic);

        nestedType.Should().NotBeNull();

        var emailSwitch = nestedType!.GetField("EmailSwitch", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        emailSwitch!.GetValue(null).Should().Be("#liveSwitch");

        var usernameInput = nestedType.GetField("UsernameInput", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        usernameInput!.GetValue(null).Should().Be("#usernameInput");

        var passwordInput = nestedType.GetField("PasswordInput", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        passwordInput!.GetValue(null).Should().Be("#Password");

        var captchaFrame = nestedType.GetField("CaptchaFrame", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        captchaFrame!.GetValue(null).Should().Be("#enforcementFrame");
    }

    [Fact]
    public void OutlookProvider_HasDefaultTimeoutConstants()
    {
        var type = typeof(OutlookProvider);

        var waitTimeout = type.GetField("WaitTimeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        waitTimeout.Should().NotBeNull();
        waitTimeout!.GetValue(null).Should().Be(10);

        var maxRetries = type.GetField("MaxCaptchaRetries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        maxRetries.Should().NotBeNull();
        maxRetries!.GetValue(null).Should().Be(3);
    }
}
