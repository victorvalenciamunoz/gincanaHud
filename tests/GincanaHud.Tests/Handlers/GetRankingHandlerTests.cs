using GincanaHud.Api.Features.Activities.GetRanking;
using GincanaHud.Shared;

namespace GincanaHud.Tests.Handlers;

[Collection(PostgresCollection.Name)]
public class GetRankingHandlerTests(PostgresFixture postgres)
{
	private static readonly DateTimeOffset T0 = DateTimeOffset.UtcNow.AddMinutes(-50);

	private sealed record Scenario(SeededActivity Activity, Dictionary<string, Guid> Players);

	/// <summary>
	/// Three POIs in sequence; minutes after T0 for each capture:
	/// Ana 5, 10, 30 · Bea 6, 12, 20 · Carla 2, 3 · Dani 15 · Eva 8 · Fede joined, nothing captured.
	/// </summary>
	private async Task<Scenario> SeedAsync()
	{
		await using var db = postgres.CreateDbContext();
		var activity = await TestData.ActivityAsync(db, poiCount: 3);
		var captures = new Dictionary<string, int[]>
		{
			["Ana"] = [5, 10, 30],
			["Bea"] = [6, 12, 20],
			["Carla"] = [2, 3],
			["Dani"] = [15],
			["Eva"] = [8],
			["Fede"] = [],
		};

		var players = new Dictionary<string, Guid>();
		foreach (var (name, minutes) in captures)
		{
			var player = await TestData.PlayerAsync(
				db, activity.Id, TestData.UniqueName(name), $"{name.ToLowerInvariant()}@example.com");
			players[name] = player;
			for (var i = 0; i < minutes.Length; i++)
				await TestData.CaptureAsync(db, activity.Id, player, activity.Pois[i], T0.AddMinutes(minutes[i]));
		}

		return new Scenario(activity, players);
	}

	private async Task<ErrorOr<IReadOnlyList<RankingEntryDto>>> SendAsync(GetRankingQuery query)
	{
		await using var db = postgres.CreateDbContext();
		return await new GetRankingHandler(db).Handle(query, TestContext.Current.CancellationToken);
	}

	private async Task<IReadOnlyList<RankingEntryDto>> RankingAsync(Guid activityId) =>
		(await SendAsync(new GetRankingQuery(activityId))).Value;

	[Fact]
	public async Task Finishers_come_first_by_arrival_then_by_captures()
	{
		var scenario = await SeedAsync();

		var ranking = await RankingAsync(scenario.Activity.Id);

		// Bea reaches the last POI before Ana; Eva and Dani tie on captures and Eva captured earlier.
		// Fede has no captures and is not listed.
		string[] expected = ["Bea", "Ana", "Carla", "Eva", "Dani"];
		Assert.Equal(expected.Select(n => scenario.Players[n]), ranking.Select(r => r.UserId));
	}

	[Fact]
	public async Task Each_row_counts_points_captures_and_finish()
	{
		var scenario = await SeedAsync();

		var ranking = await RankingAsync(scenario.Activity.Id);

		var bea = ranking.Single(r => r.UserId == scenario.Players["Bea"]);
		Assert.Equal(10 + 20 + 30, bea.TotalPoints);
		Assert.Equal(3, bea.CaptureCount);
		Assert.Equal(T0.AddMinutes(20), bea.FinishedAt!.Value, TimeSpan.FromMilliseconds(1));

		var carla = ranking.Single(r => r.UserId == scenario.Players["Carla"]);
		Assert.Equal(10 + 20, carla.TotalPoints);
		Assert.Equal(2, carla.CaptureCount);
		Assert.Equal(T0.AddMinutes(3), carla.LastCaptureAt!.Value, TimeSpan.FromMilliseconds(1));
		Assert.Null(carla.FinishedAt);
	}

	[Fact]
	public async Task The_anonymous_ranking_has_no_contacts()
	{
		var scenario = await SeedAsync();

		var ranking = await RankingAsync(scenario.Activity.Id);

		Assert.All(ranking, r =>
		{
			Assert.Null(r.ContactEmail);
			Assert.Null(r.ContactPhone);
		});
	}

	[Fact]
	public async Task The_organization_gets_the_contacts()
	{
		var scenario = await SeedAsync();

		var ranking = (await SendAsync(new GetRankingQuery(
			scenario.Activity.Id, IncludeContacts: true, scenario.Activity.OrganizationId))).Value;

		Assert.Equal("bea@example.com", ranking.Single(r => r.UserId == scenario.Players["Bea"]).ContactEmail);
	}

	[Fact]
	public async Task Another_organization_does_not_find_the_activity()
	{
		var scenario = await SeedAsync();

		var result = await SendAsync(new GetRankingQuery(scenario.Activity.Id, IncludeContacts: true, Guid.NewGuid()));

		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
	}

	[Fact]
	public async Task An_unknown_activity_is_not_found()
	{
		var result = await SendAsync(new GetRankingQuery(Guid.NewGuid()));

		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
	}
}
