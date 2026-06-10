using Microsoft.Playwright;

namespace RegMailNet.EmailProviders;

public interface IEmailProvider
{
    string ProviderName { get; }

    Task<AccountCreationResult> CreateAccountAsync(
        IPage page,
        string username,
        string password,
        string firstName,
        string lastName,
        string month,
        string day,
        string year,
        CancellationToken cancellationToken = default);
}
