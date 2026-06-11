using FluentAssertions;
using Microsoft.Playwright;
using Moq;
using RegMailNet.CaptchaSolvers;
using Xunit;

namespace RegMailNet.Tests.CaptchaSolvers;

public class CapsolverExtensionTests
{
    private readonly CapsolverExtension _extension;

    public CapsolverExtensionTests()
    {
        _extension = new CapsolverExtension();
    }

    [Fact]
    public void Name_ReturnsCapsolver()
    {
        _extension.Name.Should().Be("capsolver");
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
    public async Task ConfigureAsync_ExtensionExists_CallsAddInitScript()
    {
        var contextMock = new Mock<IBrowserContext>();
        contextMock.Setup(c => c.AddInitScriptAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var xpiPath = Path.Combine(tempDir, "capsolver_captcha_solver-1.10.4.xpi");
        await File.WriteAllTextAsync(xpiPath, "fake");

        try
        {
            await _extension.ConfigureAsync(contextMock.Object, tempDir, "my-api-key");

            contextMock.Verify(c => c.AddInitScriptAsync(
                It.Is<string>(s => s.Contains("my-api-key")),
                It.IsAny<string?>()), Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ConfigureAsync_InitScriptContainsApiKey()
    {
        var contextMock = new Mock<IBrowserContext>();
        var capturedScript = "";
        contextMock.Setup(c => c.AddInitScriptAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string?>((script, _) => capturedScript = script)
            .Returns(Task.CompletedTask);

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "capsolver_captcha_solver-1.10.4.xpi"), "fake");

        try
        {
            await _extension.ConfigureAsync(contextMock.Object, tempDir, "SUPER_SECRET_KEY");

            capturedScript.Should().Contain("SUPER_SECRET_KEY");
            capturedScript.Should().Contain("Please input your API key");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
