using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sgo.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Sgo.IntegrationTests;

/// <summary>Hosts the API against a throwaway Postgres container, migrated and seeded on startup.</summary>
public sealed class SgoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@sgo.test";
    public const string AdminPassword = "Admin12345Test";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:SeedOnStartup", "true");
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _ = Server; // start the host: applies migrations and seed
    }

    /// <summary>A fresh DI scope with its own DbContext.</summary>
    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();

    public static SgoDbContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<SgoDbContext>();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<SgoApiFactory>
{
    public const string Name = "api";
}
