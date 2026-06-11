using FluentAssertions;
using RegMailNet.EmailProviders;
using Xunit;

namespace RegMailNet.Tests.EmailProviders;

public class AccountCreationResultTests
{
    [Fact]
    public void Constructor_SetsEmailAndPassword()
    {
        var result = new AccountCreationResult("test@outlook.com", "P@ss123");

        result.Email.Should().Be("test@outlook.com");
        result.Password.Should().Be("P@ss123");
    }

    [Fact]
    public void Record_Equality_Works()
    {
        var r1 = new AccountCreationResult("a@b.com", "pass");
        var r2 = new AccountCreationResult("a@b.com", "pass");

        r1.Should().Be(r2);
        (r1 == r2).Should().BeTrue();
    }

    [Fact]
    public void Record_Inequality_DifferentEmail()
    {
        var r1 = new AccountCreationResult("a@b.com", "pass");
        var r2 = new AccountCreationResult("x@b.com", "pass");

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void Record_Deconstruction_Works()
    {
        var result = new AccountCreationResult("test@mail.com", "secret");

        var (email, password) = result;

        email.Should().Be("test@mail.com");
        password.Should().Be("secret");
    }
}
