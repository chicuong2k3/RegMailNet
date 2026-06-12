using Microsoft.EntityFrameworkCore;
using RegMailNet.Api.Data;
using RegMailNet.Api.Models;

namespace RegMailNet.Api.Services;

public class EfSettingsService
{
    private readonly AppDbContext _db;

    public EfSettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AppSettings> LoadAsync()
    {
        return await _db.Settings.FirstOrDefaultAsync() ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var existing = await _db.Settings.FirstOrDefaultAsync();
        if (existing is null)
        {
            _db.Settings.Add(settings);
        }
        else
        {
            existing.Browser = settings.Browser;
            existing.DefaultSmsService = settings.DefaultSmsService;
            existing.CapsolverKey = settings.CapsolverKey;
            existing.NopechaKey = settings.NopechaKey;
            existing.SmsPoolToken = settings.SmsPoolToken;
            existing.FiveSimToken = settings.FiveSimToken;
            existing.GetsmsCodeUser = settings.GetsmsCodeUser;
            existing.GetsmsCodeToken = settings.GetsmsCodeToken;
            existing.Proxies = settings.Proxies;
        }

        await _db.SaveChangesAsync();
    }
}
