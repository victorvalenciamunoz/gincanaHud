using GincanaHud.Api.Features.Activities.CapturePoi;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Tests.Handlers;

[Collection(PostgresCollection.Name)]
public class CapturePoiHandlerTests(PostgresFixture postgres)
{
	// About 11 m north: inside the 30 m radius. 0.01° is about 1.1 km.
	private const double Near = 0.0001;
	private const double Far = 0.01;

	private async Task<(SeededActivity Activity, Guid PlayerId)> SeedAsync(
		int poiCount = 1,
		ActivityRouteMode routeMode = ActivityRouteMode.Sequential,
		TimeSpan? startsIn = null)
	{
		await using var db = postgres.CreateDbContext();
		var activity = await TestData.ActivityAsync(db, poiCount, routeMode: routeMode, startsIn: startsIn);
		var player = await TestData.PlayerAsync(db, activity.Id);
		return (activity, player);
	}

	private async Task<ErrorOr<CaptureResponse>> CaptureAsync(
		Guid activityId,
		Guid playerId,
		SeededPoi poi,
		double northOffset = Near,
		DateTimeOffset? capturedAt = null)
	{
		await using var db = postgres.CreateDbContext();
		return await new CapturePoiHandler(db).Handle(
			new CapturePoiCommand(
				activityId,
				new CaptureRequest(playerId, poi.Id, poi.Latitude + northOffset, poi.Longitude, capturedAt)),
			TestContext.Current.CancellationToken);
	}

	private async Task<int> CaptureCountAsync(Guid activityId, Guid playerId)
	{
		await using var db = postgres.CreateDbContext();
		return await db.Captures.CountAsync(
			c => c.ActivityId == activityId && c.UserId == playerId,
			TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task From_far_away_it_is_rejected_without_clue()
	{
		var (activity, player) = await SeedAsync();

		var result = await CaptureAsync(activity.Id, player, activity.Pois[0], Far);

		Assert.False(result.IsError);
		Assert.False(result.Value.Success);
		Assert.Null(result.Value.Clue);
		Assert.Equal(0, result.Value.PointsAwarded);
		Assert.InRange(result.Value.DistanceMeters, 1000, 1200);
		Assert.Equal(0, await CaptureCountAsync(activity.Id, player));
	}

	[Fact]
	public async Task Inside_the_radius_it_gives_points_and_clue()
	{
		var (activity, player) = await SeedAsync();
		var poi = activity.Pois[0];

		var result = await CaptureAsync(activity.Id, player, poi);

		Assert.True(result.Value.Success);
		Assert.Equal(poi.Clue, result.Value.Clue);
		Assert.Equal(poi.Points, result.Value.PointsAwarded);
		Assert.InRange(result.Value.DistanceMeters, 5, TestData.PoiRadiusMeters);
		Assert.Equal(1, await CaptureCountAsync(activity.Id, player));
	}

	[Fact]
	public async Task Sequential_route_cannot_skip_a_poi()
	{
		var (activity, player) = await SeedAsync(poiCount: 3);

		var skipped = await CaptureAsync(activity.Id, player, activity.Pois[1]);

		Assert.False(skipped.Value.Success);
		Assert.Null(skipped.Value.Clue);
		Assert.Contains("POI 1", skipped.Value.Message);
		Assert.Equal(0, await CaptureCountAsync(activity.Id, player));

		Assert.True((await CaptureAsync(activity.Id, player, activity.Pois[0])).Value.Success);
		Assert.True((await CaptureAsync(activity.Id, player, activity.Pois[1])).Value.Success);
	}

	[Fact]
	public async Task Free_route_can_capture_in_any_order()
	{
		var (activity, player) = await SeedAsync(poiCount: 3, routeMode: ActivityRouteMode.Free);

		var result = await CaptureAsync(activity.Id, player, activity.Pois[2]);

		Assert.True(result.Value.Success);
	}

	[Theory]
	[InlineData(1)] // starts in an hour
	[InlineData(-5)] // lasted four hours and ended an hour ago
	public async Task Outside_the_window_it_is_rejected(int startsInHours)
	{
		var (activity, player) = await SeedAsync(startsIn: TimeSpan.FromHours(startsInHours));

		var result = await CaptureAsync(activity.Id, player, activity.Pois[0]);

		Assert.True(result.IsError);
		Assert.Equal("Activity.NotPlayable", result.FirstError.Code);
		Assert.Equal(0, await CaptureCountAsync(activity.Id, player));
	}

	[Fact]
	public async Task Capturing_again_does_not_add_points_twice()
	{
		var (activity, player) = await SeedAsync();
		var poi = activity.Pois[0];

		var first = await CaptureAsync(activity.Id, player, poi);
		var again = await CaptureAsync(activity.Id, player, poi);

		Assert.True(again.Value.Success);
		Assert.Equal(first.Value.PointsAwarded, again.Value.PointsAwarded);
		Assert.Equal(first.Value.CapturedAt!.Value, again.Value.CapturedAt!.Value, TimeSpan.FromMilliseconds(1));
		Assert.Equal(1, await CaptureCountAsync(activity.Id, player));
	}

	[Fact]
	public async Task An_offline_capture_keeps_its_time_in_utc()
	{
		var (activity, player) = await SeedAsync();
		// The phone queues local time; Npgsql rejects any offset but zero (fase 12).
		var capturedAt = DateTimeOffset.UtcNow.AddMinutes(-10).ToOffset(TimeSpan.FromHours(1));

		var result = await CaptureAsync(activity.Id, player, activity.Pois[0], capturedAt: capturedAt);

		Assert.True(result.Value.Success);
		await using var db = postgres.CreateDbContext();
		var stored = await db.Captures.SingleAsync(
			c => c.ActivityId == activity.Id && c.UserId == player,
			TestContext.Current.CancellationToken);
		Assert.Equal(TimeSpan.Zero, stored.CapturedAt.Offset);
		// Postgres keeps microseconds; .NET ticks are a tenth of that.
		Assert.Equal(capturedAt, stored.CapturedAt, TimeSpan.FromMilliseconds(1));
	}
}
