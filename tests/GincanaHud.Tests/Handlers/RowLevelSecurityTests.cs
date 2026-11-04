using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Tests.Handlers;

/// <summary>Supabase's Data API must not reach the app's tables (D29).</summary>
[Collection(PostgresCollection.Name)]
public class RowLevelSecurityTests(PostgresFixture postgres)
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task Every_table_has_row_level_security()
	{
		await using var db = postgres.CreateDbContext();
		var withoutRls = await db.Database.SqlQueryRaw<string>(
			"""
			SELECT c.relname AS "Value" FROM pg_class c
			JOIN pg_namespace n ON n.oid = c.relnamespace
			WHERE n.nspname = 'public' AND c.relkind = 'r' AND NOT c.relrowsecurity
			""").ToListAsync(Ct);

		Assert.Empty(withoutRls);
	}

	[Fact]
	public async Task A_role_like_supabase_anon_reads_no_players()
	{
		await using var db = postgres.CreateDbContext();
		var activity = await TestData.ActivityAsync(db, poiCount: 1);
		await TestData.PlayerAsync(db, activity.Id, email: "ana@example.com");

		// Supabase grants anon everything in public; RLS without policies is what stops it.
		await using var transaction = await db.Database.BeginTransactionAsync(Ct);
		await db.Database.ExecuteSqlRawAsync(
			"""
			CREATE ROLE test_anon;
			GRANT SELECT ON "Users" TO test_anon;
			SET LOCAL ROLE test_anon;
			""", Ct);
		var seen = await db.Database.SqlQueryRaw<int>("""SELECT count(*)::int AS "Value" FROM "Users" """)
			.SingleAsync(Ct);
		await transaction.RollbackAsync(Ct);

		Assert.Equal(0, seen);
	}
}
