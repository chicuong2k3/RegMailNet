using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RegMailNet.Browser;
using Xunit;

namespace RegMailNet.Tests.Browser;

public class CamoufoxBrowserFactoryTests
{
    private readonly CamoufoxBrowserFactory _factory;

    public CamoufoxBrowserFactoryTests()
    {
        _factory = new CamoufoxBrowserFactory(NullLogger<CamoufoxBrowserFactory>.Instance);
    }

    [Fact]
    public void Constructor_WithLogger_DoesNotThrow()
    {
        var act = () => new CamoufoxBrowserFactory(NullLogger<CamoufoxBrowserFactory>.Instance);
        act.Should().NotThrow();
    }

    [Fact]
    public void ImplementsIBrowserFactory()
    {
        _factory.Should().BeAssignableTo<IBrowserFactory>();
    }

    // Note: Integration tests for CreatePageAsync require Camoufox binary installed.
    // These are unit-level contract tests. Run with --filter Category=Integration for browser tests.
}
