using Microsoft.EntityFrameworkCore;
using RegMailNet.Api.Models;

namespace RegMailNet.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AccountHistoryEntry> AccountHistory => Set<AccountHistoryEntry>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountHistoryEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(64);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Browser).HasMaxLength(64);
            entity.Property(e => e.DefaultSmsService).HasMaxLength(64);
        });
    }
}
