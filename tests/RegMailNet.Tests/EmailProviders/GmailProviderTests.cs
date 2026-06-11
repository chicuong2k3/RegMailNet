using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using Moq;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using Xunit;

namespace RegMailNet.Tests.EmailProviders;

public class GmailProviderTests
{
    private readonly Mock<IPage> _pageMock;
    private readonly Mock<ISmsServiceFactory> _smsFactoryMock;
    private readonly GmailProvider _provider;

    public GmailProviderTests()
    {
        _pageMock = new Mock<IPage>();
        _smsFactoryMock = new Mock<ISmsServiceFactory>();
        _provider = new GmailProvider(_smsFactoryMock.Object, NullLogger<GmailProvider>.Instance);

        _pageMock.Setup(p => p.Url).Returns("https://accounts.google.com/signup");
    }

    [Fact]
    public void ProviderName_ReturnsGmail()
    {
        _provider.ProviderName.Should().Be("gmail");
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

        var act = () => _provider.CreateAccountAsync(
            _pageMock.Object, _smsFactoryMock.Object,
            new Dictionary<string, string> { ["name"] = "smspool", ["data"] = "token=abc" },
            "testuser", "P@ss123", "John", "Doe", "1", "15", "1990");

        await act.Should().ThrowAsync<AccountCreationException>();
    }

    [Fact]
    public void GmailProvider_HasCorrectSelectors()
    {
        // Verify the provider class has the expected selector constants via reflection
        var type = typeof(GmailProvider);
        var nestedType = type.GetNestedType("Sel", System.Reflection.BindingFlags.NonPublic);

        nestedType.Should().NotBeNull();

        var firstName = nestedType!.GetField("FirstName", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        firstName.Should().NotBeNull();
        firstName!.GetValue(null).Should().Be("#firstName");

        var lastName = nestedType.GetField("LastName", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        lastName.Should().NotBeNull();
        lastName!.GetValue(null).Should().Be("#lastName");

        var username = nestedType.GetField("Username", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        username.Should().NotBeNull();
        username!.GetValue(null).Should().Be("input[name=\"Username\"]");
    }
}
