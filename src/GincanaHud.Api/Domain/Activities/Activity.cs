using ErrorOr;
using GincanaHud.Api.Domain.ValueObjects;
using GincanaHud.Shared;

namespace GincanaHud.Api.Domain.Activities;

public sealed class Activity
{
	private Activity() { }

	public Guid Id { get; private set; }
	public Guid OrganizationId { get; private set; }
	public Organizations.Organization Organization { get; private set; } = null!;
	public string Title { get; private set; } = "";
	public string Description { get; private set; } = "";
	public bool IsActive { get; private set; } = true;
	public string JoinCode { get; private set; } = "";
	public ActivityRouteMode RouteMode { get; private set; } = ActivityRouteMode.Sequential;
	public DateTimeOffset StartsAt { get; private set; }
	public DateTimeOffset EndsAt { get; private set; }
	public List<ActivityPoi> Pois { get; private set; } = [];
	public List<ActivityParticipant> Participants { get; private set; } = [];
	public List<Captures.Capture> Captures { get; private set; } = [];

	public static ErrorOr<Activity> Create(
		Guid organizationId,
		string? title,
		string? description,
		DateTimeOffset startsAt,
		DateTimeOffset endsAt,
		string? joinCode = null,
		Guid? id = null,
		bool isActive = true,
		ActivityRouteMode routeMode = ActivityRouteMode.Sequential)
	{
		var activityTitle = ActivityTitle.Create(title);
		if (activityTitle.IsError)
			return activityTitle.Errors;

		if (endsAt <= startsAt)
		{
			return Error.Validation(
				code: "Activity.Window",
				description: "EndsAt debe ser posterior a StartsAt.");
		}

		if (!Enum.IsDefined(routeMode))
		{
			return Error.Validation(
				code: "Activity.RouteMode",
				description: "Modo de ruta no válido (Sequential | Free).");
		}

		ValueObjects.JoinCode codeValue;
		if (string.IsNullOrWhiteSpace(joinCode))
		{
			codeValue = ValueObjects.JoinCode.Generate();
		}
		else
		{
			var parsed = ValueObjects.JoinCode.Create(joinCode);
			if (parsed.IsError)
				return parsed.Errors;
			codeValue = parsed.Value;
		}

		return new Activity
		{
			Id = id ?? Guid.NewGuid(),
			OrganizationId = organizationId,
			Title = activityTitle.Value.Value,
			Description = description?.Trim() ?? "",
			IsActive = isActive,
			JoinCode = codeValue.Value,
			RouteMode = routeMode,
			StartsAt = startsAt,
			EndsAt = endsAt
		};
	}

	public ErrorOr<Success> Update(
		string? title,
		string? description,
		bool isActive,
		DateTimeOffset startsAt,
		DateTimeOffset endsAt,
		ActivityRouteMode routeMode)
	{
		var activityTitle = ActivityTitle.Create(title);
		if (activityTitle.IsError)
			return activityTitle.Errors;

		if (endsAt <= startsAt)
		{
			return Error.Validation(
				code: "Activity.Window",
				description: "EndsAt debe ser posterior a StartsAt.");
		}

		if (!Enum.IsDefined(routeMode))
		{
			return Error.Validation(
				code: "Activity.RouteMode",
				description: "Modo de ruta no válido (Sequential | Free).");
		}

		Title = activityTitle.Value.Value;
		Description = description?.Trim() ?? "";
		IsActive = isActive;
		StartsAt = startsAt;
		EndsAt = endsAt;
		RouteMode = routeMode;
		return Result.Success;
	}

	public bool IsOpenForJoin(DateTimeOffset utcNow)
		=> IsActive && utcNow <= EndsAt;

	public bool IsOpenForPlay(DateTimeOffset utcNow)
		=> IsActive && utcNow >= StartsAt && utcNow <= EndsAt;

	public ErrorOr<ActivityParticipant> RegisterParticipant(Guid userId, DateTimeOffset utcNow)
	{
		if (!IsOpenForJoin(utcNow))
		{
			return Error.Validation(
				code: "Activity.Closed",
				description: "Esta actividad ya no admite uniones (caducada o inactiva).");
		}

		if (Participants.Any(p => p.UserId == userId))
		{
			return Error.Conflict(
				code: "Activity.AlreadyJoined",
				description: "El usuario ya está unido a esta actividad.");
		}

		var participant = ActivityParticipant.Create(Id, userId);
		Participants.Add(participant);
		return participant;
	}

	public ErrorOr<ActivityPoi> AssignPoi(Guid poiId, int order, int? pointsOverride = null)
	{
		if (order <= 0)
			return Error.Validation(code: "ActivityPoi.Order", description: "Order debe ser mayor que 0.");

		if (Pois.Any(p => p.PoiId == poiId))
			return Error.Conflict(code: "ActivityPoi.Duplicate", description: "El POI ya está en la actividad.");

		if (Pois.Any(p => p.Order == order))
			return Error.Conflict(code: "ActivityPoi.OrderTaken", description: $"Ya hay un POI con orden {order}.");

		var link = ActivityPoi.Create(Id, poiId, order, pointsOverride);
		Pois.Add(link);
		return link;
	}
}
