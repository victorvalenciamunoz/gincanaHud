using ErrorOr;

using GincanaHud.Api.Common.Messaging;

using GincanaHud.Api.Data;

using GincanaHud.Shared;

using Microsoft.EntityFrameworkCore;



namespace GincanaHud.Api.Features.Activities.GetRanking;



public sealed class GetRankingHandler(AppDbContext db)

	: IRequestHandler<GetRankingQuery, ErrorOr<IReadOnlyList<RankingEntryDto>>>

{

	public async Task<ErrorOr<IReadOnlyList<RankingEntryDto>>> Handle(

		GetRankingQuery request,

		CancellationToken cancellationToken)

	{

		var activity = await db.Activities.AsNoTracking()

			.Include(a => a.Pois)

			.FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

		if (activity is null)

			return Error.NotFound(code: "Activity.NotFound", description: "Actividad no encontrada.");



		var poiTotal = activity.Pois.Count;

		var lastPoiId = activity.Pois

			.OrderByDescending(ap => ap.Order)

			.Select(ap => (Guid?)ap.PoiId)

			.FirstOrDefault();



		var captures = await db.Captures.AsNoTracking()

			.Where(c => c.ActivityId == request.ActivityId)

			.Select(c => new { c.UserId, c.PoiId, c.PointsAwarded, c.CapturedAt })

			.ToListAsync(cancellationToken);



		var rows = captures

			.GroupBy(c => c.UserId)

			.Select(g =>

			{

				var capturedIds = g.Select(x => x.PoiId).ToHashSet();

				var lastCaptureAt = (DateTimeOffset?)g.Max(x => x.CapturedAt);



				// Completar = todos los POIs capturados.

				// Secuencial: coincide con llegar al último Order si el cliente respeta la ruta.

				// Libre: el instante es el de la captura que cierra el set (max CapturedAt).

				DateTimeOffset? finishedAt = null;

				if (poiTotal > 0 && capturedIds.Count >= poiTotal)

				{

					if (activity.RouteMode == ActivityRouteMode.Sequential && lastPoiId is Guid lastId)

					{

						var finish = g.FirstOrDefault(x => x.PoiId == lastId);

						finishedAt = finish?.CapturedAt ?? lastCaptureAt;

					}

					else

					{

						finishedAt = lastCaptureAt;

					}

				}



				return new

				{

					UserId = g.Key,

					TotalPoints = g.Sum(x => x.PointsAwarded),

					CaptureCount = capturedIds.Count,

					LastCaptureAt = lastCaptureAt,

					FinishedAt = finishedAt

				};

			})

			.OrderBy(r => r.FinishedAt is null)

			.ThenBy(r => r.FinishedAt)

			.ThenByDescending(r => r.CaptureCount)

			.ThenBy(r => r.LastCaptureAt)

			.ToList();



		var userIds = rows.Select(r => r.UserId).ToList();

		var users = await db.Users.AsNoTracking()

			.Where(u => userIds.Contains(u.Id))

			.ToDictionaryAsync(u => u.Id, cancellationToken);



		return rows.Select(r =>

		{

			users.TryGetValue(r.UserId, out var user);

			return new RankingEntryDto(

				r.UserId,

				user?.DisplayName ?? "?",

				user?.ContactEmail,

				user?.ContactPhone,

				r.TotalPoints,

				r.CaptureCount,

				r.LastCaptureAt,

				r.FinishedAt);

		}).ToList();

	}

}

