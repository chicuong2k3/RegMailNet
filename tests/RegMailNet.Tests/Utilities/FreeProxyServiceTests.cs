using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using RegMailNet.Utilities;
using Xunit;

namespace RegMailNet.Tests.Utilities;

public class FreeProxyServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly FreeProxyService _service;

    private const string SampleProxyHtml = @"
<html><body>
<table id='list'>
<thead><tr><th>IP</th><th>Port</th><th>Code</th><th class='hm'>Country</th><th>Anonymity</th><th class='hm'>Google</th><th>Https</th><th class='hx'>Last Checked</th><th class='hm'>Operator</th></tr></thead>
<tbody>
<tr><td>1.2.3.4</td><td>8080</td><td>US</td><td class='hm'>United States</td><td>elite proxy</td><td class='hm'>no</td><td>yes</td><td class='hx'>1 minute ago</td><td class='hm'>-</td></tr>
<tr><td>5.6.7.8</td><td>3128</td><td>DE</td><td class='hm'>Germany</td><td>anonymous</td><td class='hm'>no</td><td>no</td><td class='hx'>5 minutes ago</td><td class='hm'>-</td></tr>
</tbody>
</table></body></html>";

    public FreeProxyServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _service = new FreeProxyService(_httpClient, NullLogger<FreeProxyService>.Instance);
    }

    public void Dispose() => _httpClient.Dispose();

    private void SetupHttpResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
    }

    [Fact]
    public async Task GetProxyAsync_ValidHtml_ReturnsProxy()
    {
        SetupHttpResponse(SampleProxyHtml);

        var proxy = await _service.GetProxyAsync();

        proxy.Should().NotBeNullOrEmpty();
        proxy.Should().StartWith("http://");
        proxy.Should().MatchRegex(@"http://\d+\.\d+\.\d+\.\d+:\d+");
    }

    [Fact]
    public async Task GetProxyAsync_WithCountryFilter_ReturnsMatchingProxy()
    {
        SetupHttpResponse(SampleProxyHtml);

        var proxy = await _service.GetProxyAsync(new[] { "US" });

        proxy.Should().NotBeNullOrEmpty();
        proxy.Should().Contain("1.2.3.4");
    }

    [Fact]
    public async Task GetProxyAsync_WithNonMatchingCountry_FallsBackToUnfiltered()
    {
        SetupHttpResponse(SampleProxyHtml);

        var proxy = await _service.GetProxyAsync(new[] { "ZZ" });

        proxy.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProxyAsync_EmptyHtml_ReturnsNull()
    {
        SetupHttpResponse("<html><body>no proxies here</body></html>");

        var proxy = await _service.GetProxyAsync();

        proxy.Should().BeNull();
    }

    [Fact]
    public async Task GetProxyAsync_HttpError_ReturnsNull()
    {
        SetupHttpResponse("error", HttpStatusCode.InternalServerError);

        var proxy = await _service.GetProxyAsync();

        proxy.Should().BeNull();
    }

    [Fact]
    public async Task GetProxyAsync_FallsBackToSslProxies()
    {
        var callCount = 0;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                // First call (free-proxy-list.net) returns empty, second (sslproxies.org) returns valid
                var html = callCount == 1
                    ? "<html><body>no table</body></html>"
                    : SampleProxyHtml;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(html)
                };
            });

        var proxy = await _service.GetProxyAsync();

        proxy.Should().NotBeNullOrEmpty();
        callCount.Should().Be(2);
    }
}
