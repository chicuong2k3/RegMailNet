using FluentAssertions;
using RegMailNet.Browser;
using Xunit;

namespace RegMailNet.Tests.Browser;

public class CaptchaKeyInfoTests
{
    [Fact]
    public void Constructor_SetsNameAndKey()
    {
        var info = new CaptchaKeyInfo("capsolver", "api-key-123");

        info.Name.Should().Be("capsolver");
        info.Key.Should().Be("api-key-123");
    }

    [Fact]
    public void Record_Equality_Works()
    {
        var info1 = new CaptchaKeyInfo("capsolver", "key123");
        var info2 = new CaptchaKeyInfo("capsolver", "key123");

        info1.Should().Be(info2);
        (info1 == info2).Should().BeTrue();
    }

    [Fact]
    public void Record_Inequality_DifferentName()
    {
        var info1 = new CaptchaKeyInfo("capsolver", "key123");
        var info2 = new CaptchaKeyInfo("nopecha", "key123");

        info1.Should().NotBe(info2);
    }

    [Fact]
    public void Record_Inequality_DifferentKey()
    {
        var info1 = new CaptchaKeyInfo("capsolver", "key123");
        var info2 = new CaptchaKeyInfo("capsolver", "key456");

        info1.Should().NotBe(info2);
    }
}
