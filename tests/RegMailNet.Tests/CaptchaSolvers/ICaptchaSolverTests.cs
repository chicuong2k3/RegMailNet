using FluentAssertions;
using RegMailNet.CaptchaSolvers;
using Xunit;

namespace RegMailNet.Tests.CaptchaSolvers;

public class ICaptchaSolverTests
{
    [Fact]
    public void ICaptchaSolver_HasNameProperty()
    {
        var type = typeof(ICaptchaSolver);
        var prop = type.GetProperty("Name");

        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void ICaptchaSolver_HasConfigureAsyncMethod()
    {
        var type = typeof(ICaptchaSolver);
        var method = type.GetMethod("ConfigureAsync");

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }
}
