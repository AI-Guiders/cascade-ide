using CascadeIDE.Models;
using Microsoft.EntityFrameworkCore;

namespace CascadeIDE.Data;

/// <summary>EF Core контекст для WitDatabase-хранилища приложения (<c>app.witdb</c> в %LocalAppData%).</summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppDataItem> AppData => Set<AppDataItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppDataItem>()
            .ToTable("AppData")
            .HasKey(e => e.Key);
    }
}
