namespace RegMailNet.Api.Models;

public class AccountHistoryEntry
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Provider { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
}
