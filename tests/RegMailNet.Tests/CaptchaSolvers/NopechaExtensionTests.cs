using FluentAssertions;
using Microsoft.Playwright;
using Moq;
using RegMailNet.CaptchaSolvers;
using Xunit;

namespace RegMailNet.Tests.CaptchaSolvers;

public class NopechaExtensionTests
{
    private readonly NopechaExtension _extension;

    public NopechaExtensionTests()
    {
        _extension = new NopechaExtension();
    }

    [Fact]
    public void Name_ReturnsNopecha()
    {
        _extension.Name.Should().Be("nopecha");
    }

    [Fact]
    public void ImplementsICaptchaSolver()
    {
        _extension.Should().BeAssignableTo<ICaptchaSolver>();
    }

    [Fact]
    public async Task ConfigureAsync_ExtensionNotFound_ThrowsFileNotFoundException()
    {
        var contextMock = new Mock<IBrowserContext>();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var act = () => _extension.ConfigureAsync(contextMock.Object, nonExistentPath, "api-key");
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ConfigureAsync_ExtensionExists_DoesNotThrow()
    {
        var contextMock = new Mock<IBrowserContext>();

        // Create temp directory with fake xpi
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var xpiPath = Path.Combine(tempDir, "noptcha-0.4.9.xpi");
        await File.WriteAllTextAsync(xpiPath, "fake");

        try
        {
            var act = () => _extension.ConfigureAsync(contextMock.Object, tempDir, "api-key");
            await act.Should().NotThrowAsync();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
