using FluentAssertions;
using RegMailNet.Utilities;
using Xunit;

namespace RegMailNet.Tests.Utilities;

public class DataGeneratorTests
{
    private readonly DataGenerator _generator = new();

    [Fact]
    public void GenerateMissingInfo_AllFieldsEmpty_GeneratesAll()
    {
        var result = _generator.GenerateMissingInfo("", "", "", "", "", "");

        result.Username.Should().NotBeNullOrEmpty();
        result.Password.Should().NotBeNullOrEmpty();
        result.Password.Length.Should().BeInRange(8, 12);
        result.FirstName.Should().NotBeNullOrEmpty();
        result.LastName.Should().NotBeNullOrEmpty();
        result.Country.Should().NotBeNullOrEmpty();
        result.Birthdate.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateMissingInfo_AllFieldsProvided_ReturnsUnchanged()
    {
        var result = _generator.GenerateMissingInfo("testuser", "P@ssw0rd!", "John", "Doe", "USA", "5-15-1990");

        result.Username.Should().Be("testuser");
        result.Password.Should().Be("P@ssw0rd!");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Country.Should().Be("USA");
        result.Birthdate.Should().Be("5-15-1990");
    }

    [Fact]
    public void GetBirthdate_ValidFormat_ReturnsComponents()
    {
        var result = _generator.GetBirthdate("5-15-1990");

        result.Month.Should().Be("5");
        result.Day.Should().Be("15");
        result.Year.Should().Be("1990");
    }

    [Theory]
    [InlineData("1", "January")]
    [InlineData("2", "February")]
    [InlineData("3", "March")]
    [InlineData("6", "June")]
    [InlineData("12", "December")]
    [InlineData("01", "January")]
    [InlineData("06", "June")]
    public void GetMonthByNumber_ValidNumber_ReturnsName(string number, string expected)
    {
        var result = _generator.GetMonthByNumber(number);
        result.Should().Be(expected);
    }

    [Fact]
    public void GenerateMissingInfo_PartialFields_GeneratesOnlyMissing()
    {
        var result = _generator.GenerateMissingInfo("", "MyP@ss123", "", "Smith", "", "3-20-1985");

        result.Username.Should().NotBeNullOrEmpty();
        result.Password.Should().Be("MyP@ss123");
        result.FirstName.Should().NotBeNullOrEmpty();
        result.LastName.Should().Be("Smith");
        result.Country.Should().NotBeNullOrEmpty();
        result.Birthdate.Should().Be("3-20-1985");
    }

    [Fact]
    public void GenerateUsername_FollowsExpectedFormat()
    {
        var result = _generator.GenerateMissingInfo("", "", "Alice", "Johnson", "", "7-4-1995");

        result.Username.Should().StartWith("a");
        result.Username.Should().Contain("johnson");
        result.Username.Should().Contain("1995");
    }
}
