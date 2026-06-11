using FluentAssertions;
using RegMailNet.SmsServices;
using Xunit;

namespace RegMailNet.Tests.SmsServices;

public class ISmsServiceTests
{
    [Fact]
    public void PhoneResult_Constructor_SetsProperties()
    {
        var result = new PhoneResult("1234567890", "order123");

        result.PhoneNumber.Should().Be("1234567890");
        result.OrderId.Should().Be("order123");
    }

    [Fact]
    public void PhoneResult_WithoutOrderId_OrderIdIsNull()
    {
        var result = new PhoneResult("1234567890");

        result.PhoneNumber.Should().Be("1234567890");
        result.OrderId.Should().BeNull();
    }

    [Fact]
    public void PhoneResult_RecordEquality()
    {
        var r1 = new PhoneResult("123", "order1");
        var r2 = new PhoneResult("123", "order1");

        r1.Should().Be(r2);
    }

    [Fact]
    public void SmsServiceApiException_WithMessage_SetsMessage()
    {
        var ex = new SmsServiceApiException("api error");

        ex.Message.Should().Be("api error");
    }

    [Fact]
    public void SmsServiceApiException_WithInner_SetsInnerException()
    {
        var inner = new Exception("inner");
        var ex = new SmsServiceApiException("outer", inner);

        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void SmsServiceApiException_InheritsException()
    {
        var ex = new SmsServiceApiException("test");
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void ISmsService_HasGetPhoneAsync()
    {
        var method = typeof(ISmsService).GetMethod("GetPhoneAsync");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<PhoneResult>));
    }

    [Fact]
    public void ISmsService_HasGetCodeAsync()
    {
        var method = typeof(ISmsService).GetMethod("GetCodeAsync");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<string>));
    }
}
