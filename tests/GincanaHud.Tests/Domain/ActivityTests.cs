using GincanaHud.Api.Domain.Activities;
using GincanaHud.Shared;

namespace GincanaHud.Tests.Domain;

public class ActivityTests
{
	private static readonly DateTimeOffset Starts = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset Ends = Starts.AddHours(4);

	private static Activity NewActivity(bool isActive = true) =>
		Activity.Create(Guid.NewGuid(), "Gincana", "", Starts, Ends, isActive: isActive).Value;

	[Fact]
	public void Create_rejects_an_end_that_is_not_after_the_start()
	{
		var result = Activity.Create(Guid.NewGuid(), "Gincana", "", Starts, Starts);

		Assert.True(result.IsError);
		Assert.Equal("Activity.Window", result.FirstError.Code);
	}

	[Fact]
	public void Create_rejects_an_empty_title()
	{
		var result = Activity.Create(Guid.NewGuid(), "  ", "", Starts, Ends);

		Assert.True(result.IsError);
		Assert.Equal(ErrorType.Validation, result.FirstError.Type);
	}

	[Fact]
	public void Create_stores_the_window_in_utc()
	{
		// The panel sends local time; Npgsql only accepts offset 0 (fase 12, paso 3b).
		var localStart = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.FromHours(2));

		var activity = Activity.Create(Guid.NewGuid(), "Gincana", "", localStart, localStart.AddHours(1)).Value;

		Assert.Equal(TimeSpan.Zero, activity.StartsAt.Offset);
		Assert.Equal(TimeSpan.Zero, activity.EndsAt.Offset);
		Assert.Equal(localStart, activity.StartsAt);
	}

	[Fact]
	public void Update_stores_the_window_in_utc()
	{
		var activity = NewActivity();
		var localStart = Starts.ToOffset(TimeSpan.FromHours(1));

		var result = activity.Update("Gincana", "", true, localStart, localStart.AddHours(2), ActivityRouteMode.Free);

		Assert.False(result.IsError);
		Assert.Equal(TimeSpan.Zero, activity.StartsAt.Offset);
		Assert.Equal(ActivityRouteMode.Free, activity.RouteMode);
	}

	[Fact]
	public void Create_generates_a_join_code_when_none_is_given()
	{
		var activity = NewActivity();

		Assert.Equal(6, activity.JoinCode.Length);
		Assert.All(activity.JoinCode, c => Assert.True(char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c)));
	}

	[Fact]
	public void Create_normalizes_the_given_join_code()
	{
		var activity = Activity.Create(Guid.NewGuid(), "Gincana", "", Starts, Ends, joinCode: " ab-c1 2 ").Value;

		Assert.Equal("ROTO-A-PROPOSITO", activity.JoinCode);
	}

	[Theory]
	[InlineData(-1, false)]
	[InlineData(0, true)]
	[InlineData(2, true)]
	[InlineData(4, true)]
	[InlineData(5, false)]
	public void Play_is_open_only_inside_the_window(int hoursFromStart, bool expected)
	{
		Assert.Equal(expected, NewActivity().IsOpenForPlay(Starts.AddHours(hoursFromStart)));
	}

	[Fact]
	public void Players_can_join_before_the_start_but_not_after_the_end()
	{
		var activity = NewActivity();

		Assert.True(activity.IsOpenForJoin(Starts.AddHours(-12)));
		Assert.False(activity.IsOpenForJoin(Ends.AddMinutes(1)));
	}

	[Fact]
	public void An_inactive_activity_is_closed_for_join_and_play()
	{
		var activity = NewActivity(isActive: false);

		Assert.False(activity.IsOpenForJoin(Starts));
		Assert.False(activity.IsOpenForPlay(Starts.AddHours(1)));
	}

	[Fact]
	public void RegisterParticipant_rejects_a_closed_activity()
	{
		var result = NewActivity().RegisterParticipant(Guid.NewGuid(), Ends.AddMinutes(1));

		Assert.Equal("Activity.Closed", result.FirstError.Code);
	}

	[Fact]
	public void RegisterParticipant_rejects_the_same_player_twice()
	{
		var activity = NewActivity();
		var userId = Guid.NewGuid();
		activity.RegisterParticipant(userId, Starts);

		var second = activity.RegisterParticipant(userId, Starts);

		Assert.Equal(ErrorType.Conflict, second.FirstError.Type);
		Assert.Single(activity.Participants);
	}

	[Fact]
	public void AssignPoi_rejects_an_order_below_one()
	{
		Assert.Equal("ActivityPoi.Order", NewActivity().AssignPoi(Guid.NewGuid(), 0).FirstError.Code);
	}

	[Fact]
	public void AssignPoi_rejects_the_same_poi_twice()
	{
		var activity = NewActivity();
		var poiId = Guid.NewGuid();
		activity.AssignPoi(poiId, 1);

		Assert.Equal("ActivityPoi.Duplicate", activity.AssignPoi(poiId, 2).FirstError.Code);
	}

	[Fact]
	public void AssignPoi_rejects_an_order_already_taken()
	{
		var activity = NewActivity();
		activity.AssignPoi(Guid.NewGuid(), 1);

		Assert.Equal("ActivityPoi.OrderTaken", activity.AssignPoi(Guid.NewGuid(), 1).FirstError.Code);
	}
}
