using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using RegMailNet.SmsServices;
using Xunit;

namespace RegMailNet.Tests.SmsServices;

public class SmsPoolServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly SmsPoolService _service;

    public SmsPoolServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _service = new SmsPoolService("395", "test_token", "1", _httpClient, NullLogger<SmsPoolService>.Instance);
    }

    public void Dispose() => _httpClient.Dispose();

    private void SetupHttpResponse(JsonElement json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json.GetRawText())
        };
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task GetPhoneAsync_ReturnsPhoneAndOrderId()
    {
        var json = JsonSerializer.Deserialize<JsonElement>(@"{
            ""success"": true,
            ""number"": ""1234567890"",
            ""phonenumber"": ""234567890"",
            ""order_id"": ""order123""
        }");
        SetupHttpResponse(json);

        var result = await _service.GetPhoneAsync();

        result.PhoneNumber.Should().Be("234567890");
        result.OrderId.Should().Be("order123");
    }

    [Fact]
    public async Task GetPhoneAsync_WithPrefix_ReturnsFullNumber()
    {
        var json = JsonSerializer.Deserialize<JsonElement>(@"{
            ""success"": true,
            ""number"": ""1234567890"",
            ""phonenumber"": ""234567890"",
            ""order_id"": ""order123""
        }");
        SetupHttpResponse(json);

        var result = await _service.GetPhoneAsync(sendPrefix: true);

        result.PhoneNumber.Should().Be("1234567890");
        result.OrderId.Should().Be("order123");
    }

    [Fact]
    public async Task GetCodeAsync_ReturnsCode()
    {
        var json = JsonSerializer.Deserialize<JsonElement>(@"{
            ""success"": true,
            ""sms"": ""12345""
        }");
        SetupHttpResponse(json);

        var code = await _service.GetCodeAsync("order123");

        code.Should().Be("12345");
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
                    ? @"{""success"": true, ""status"": 3}"
                    : @"{""success"": true, ""status"": 3, ""sms"": ""67890""}";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };
            });

        var code = await _service.GetCodeAsync("order123");

        code.Should().Be("67890");
        callCount.Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public void GetPhoneAsync_ErrorResponse_ThrowsApiException()
    {
        var json = JsonSerializer.Deserialize<JsonElement>(@"{
            ""success"": false,
            ""message"": ""Error message""
        }");
        SetupHttpResponse(json);

        var act = () => _service.GetPhoneAsync();
        act.Should().ThrowAsync<SmsServiceApiException>()
            .WithMessage("*Error message*");
    }
}
