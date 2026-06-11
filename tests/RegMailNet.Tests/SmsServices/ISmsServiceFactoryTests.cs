using FluentAssertions;
using RegMailNet.SmsServices;
using Xunit;

namespace RegMailNet.Tests.SmsServices;

public class ISmsServiceFactoryTests
{
    [Fact]
    public void ISmsServiceFactory_HasCreateMethod()
    {
        var method = typeof(ISmsServiceFactory).GetMethod("Create");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(ISmsService));
    }

    [Fact]
    public void ISmsServiceFactory_CreateMethodParameters_AreCorrect()
    {
        var method = typeof(ISmsServiceFactory).GetMethod("Create");
        var parameters = method!.GetParameters();

        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(Dictionary<string, string>));
        parameters[1].ParameterType.Should().Be(typeof(string));
    }
}
