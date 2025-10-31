using AzuriteUI.Web.Services.CacheDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AzuriteUI.Web.UnitTests.Helpers;

public abstract class SqliteDbTests : IDisposable
{
    protected readonly SqliteConnection _connection;
    private bool _disposed;

    protected SqliteDbTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected CacheDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CacheDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new CacheDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    #region Dispose

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }

    #endregion
}