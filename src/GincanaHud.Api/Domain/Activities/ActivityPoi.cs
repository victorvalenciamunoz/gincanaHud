using ErrorOr;

namespace GincanaHud.Api.Domain.Activities;

public sealed class ActivityPoi
{
	private ActivityPoi() { }

	public Guid ActivityId { get; private set; }
	public Activity Activity { get; private set; } = null!;
	public Guid PoiId { get; private set; }
	public Pois.Poi Poi { get; private set; } = null!;
	public int Order { get; private set; }
	public int? PointsOverride { get; private set; }

	internal static ActivityPoi Create(Guid activityId, Guid poiId, int order, int? pointsOverride)
		=> new()
		{
			ActivityId = activityId,
			PoiId = poiId,
			Order = order,
			PointsOverride = pointsOverride
		};

	public ErrorOr<Success> ChangeOrder(int newOrder, IEnumerable<ActivityPoi> siblings)
	{
		if (newOrder <= 0)
			return Error.Validation(code: "ActivityPoi.Order", description: "Order debe ser mayor que 0.");

		if (siblings.Any(p => p.PoiId != PoiId && p.Order == newOrder))
			return Error.Conflict(code: "ActivityPoi.OrderTaken", description: $"Ya hay un POI con orden {newOrder}.");

		Order = newOrder;
		return Result.Success;
	}

	public void ClearPointsOverride() => PointsOverride = null;
}
