using FluentAssertions;
using Microsoft.Playwright;
using Moq;
using RegMailNet.Browser;
using Xunit;

namespace RegMailNet.Tests.Browser;

public class BrowserPageTests
{
    [Fact]
    public void Constructor_SetsPageAndContext()
    {
        var pageMock = new Mock<IPage>();
        var contextMock = new Mock<IBrowserContext>();

        var browserPage = new BrowserPage(pageMock.Object, contextMock.Object);

        browserPage.Page.Should().BeSameAs(pageMock.Object);
        browserPage.Context.Should().BeSameAs(contextMock.Object);
    }

    [Fact]
    public void Constructor_WithBrowser_SetsAllProperties()
    {
        var pageMock = new Mock<IPage>();
        var contextMock = new Mock<IBrowserContext>();
        var browserMock = new Mock<IAsyncDisposable>();

        var browserPage = new BrowserPage(pageMock.Object, contextMock.Object, browserMock.Object);

        browserPage.Page.Should().BeSameAs(pageMock.Object);
        browserPage.Context.Should().BeSameAs(contextMock.Object);
    }

    [Fact]
    public void ImplementsIAsyncDisposable()
    {
        var pageMock = new Mock<IPage>();
        var contextMock = new Mock<IBrowserContext>();

        var browserPage = new BrowserPage(pageMock.Object, contextMock.Object);
        browserPage.Should().BeAssignableTo<IAsyncDisposable>();
    }

    [Fact]
    public void BrowserPage_IsSealed()
    {
        typeof(BrowserPage).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullBrowser_DoesNotThrow()
    {
        var pageMock = new Mock<IPage>();
        var contextMock = new Mock<IBrowserContext>();

        var act = () => new BrowserPage(pageMock.Object, contextMock.Object, null);
        act.Should().NotThrow();
    }
}
