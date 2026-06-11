using FluentAssertions;
using Microsoft.Playwright;
using Moq;
using RegMailNet.Utilities;
using Xunit;

namespace RegMailNet.Tests.Utilities;

/// <summary>
/// Tests for WebHelpers static utility class.
/// Note: Playwright ILocator/IPage methods use optional params that can't be used in Moq expression trees.
/// These tests verify the helper logic using loose mocks where possible.
/// </summary>
public class WebHelpersTests
{
    // ── GetUrl ──────────────────────────────────────────────────────────────

    [Fact]
    public void GetUrl_ReturnsPageUrl()
    {
        var pageMock = new Mock<IPage>();
        pageMock.Setup(p => p.Url).Returns("https://example.com/page");

        var url = WebHelpers.GetUrl(pageMock.Object);

        url.Should().Be("https://example.com/page");
    }

    [Fact]
    public void GetUrl_EmptyUrl_ReturnsEmpty()
    {
        var pageMock = new Mock<IPage>();
        pageMock.Setup(p => p.Url).Returns("");

        var url = WebHelpers.GetUrl(pageMock.Object);

        url.Should().BeEmpty();
    }

    // ── WaitAndClickAnyAsync ────────────────────────────────────────────────

    [Fact]
    public async Task WaitAndClickAnyAsync_EmptySelectors_ThrowsTimeoutException()
    {
        var pageMock = new Mock<IPage>();

        var act = () => WebHelpers.WaitAndClickAnyAsync(pageMock.Object, [], 1);
        await act.Should().ThrowAsync<TimeoutException>();
    }

    // ── SelectByIndexAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SelectByIndexAsync_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        var pageMock = new Mock<IPage>();
        var locatorMock = new Mock<ILocator>();
        pageMock.Setup(p => p.Locator(It.IsAny<string>(), null)).Returns(locatorMock.Object);
        locatorMock.Setup(l => l.AllAsync()).ReturnsAsync(new List<ILocator> { new Mock<ILocator>().Object });

        var act = () => WebHelpers.SelectByIndexAsync(pageMock.Object, "#select", -1, 10);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ── Static method signatures ────────────────────────────────────────────

    [Fact]
    public void WebHelpers_IsStaticClass()
    {
        typeof(WebHelpers).IsAbstract.Should().BeTrue();
        typeof(WebHelpers).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void WebHelpers_HasClickAsyncMethod()
    {
        var method = typeof(WebHelpers).GetMethod("ClickAsync");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void WebHelpers_HasFillAsyncMethod()
    {
        var method = typeof(WebHelpers).GetMethod("FillAsync");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void WebHelpers_HasWaitAndClickAnyAsyncMethod()
    {
        var method = typeof(WebHelpers).GetMethod("WaitAndClickAnyAsync");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void WebHelpers_HasSelectOptionAsyncMethod()
    {
        var method = typeof(WebHelpers).GetMethod("SelectOptionAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void WebHelpers_HasSelectByTextAsyncMethod()
    {
        var method = typeof(WebHelpers).GetMethod("SelectByTextAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void WebHelpers_HasSelectByIndexAsyncMethod()
    {
        var method = typeof(WebHelpers).GetMethod("SelectByIndexAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void WebHelpers_HasTypeIntoAsyncMethod()
    {
        var method = typeof(WebHelpers).GetMethod("TypeIntoAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void WebHelpers_HasElementExistsAsyncMethod()
    {
        var method = typeof(WebHelpers).GetMethod("ElementExistsAsync");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<bool>));
    }

    [Fact]
    public void WebHelpers_HasWaitForUrlContainsAsyncMethod()
    {
        var method = typeof(WebHelpers).GetMethod("WaitForUrlContainsAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void WebHelpers_HasGetUrlMethod()
    {
        var method = typeof(WebHelpers).GetMethod("GetUrl");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }
}
