using GincanaHud.Api.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GincanaHud.Tests.Handlers;

/// <summary>
/// One real Postgres per test run of the collection, with the app's migrations applied (D23).
/// Tests don't clean up: each one seeds its own activity, POIs and players with fresh ids.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
	private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
	private DbContextOptions<AppDbContext> _options = null!;

	public async ValueTask InitializeAsync()
	{
		await _container.StartAsync();
		_options = new DbContextOptionsBuilder<AppDbContext>()
			.UseNpgsql(_container.GetConnectionString())
			.Options;

		await using var db = CreateDbContext();
		await db.Database.MigrateAsync();
	}

	/// <summary>A fresh context, like each request gets in the Api.</summary>
	public AppDbContext CreateDbContext() => new(_options);

	public ValueTask DisposeAsync() => _container.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
	public const string Name = "Postgres";
}
