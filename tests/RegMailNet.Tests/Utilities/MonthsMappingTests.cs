using FluentAssertions;
using RegMailNet.Utilities;
using Xunit;

namespace RegMailNet.Tests.Utilities;

public class MonthsMappingTests
{
    [Theory]
    [InlineData("1", "January")]
    [InlineData("2", "February")]
    [InlineData("3", "March")]
    [InlineData("4", "April")]
    [InlineData("5", "May")]
    [InlineData("6", "June")]
    [InlineData("7", "July")]
    [InlineData("8", "August")]
    [InlineData("9", "September")]
    [InlineData("10", "October")]
    [InlineData("11", "November")]
    [InlineData("12", "December")]
    public void GetMonthName_ValidNumber_ReturnsCorrectMonth(string number, string expected)
    {
        MonthsMapping.GetMonthName(number).Should().Be(expected);
    }

    [Theory]
    [InlineData("01", "January")]
    [InlineData("06", "June")]
    public void GetMonthName_WithLeadingZero_ReturnsCorrectMonth(string number, string expected)
    {
        MonthsMapping.GetMonthName(number).Should().Be(expected);
    }

    [Fact]
    public void GetMonthName_InvalidNumber_ReturnsInvalidMessage()
    {
        MonthsMapping.GetMonthName("13").Should().Be("Invalid month number");
    }
}
