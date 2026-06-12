using RegMailNet.EmailProviders;

namespace RegMailNet.Ui.Services;

public class AccountHistoryService
{
    private readonly List<AccountHistoryEntry> _entries = new();

    public IReadOnlyList<AccountHistoryEntry> Entries => _entries.AsReadOnly();

    public void Add(AccountCreationResult result, string provider)
    {
        _entries.Insert(0, new AccountHistoryEntry
        {
            Email = result.Email,
            Password = result.Password,
            Provider = provider,
            CreatedAt = DateTime.Now,
            Status = "Success"
        });
    }

    public void AddFailure(string provider, string error)
    {
        _entries.Insert(0, new AccountHistoryEntry
        {
            Email = "-",
            Password = "-",
            Provider = provider,
            CreatedAt = DateTime.Now,
            Status = $"Failed: {error}"
        });
    }

    public void Delete(int index)
    {
        if (index >= 0 && index < _entries.Count)
            _entries.RemoveAt(index);
    }

    public void ClearAll()
    {
        _entries.Clear();
    }

    public string ExportToCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Email,Password,Provider,Created,Status");
        foreach (var entry in _entries)
        {
            sb.AppendLine($"\"{entry.Email}\",\"{entry.Password}\",\"{entry.Provider}\",\"{entry.CreatedAt:yyyy-MM-dd HH:mm}\",\"{entry.Status}\"");
        }
        return sb.ToString();
    }
}

public class AccountHistoryEntry
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Provider { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
}