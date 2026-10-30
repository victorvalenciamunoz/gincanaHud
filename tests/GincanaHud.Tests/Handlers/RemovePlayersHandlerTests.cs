using GincanaHud.Api.Features.Activities.RemovePlayers;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Tests.Handlers;

[Collection(PostgresCollection.Name)]
public class RemovePlayersHandlerTests(PostgresFixture postgres)
{
	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private sealed record Scenario(SeededActivity Activity, Guid Ana, Guid Bea);

	// Two players with one capture each.
	private async Task<Scenario> SeedAsync()
	{
		await using var db = postgres.CreateDbContext();
		var activity = await TestData.ActivityAsync(db, poiCount: 2);
		var ana = await TestData.PlayerAsync(db, activity.Id, email: "ana@example.com");
		var bea = await TestData.PlayerAsync(db, activity.Id);
		var at = DateTimeOffset.UtcNow.AddMinutes(-5);
		await TestData.CaptureAsync(db, activity.Id, ana, activity.Pois[0], at);
		await TestData.CaptureAsync(db, activity.Id, bea, activity.Pois[0], at);
		return new Scenario(activity, ana, bea);
	}

	private async Task<ErrorOr<int>> SendAsync(RemovePlayersCommand command)
	{
		await using var db = postgres.CreateDbContext();
		return await new RemovePlayersHandler(db).Handle(command, Ct);
	}

	private async Task<(bool User, int Participations, int Captures)> LeftOfAsync(Guid userId)
	{
		await using var db = postgres.CreateDbContext();
		return (
			await db.Users.AnyAsync(u => u.Id == userId, Ct),
			await db.ActivityParticipants.CountAsync(p => p.UserId == userId, Ct),
			await db.Captures.CountAsync(c => c.UserId == userId, Ct));
	}

	[Fact]
	public async Task Removing_a_player_takes_their_captures_participation_and_contact()
	{
		var scenario = await SeedAsync();

		var result = await SendAsync(new RemovePlayersCommand(
			scenario.Activity.Id, scenario.Ana, scenario.Activity.OrganizationId));

		Assert.Equal(1, result.Value);
		Assert.Equal((false, 0, 0), await LeftOfAsync(scenario.Ana));
		Assert.Equal((true, 1, 1), await LeftOfAsync(scenario.Bea));
	}

	[Fact]
	public async Task Removing_all_only_touches_that_activity()
	{
		var scenario = await SeedAsync();
		var other = await SeedAsync();

		var result = await SendAsync(new RemovePlayersCommand(
			scenario.Activity.Id, OrganizationScope: scenario.Activity.OrganizationId));

		Assert.Equal(2, result.Value);
		Assert.Equal((false, 0, 0), await LeftOfAsync(scenario.Ana));
		Assert.Equal((false, 0, 0), await LeftOfAsync(scenario.Bea));
		Assert.Equal((true, 1, 1), await LeftOfAsync(other.Ana));
	}

	[Fact]
	public async Task Another_organization_removes_nothing()
	{
		var scenario = await SeedAsync();
		var another = Guid.NewGuid();

		var one = await SendAsync(new RemovePlayersCommand(scenario.Activity.Id, scenario.Ana, another));
		var all = await SendAsync(new RemovePlayersCommand(scenario.Activity.Id, OrganizationScope: another));

		Assert.Equal(ErrorType.NotFound, one.FirstError.Type);
		Assert.Equal(ErrorType.NotFound, all.FirstError.Type);
		Assert.Equal((true, 1, 1), await LeftOfAsync(scenario.Ana));
		Assert.Equal((true, 1, 1), await LeftOfAsync(scenario.Bea));
	}

	[Fact]
	public async Task The_superadmin_removes_from_any_organization()
	{
		var scenario = await SeedAsync();

		var result = await SendAsync(new RemovePlayersCommand(scenario.Activity.Id, scenario.Ana));

		Assert.Equal(1, result.Value);
		Assert.Equal((false, 0, 0), await LeftOfAsync(scenario.Ana));
	}

	[Fact]
	public async Task A_player_of_another_activity_is_not_found()
	{
		var scenario = await SeedAsync();
		var other = await SeedAsync();

		var result = await SendAsync(new RemovePlayersCommand(
			scenario.Activity.Id, other.Ana, scenario.Activity.OrganizationId));

		Assert.Equal("Player.NotFound", result.FirstError.Code);
		Assert.Equal((true, 1, 1), await LeftOfAsync(other.Ana));
	}

	[Fact]
	public async Task A_player_joined_before_D25_keeps_their_other_activity()
	{
		var scenario = await SeedAsync();
		var other = await SeedAsync();
		await using (var db = postgres.CreateDbContext())
		{
			var activity = await db.Activities.Include(a => a.Participants).SingleAsync(a => a.Id == other.Activity.Id, Ct);
			activity.RegisterParticipant(scenario.Ana, activity.StartsAt);
			await db.SaveChangesAsync(Ct);
		}

		await SendAsync(new RemovePlayersCommand(scenario.Activity.Id, scenario.Ana));

		Assert.Equal((true, 1, 0), await LeftOfAsync(scenario.Ana));
	}
}
