using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RegMailNet.Browser;
using RegMailNet.Configuration;
using RegMailNet.EmailProviders;
using RegMailNet.SmsServices;
using RegMailNet.Utilities;
using Xunit;

namespace RegMailNet.Tests.Configuration;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRegMailNet_RegistersBrowserFactory()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddRegMailNet(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IBrowserFactory));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(CamoufoxBrowserFactory));
    }

    [Fact]
    public void AddRegMailNet_RegistersDataGenerator()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddRegMailNet(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DataGenerator));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddRegMailNet_RegistersFreeProxyService()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddRegMailNet(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IFreeProxyService));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(FreeProxyService));
    }

    [Fact]
    public void AddRegMailNet_RegistersEmailProviders()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddRegMailNet(configuration);

        services.FirstOrDefault(d => d.ServiceType == typeof(OutlookProvider)).Should().NotBeNull();
        services.FirstOrDefault(d => d.ServiceType == typeof(GmailProvider)).Should().NotBeNull();
        services.FirstOrDefault(d => d.ServiceType == typeof(YahooProvider)).Should().NotBeNull();
    }

    [Fact]
    public void AddRegMailNet_RegistersSmsServiceFactory()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddRegMailNet(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISmsServiceFactory));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(SmsServiceFactory));
    }

    [Fact]
    public void AddRegMailNet_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddRegMailNet(configuration);

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<RegMailNetOptions>>();

        options.Should().NotBeNull();
        options!.Value.DefaultSmsService.Should().Be("smspool");
        options.Value.DefaultCaptchaService.Should().Be("capsolver");
    }

    [Fact]
    public void AddRegMailNet_RegistersAllAsSingleton()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddRegMailNet(configuration);

        var browserFactory = services.FirstOrDefault(d => d.ServiceType == typeof(IBrowserFactory));
        browserFactory!.Lifetime.Should().Be(ServiceLifetime.Singleton);

        var dataGen = services.FirstOrDefault(d => d.ServiceType == typeof(DataGenerator));
        dataGen!.Lifetime.Should().Be(ServiceLifetime.Singleton);

        var smsFactory = services.FirstOrDefault(d => d.ServiceType == typeof(ISmsServiceFactory));
        smsFactory!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    private static IConfiguration CreateConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["RegMailNet:DefaultSmsService"] = "smspool",
            ["RegMailNet:DefaultCaptchaService"] = "capsolver",
            ["RegMailNet:CaptchaServicesSupported:0"] = "capsolver",
            ["RegMailNet:SmsServicesSupported:0"] = "smspool",
            ["RegMailNet:SupportedSolversByEmail:0:EmailService"] = "outlook",
            ["RegMailNet:SupportedSolversByEmail:0:Solvers:0"] = "capsolver",
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}
