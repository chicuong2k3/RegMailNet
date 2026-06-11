using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using RegMailNet.SmsServices;
using Xunit;

namespace RegMailNet.Tests.SmsServices;

public class GetsmsCodeServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly GetsmsCodeService _service;

    public GetsmsCodeServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _service = new GetsmsCodeService("123", "testuser", "testtoken", "us", _httpClient, NullLogger<GetsmsCodeService>.Instance);
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
    public async Task GetPhoneAsync_ReturnsPhoneNumber()
    {
        SetupHttpResponse("234567");

        var result = await _service.GetPhoneAsync();

        result.PhoneNumber.Should().Be("234567");
    }

    [Fact]
    public async Task GetPhoneAsync_WithPrefix_ReturnsFullNumber()
    {
        SetupHttpResponse("1234567");

        var result = await _service.GetPhoneAsync(sendPrefix: true);

        result.PhoneNumber.Should().Be("1234567");
    }

    [Fact]
    public async Task GetCodeAsync_ReturnsCode()
    {
        SetupHttpResponse("Success|12345");

        var code = await _service.GetCodeAsync("234567");

        code.Should().Be("12345");
    }

    [Fact]
    public async Task GetCodeAsync_RetriesUntilCodeFound()
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
                var content = callCount < 3 ? "Success|" : "Success|67890";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                };
            });

        var code = await _service.GetCodeAsync("234567");

        code.Should().Be("67890");
        callCount.Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public void RequestAsync_ErrorResponse_ThrowsApiException()
    {
        SetupHttpResponse("Message|Error occurred");

        var act = () => _service.GetPhoneAsync();
        act.Should().ThrowAsync<SmsServiceApiException>();
    }

    [Fact]
    public void RequestAsync_HttpError_ThrowsHttpRequestException()
    {
        SetupHttpResponse("error", HttpStatusCode.InternalServerError);

        var act = () => _service.GetPhoneAsync();
        act.Should().ThrowAsync<HttpRequestException>();
    }
}
