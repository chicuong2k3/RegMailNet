using FluentAssertions;
using RegMailNet.EmailProviders;
using Xunit;

namespace RegMailNet.Tests.EmailProviders;

public class AccountCreationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new AccountCreationException("test error");

        ex.Message.Should().Be("test error");
    }

    [Fact]
    public void Constructor_WithMessageAndInner_SetsBoth()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new AccountCreationException("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void InheritsFromException()
    {
        var ex = new AccountCreationException("test");
        ex.Should().BeAssignableTo<Exception>();
    }
}
