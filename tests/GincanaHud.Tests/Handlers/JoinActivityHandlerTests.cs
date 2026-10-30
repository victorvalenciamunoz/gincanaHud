using GincanaHud.Api.Features.Activities.JoinActivity;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Tests.Handlers;

[Collection(PostgresCollection.Name)]
public class JoinActivityHandlerTests(PostgresFixture postgres)
{
	private async Task<ErrorOr<JoinActivityResponse>> JoinAsync(string joinCode, string displayName)
	{
		await using var db = postgres.CreateDbContext();
		return await new JoinActivityHandler(db).Handle(
			new JoinActivityCommand(new JoinActivityRequest(joinCode, displayName, null, null)),
			TestContext.Current.CancellationToken);
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
	public async Task Joining_twice_with_the_same_name_keeps_one_participant()
	{
		SeededActivity activity;
		await using (var db = postgres.CreateDbContext())
			activity = await TestData.ActivityAsync(db);
		var name = TestData.UniqueName();

		var first = await JoinAsync(activity.JoinCode, name);
		var second = await JoinAsync(activity.JoinCode, name);

		Assert.False(second.IsError);
		Assert.Equal(first.Value.User.Id, second.Value.User.Id);
		Assert.Equal(1, await ParticipantCountAsync(activity.Id));
	}
}
