namespace GincanaHud.Api.Domain.Captures;

public sealed class Capture
{
	private Capture() { }

	public Guid Id { get; private set; }
	public Guid UserId { get; private set; }
	public Users.User User { get; private set; } = null!;
	public Guid ActivityId { get; private set; }
	public Activities.Activity Activity { get; private set; } = null!;
	public Guid PoiId { get; private set; }
	public Pois.Poi Poi { get; private set; } = null!;
	public DateTimeOffset CapturedAt { get; private set; }
	public double DistanceMeters { get; private set; }
	public int PointsAwarded { get; private set; }

	public static Capture Record(
		Guid userId,
		Guid activityId,
		Guid poiId,
		double distanceMeters,
		int pointsAwarded,
		DateTimeOffset? capturedAt = null)
		=> new()
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			ActivityId = activityId,
			PoiId = poiId,
			CapturedAt = capturedAt ?? DateTimeOffset.UtcNow,
			DistanceMeters = distanceMeters,
			PointsAwarded = pointsAwarded
		};
}
