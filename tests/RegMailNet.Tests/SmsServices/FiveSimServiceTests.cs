using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using RegMailNet.SmsServices;
using Xunit;

namespace RegMailNet.Tests.SmsServices;

public class FiveSimServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly FiveSimService _service;

    public FiveSimServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _service = new FiveSimService("outlook", "api_key", "usa", _httpClient, NullLogger<FiveSimService>.Instance);
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
    public async Task GetPhoneAsync_ReturnsPhoneAndOrderId()
    {
        SetupHttpResponse(@"{""phone"": ""+1234567890"", ""id"": 12345}");

        var result = await _service.GetPhoneAsync();

        result.PhoneNumber.Should().Be("234567890");
        result.OrderId.Should().Be("12345");
    }

    [Fact]
    public async Task GetPhoneAsync_WithPrefix_ReturnsFullNumber()
    {
        SetupHttpResponse(@"{""phone"": ""+1234567890"", ""id"": 12345}");

        var result = await _service.GetPhoneAsync(sendPrefix: true);

        result.PhoneNumber.Should().Be("1234567890");
        result.OrderId.Should().Be("12345");
    }

    [Fact]
    public async Task GetCodeAsync_ReturnsCode()
    {
        SetupHttpResponse(@"{""sms"": [{""code"": ""67890""}], ""status"": ""RECEIVED""}");

        var code = await _service.GetCodeAsync("12345");

        code.Should().Be("67890");
    }

    [Fact]
    public async Task GetCodeAsync_RetriesUntilCodeReceived()
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
                var json = callCount < 3
                    ? @"{""sms"": [], ""status"": ""WAITING""}"
                    : @"{""sms"": [{""code"": ""11111""}], ""status"": ""RECEIVED""}";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };
            });

        var code = await _service.GetCodeAsync("12345");

        code.Should().Be("11111");
        callCount.Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public void GetCodeAsync_CancelledStatus_ThrowsApiException()
    {
        SetupHttpResponse(@"{""sms"": [], ""status"": ""CANCELED""}");

        var act = () => _service.GetCodeAsync("12345");
        act.Should().ThrowAsync<SmsServiceApiException>()
            .WithMessage("*CANCELED*");
    }

    [Fact]
    public void GetCodeAsync_TimeoutStatus_ThrowsApiException()
    {
        SetupHttpResponse(@"{""sms"": [], ""status"": ""TIMEOUT""}");

        var act = () => _service.GetCodeAsync("12345");
        act.Should().ThrowAsync<SmsServiceApiException>()
            .WithMessage("*TIMEOUT*");
    }

    [Fact]
    public void RequestAsync_NoFreePhones_ThrowsApiException()
    {
        SetupHttpResponse("no free phones");

        var act = () => _service.GetPhoneAsync();
        act.Should().ThrowAsync<SmsServiceApiException>()
            .WithMessage("*no free phones*");
    }

    [Fact]
    public void RequestAsync_NotEnoughBalance_ThrowsApiException()
    {
        SetupHttpResponse("not enough user balance");

        var act = () => _service.GetPhoneAsync();
        act.Should().ThrowAsync<SmsServiceApiException>()
            .WithMessage("*Not enough balance*");
    }

    [Fact]
    public void RequestAsync_HttpError_ThrowsHttpRequestException()
    {
        SetupHttpResponse("error", HttpStatusCode.InternalServerError);

        var act = () => _service.GetPhoneAsync();
        act.Should().ThrowAsync<HttpRequestException>();
    }
}
