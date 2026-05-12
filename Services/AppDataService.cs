using CascadeIDE.Data;
using CascadeIDE.Models;
using Microsoft.EntityFrameworkCore;
using OutWit.Database.EntityFramework.Extensions;

namespace CascadeIDE.Services;

/// <summary>Хранение данных приложения (недавние решения, кэш, настройки отправки и т.д.) в WitDatabase через EF Core.</summary>
public sealed class AppDataService : IDisposable
{
    private AppDbContext _context;

    public AppDataService()
    {
        var path = Path.Combine(SettingsService.GetSettingsDirectory(), "app.witdb");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseWitDb($"Data Source={path}")
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        // EnsureCreated при уже существующем файле не добавляет новые таблицы; старый app.witdb без AppData давал ошибку Wit «Table … not found».
        EnsureAppDataTableReachable(path, options);
    }

    /// <summary>Если база уже была, но без таблицы AppData — пересоздаём файл (единоразово восстанавливает схему; локальное kv-хранилище).</summary>
    private void EnsureAppDataTableReachable(string witPath, DbContextOptions<AppDbContext> options)
    {
        try
        {
            // Без Take(1)/Count: Wit SQL не переваривает LIMIT из такого запроса EF.
            _ = _context.AppData.Find("__cascade_ide_schema_probe__");
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("Table", StringComparison.Ordinal)
            && ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            _context.Dispose();
            if (File.Exists(witPath))
                File.Delete(witPath);
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
            _ = _context.AppData.Find("__cascade_ide_schema_probe__");
        }
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
