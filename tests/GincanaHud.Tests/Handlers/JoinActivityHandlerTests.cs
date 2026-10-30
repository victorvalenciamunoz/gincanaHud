using GincanaHud.Api.Features.Activities.JoinActivity;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Tests.Handlers;

[Collection(PostgresCollection.Name)]
public class JoinActivityHandlerTests(PostgresFixture postgres)
{
	private async Task<ErrorOr<JoinActivityResponse>> JoinAsync(
		string joinCode, string displayName, Guid? keptId = null, string? email = null)
	{
		await using var db = postgres.CreateDbContext();
		return await new JoinActivityHandler(db).Handle(
			new JoinActivityCommand(new JoinActivityRequest(joinCode, displayName, email, null, keptId)),
			TestContext.Current.CancellationToken);
	}

	private async Task<SeededActivity> SeedAsync(int poiCount = 0)
	{
		await using var db = postgres.CreateDbContext();
		return await TestData.ActivityAsync(db, poiCount);
	}

	private async Task<int> ParticipantCountAsync(Guid activityId)
	{
		await using var db = postgres.CreateDbContext();
		return await db.ActivityParticipants.CountAsync(p => p.ActivityId == activityId, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task A_valid_code_joins_the_player()
	{
		SeededActivity activity;
		await using (var db = postgres.CreateDbContext())
			activity = await TestData.ActivityAsync(db);
		var name = TestData.UniqueName();

		// The player may type the code in lower case.
		var result = await JoinAsync(activity.JoinCode.ToLowerInvariant(), name);

		Assert.False(result.IsError);
		Assert.Equal(name, result.Value.User.DisplayName);
		Assert.Equal(activity.Id, result.Value.Activity.Id);
		Assert.Equal(1, await ParticipantCountAsync(activity.Id));
	}

	[Fact]
	public async Task An_unknown_code_is_not_found()
	{
		// Join codes never contain 0, 1, I or O.
		var result = await JoinAsync("ZZZ0001", TestData.UniqueName());

		Assert.True(result.IsError);
		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
		Assert.Equal("Activity.NotFound", result.FirstError.Code);
	}

	[Fact]
	public async Task An_expired_activity_cannot_be_joined()
	{
		SeededActivity activity;
		await using (var db = postgres.CreateDbContext())
			activity = await TestData.ActivityAsync(db, startsIn: TimeSpan.FromHours(-5), duration: TimeSpan.FromHours(4));

		var result = await JoinAsync(activity.JoinCode, TestData.UniqueName());

		Assert.True(result.IsError);
		Assert.Equal("Activity.Closed", result.FirstError.Code);
		Assert.Equal(0, await ParticipantCountAsync(activity.Id));
	}

	[Fact]
	public async Task An_inactive_activity_cannot_be_joined()
	{
		SeededActivity activity;
		await using (var db = postgres.CreateDbContext())
			activity = await TestData.ActivityAsync(db, isActive: false);

		var result = await JoinAsync(activity.JoinCode, TestData.UniqueName());

		Assert.True(result.IsError);
		Assert.Equal("Activity.Closed", result.FirstError.Code);
		Assert.Equal(0, await ParticipantCountAsync(activity.Id));
	}

	[Fact]
	public async Task The_answer_does_not_carry_the_contact()
	{
		var activity = await SeedAsync();

		var result = await JoinAsync(activity.JoinCode, TestData.UniqueName(), email: "ana@example.com");

		Assert.False(result.IsError);
		Assert.Null(result.Value.User.ContactEmail);
		Assert.Null(result.Value.User.ContactPhone);
	}

	[Fact]
	public async Task A_name_already_in_the_activity_is_rejected()
	{
		var activity = await SeedAsync();
		var name = TestData.UniqueName();
		await JoinAsync(activity.JoinCode, name);

		// Someone else typing the same name, in other case, gets neither the player nor a duplicate.
		var second = await JoinAsync(activity.JoinCode, name.ToUpperInvariant());

		Assert.Equal(ErrorType.Conflict, second.FirstError.Type);
		Assert.Equal("Player.NameTaken", second.FirstError.Code);
		Assert.Equal(1, await ParticipantCountAsync(activity.Id));
	}

	[Fact]
	public async Task Rejoining_with_the_kept_id_keeps_the_progress()
	{
		var activity = await SeedAsync(poiCount: 1);
		var name = TestData.UniqueName();
		var first = (await JoinAsync(activity.JoinCode, name)).Value.User.Id;
		await using (var db = postgres.CreateDbContext())
			await TestData.CaptureAsync(db, activity.Id, first, activity.Pois[0], DateTimeOffset.UtcNow.AddMinutes(-5));

		var again = await JoinAsync(activity.JoinCode, name, keptId: first);

		Assert.Equal(first, again.Value.User.Id);
		Assert.Equal(1, await ParticipantCountAsync(activity.Id));
		await using var check = postgres.CreateDbContext();
		Assert.Equal(1, await check.Captures.CountAsync(c => c.UserId == first, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task The_same_name_in_another_activity_is_another_player()
	{
		var name = TestData.UniqueName();
		var first = (await JoinAsync((await SeedAsync()).JoinCode, name)).Value.User.Id;

		// Even sending the id kept from the other activity.
		var other = await JoinAsync((await SeedAsync()).JoinCode, name, keptId: first);

		Assert.False(other.IsError);
		Assert.NotEqual(first, other.Value.User.Id);
	}
}
