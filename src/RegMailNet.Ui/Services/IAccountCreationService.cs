namespace RegMailNet.Ui.Services;

public interface IAccountCreationService
{
    Task<AccountCreatedResult> CreateOutlookAsync(bool useProxy);
    Task<AccountCreatedResult> CreateGmailAsync(bool useProxy);
    Task<AccountCreatedResult> CreateYahooAsync(bool useProxy);
}

public sealed class AccountCreatedResult
{
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
    public string Provider { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public bool Success { get; set; }
}
