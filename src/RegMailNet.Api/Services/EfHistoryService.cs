using Microsoft.EntityFrameworkCore;
using RegMailNet.Api.Data;
using RegMailNet.Api.Models;

namespace RegMailNet.Api.Services;

public class EfHistoryService
{
    private readonly AppDbContext _db;

    public EfHistoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AccountHistoryEntry>> LoadAsync()
    {
        return await _db.AccountHistory
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(AccountHistoryEntry entry)
    {
        _db.AccountHistory.Add(entry);
        await _db.SaveChangesAsync();
    }
}
