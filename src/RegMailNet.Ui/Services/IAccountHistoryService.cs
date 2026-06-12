using RegMailNet.Ui.Models;

namespace RegMailNet.Ui.Services;

public interface IAccountHistoryService
{
    Task<IReadOnlyList<AccountHistoryEntry>> GetHistoryAsync();
    Task AddAsync(AccountHistoryEntry entry);
}
