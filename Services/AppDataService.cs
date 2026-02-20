using CascadeIDE.Data;
using CascadeIDE.Models;
using Microsoft.EntityFrameworkCore;
using OutWit.Database.EntityFramework.Extensions;

namespace CascadeIDE.Services;

/// <summary>Хранение данных приложения (недавние решения, кэш, настройки отправки и т.д.) в WitDatabase через EF Core.</summary>
public sealed class AppDataService : IDisposable
{
    private readonly AppDbContext _context;

    public AppDataService()
    {
        var path = Path.Combine(SettingsService.GetSettingsDirectory(), "app.witdb");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseWitDb($"Data Source={path}")
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Put(string key, string value)
    {
        var item = _context.AppData.Find(key);
        if (item is null)
            _context.AppData.Add(new AppDataItem { Key = key, Value = value });
        else
            item.Value = value;
        _context.SaveChanges();
    }

    public string? Get(string key)
    {
        return _context.AppData.Find(key)?.Value;
    }

    public void Delete(string key)
    {
        var item = _context.AppData.Find(key);
        if (item is not null)
        {
            _context.AppData.Remove(item);
            _context.SaveChanges();
        }
    }

    public void Flush() => _context.SaveChanges();

    public void Dispose() => _context.Dispose();
}
