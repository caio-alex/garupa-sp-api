using Garupa.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Garupa.Application.Interfaces;
using Garupa.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Garupa.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly string? postgresConnection;
    private readonly string schema = "garupa_tests_" + Guid.NewGuid().ToString("N");
    private bool disposed;
    public bool UsaPostgres => postgresConnection is not null;
    public TestClock Clock { get; } = new();
    public ConcurrentQueue<string> Logs { get; } = new();

    public ApiFactory()
    {
        var configured = Environment.GetEnvironmentVariable("GARUPA_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(configured))
        {
            connection.Open();
            return;
        }

        var settings = new NpgsqlConnectionStringBuilder(configured);
        if (settings.Host is not ("127.0.0.1" or "localhost") || settings.Database != "garupa_sp")
            throw new InvalidOperationException("Os testes exigem o banco local garupa_sp.");
        settings.SearchPath = schema;
        settings.Pooling = false;
        settings.IncludeErrorDetail = false;
        postgresConnection = settings.ConnectionString;
        using var database = new NpgsqlConnection(postgresConnection);
        database.Open();
        using var command = database.CreateCommand();
        command.CommandText = $"CREATE SCHEMA \"{schema}\"";
        command.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:GarupaDb"] = "Host=unused;Database=tests" }));
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILoggerProvider>(new LogCapturado(Logs));
            services.RemoveAll<DbContextOptions<GarupaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GarupaDbContext>>();
            services.AddDbContext<GarupaDbContext>(options =>
            {
                if (postgresConnection is null)
                    options.UseSqlite(connection);
                else
                    options.UseNpgsql(postgresConnection,
                        postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema));
            });
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.RemoveAll<ICriptografiaDados>();
            services.AddSingleton<ICriptografiaDados>(new CriptografiaDados(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()));
        });
    }

    public HttpClient CriarCliente()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<GarupaDbContext>().Database;
        if (UsaPostgres) database.Migrate();
        else database.EnsureCreated();
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed) return;
        disposed = true;
        base.Dispose(disposing);
        if (!disposing) return;
        connection.Dispose();
        if (postgresConnection is not null)
        {
            using var database = new NpgsqlConnection(postgresConnection);
            database.Open();
            using var command = database.CreateCommand();
            // O identificador contém apenas o prefixo fixo e um GUID gerado por esta instância.
            command.CommandText = $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE";
            command.ExecuteNonQuery();
        }
    }
}

internal sealed class LogCapturado(ConcurrentQueue<string> mensagens) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new Logger(mensagens);
    public void Dispose() { }
    private sealed class Logger(ConcurrentQueue<string> mensagens) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => mensagens.Enqueue(formatter(state, exception));
    }
}

public class TestClock : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Agora;
}
