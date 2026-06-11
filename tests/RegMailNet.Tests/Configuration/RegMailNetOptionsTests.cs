using FluentAssertions;
using RegMailNet.Configuration;
using Xunit;

namespace RegMailNet.Tests.Configuration;

public class RegMailNetOptionsTests
{
    [Fact]
    public void SectionName_ReturnsRegMailNet()
    {
        RegMailNetOptions.SectionName.Should().Be("RegMailNet");
    }

    [Fact]
    public void DefaultValues_AreEmpty()
    {
        var options = new RegMailNetOptions();

        options.CaptchaServicesSupported.Should().BeEmpty();
        options.DefaultCaptchaService.Should().BeEmpty();
        options.SmsServicesSupported.Should().BeEmpty();
        options.DefaultSmsService.Should().BeEmpty();
        options.SupportedSolversByEmail.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new RegMailNetOptions
        {
            CaptchaServicesSupported = ["capsolver"],
            DefaultCaptchaService = "capsolver",
            SmsServicesSupported = ["smspool"],
            DefaultSmsService = "smspool",
            SupportedSolversByEmail =
            [
                new CaptchaSolverMapping
                {
                    EmailService = "outlook",
                    Solvers = ["capsolver"]
                }
            ]
        };

        options.CaptchaServicesSupported.Should().HaveCount(1);
        options.DefaultCaptchaService.Should().Be("capsolver");
        options.SupportedSolversByEmail.Should().HaveCount(1);
        options.SupportedSolversByEmail[0].EmailService.Should().Be("outlook");
    }

    [Fact]
    public void CaptchaSolverMapping_DefaultValues()
    {
        var mapping = new CaptchaSolverMapping();

        mapping.EmailService.Should().BeEmpty();
        mapping.Solvers.Should().BeEmpty();
    }
}
