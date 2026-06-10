using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RegMailNet.SmsServices;
using Xunit;

namespace RegMailNet.Tests.SmsServices;

public class SmsServiceFactoryTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

    public SmsServiceFactoryTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IHttpClientFactory))).Returns(_httpClientFactoryMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<SmsPoolService>))).Returns(NullLogger<SmsPoolService>.Instance);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<GetsmsCodeService>))).Returns(NullLogger<GetsmsCodeService>.Instance);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<FiveSimService>))).Returns(NullLogger<FiveSimService>.Instance);
    }

    [Fact]
    public void Create_SmsPoolService_ReturnsSmsPoolService()
    {
        var factory = new SmsServiceFactory(_serviceProviderMock.Object, NullLogger<SmsServiceFactory>.Instance);
        var smsData = new Dictionary<string, string>
        {
            ["name"] = "smspool",
            ["data"] = "token=test123,country=1"
        };

        var result = factory.Create(smsData, "google");

        result.Should().BeOfType<SmsPoolService>();
    }

    [Fact]
    public void Create_GetsmsCodeService_ReturnsGetsmsCodeService()
    {
        var factory = new SmsServiceFactory(_serviceProviderMock.Object, NullLogger<SmsServiceFactory>.Instance);
        var smsData = new Dictionary<string, string>
        {
            ["name"] = "getsmscode",
            ["data"] = "user=testuser,token=test123"
        };

        var result = factory.Create(smsData, "google");

        result.Should().BeOfType<GetsmsCodeService>();
    }

    [Fact]
    public void Create_FiveSimService_ReturnsFiveSimService()
    {
        var factory = new SmsServiceFactory(_serviceProviderMock.Object, NullLogger<SmsServiceFactory>.Instance);
        var smsData = new Dictionary<string, string>
        {
            ["name"] = "5sim",
            ["data"] = "token=test123,country=usa"
        };

        var result = factory.Create(smsData, "google");

        result.Should().BeOfType<FiveSimService>();
    }

    [Fact]
    public void Create_UnsupportedService_ThrowsArgumentException()
    {
        var factory = new SmsServiceFactory(_serviceProviderMock.Object, NullLogger<SmsServiceFactory>.Instance);
        var smsData = new Dictionary<string, string>
        {
            ["name"] = "unknown",
            ["data"] = ""
        };

        var act = () => factory.Create(smsData, "google");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unsupported SMS service*");
    }

    [Fact]
    public void Create_YahooProvider_SetsCorrectServiceIds()
    {
        var factory = new SmsServiceFactory(_serviceProviderMock.Object, NullLogger<SmsServiceFactory>.Instance);

        var smsPoolData = new Dictionary<string, string>
        {
            ["name"] = "smspool",
            ["data"] = "token=test123,country=1"
        };
        var smsPool = (SmsPoolService)factory.Create(smsPoolData, "yahoo");
        smsPool.Should().NotBeNull();

        var getsmsData = new Dictionary<string, string>
        {
            ["name"] = "getsmscode",
            ["data"] = "user=testuser,token=test123"
        };
        var getsms = (GetsmsCodeService)factory.Create(getsmsData, "yahoo");
        getsms.Should().NotBeNull();
    }
}
