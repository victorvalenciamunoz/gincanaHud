using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Activities;
using GincanaHud.Api.Domain.Captures;
using GincanaHud.Api.Domain.Organizations;
using GincanaHud.Api.Domain.Pois;
using GincanaHud.Api.Domain.Users;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Tests.Handlers;

public sealed record SeededPoi(Guid Id, int Order, double Latitude, double Longitude, string Clue, int Points);

public sealed record SeededActivity(Guid Id, string JoinCode, IReadOnlyList<SeededPoi> Pois);

/// <summary>Seeds through the domain, like the panel does. Neutral coordinates around (0, 0): public repo.</summary>
public static class TestData
{
	public const double PoiRadiusMeters = 30;

	// The panel sends local time; seeding with an offset checks that Npgsql still gets UTC (fase 12, paso 3b).
	private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(1);

	public static string UniqueName(string prefix = "Jugador") => $"{prefix} {Guid.NewGuid():N}"[..(prefix.Length + 9)];

	/// <summary>
	/// An activity whose window starts <paramref name="startsIn"/> from now and lasts <paramref name="duration"/>
	/// (by default, started an hour ago and open three more hours). POI n sits about n × 111 m east of (0, 0)
	/// and gives n × 10 points.
	/// </summary>
	public static async Task<SeededActivity> ActivityAsync(
		AppDbContext db,
		int poiCount = 0,
		bool isActive = true,
		ActivityRouteMode routeMode = ActivityRouteMode.Sequential,
		TimeSpan? startsIn = null,
		TimeSpan? duration = null)
	{
		var ct = TestContext.Current.CancellationToken;
		var organization = Organization.Create(UniqueName("Empresa")).Value;
		var startsAt = DateTimeOffset.UtcNow.Add(startsIn ?? TimeSpan.FromHours(-1)).ToOffset(LocalOffset);
		var activity = Activity.Create(
			organization.Id,
			"Gincana de prueba",
			"",
			startsAt,
			startsAt.Add(duration ?? TimeSpan.FromHours(4)),
			isActive: isActive,
			routeMode: routeMode).Value;

		var pois = new List<SeededPoi>();
		for (var order = 1; order <= poiCount; order++)
		{
			var poi = Poi.Create(organization.Id, $"POI {order}", 0, order * 0.001, PoiRadiusMeters, $"Pista {order}", order * 10).Value;
			activity.AssignPoi(poi.Id, order);
			db.Pois.Add(poi);
			pois.Add(new SeededPoi(poi.Id, order, poi.Latitude, poi.Longitude, poi.Clue, poi.DefaultPoints));
		}

		db.Organizations.Add(organization);
		db.Activities.Add(activity);
		await db.SaveChangesAsync(ct);
		return new SeededActivity(activity.Id, activity.JoinCode, pois);
	}

	/// <summary>A player already joined to the activity (registered at its start, so it also works once expired).</summary>
	public static async Task<Guid> PlayerAsync(AppDbContext db, Guid activityId, string? name = null)
	{
		var ct = TestContext.Current.CancellationToken;
		var activity = await db.Activities.Include(a => a.Participants).SingleAsync(a => a.Id == activityId, ct);
		var user = User.Register(name ?? UniqueName()).Value;
		db.Users.Add(user);
		activity.RegisterParticipant(user.Id, activity.StartsAt);
		await db.SaveChangesAsync(ct);
		return user.Id;
	}

	/// <summary>A capture stored as is, without the handler's rules: for ranking scenarios.</summary>
	public static async Task CaptureAsync(AppDbContext db, Guid activityId, Guid playerId, SeededPoi poi, DateTimeOffset at)
	{
		db.Captures.Add(Capture.Record(playerId, activityId, poi.Id, 0, poi.Points, at));
		await db.SaveChangesAsync(TestContext.Current.CancellationToken);
	}
}
