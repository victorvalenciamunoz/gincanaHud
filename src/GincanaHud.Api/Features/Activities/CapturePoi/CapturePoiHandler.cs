using ErrorOr;

using GincanaHud.Api.Common.Messaging;

using GincanaHud.Api.Data;

using GincanaHud.Api.Domain.Captures;

using GincanaHud.Shared;

using Microsoft.EntityFrameworkCore;



namespace GincanaHud.Api.Features.Activities.CapturePoi;



public sealed class CapturePoiHandler(AppDbContext db)

	: IRequestHandler<CapturePoiCommand, ErrorOr<CaptureResponse>>

{

	public async Task<ErrorOr<CaptureResponse>> Handle(

		CapturePoiCommand request,

		CancellationToken cancellationToken)

	{

		var body = request.Request;

		var now = DateTimeOffset.UtcNow;



		// Cola offline: respetar el instante de captura del cliente (ranking / ventana de juego).

		DateTimeOffset effectiveTime = now;

		if (body.CapturedAt is { } clientAt)

		{

			if (clientAt > now.AddMinutes(2))

			{

				return Error.Validation(

					code: "Capture.FutureTimestamp",

					description: "La hora de captura no puede ser futura.");

			}



			if (clientAt < now.AddHours(-36))

			{

				return Error.Validation(

					code: "Capture.StaleTimestamp",

					description: "La captura offline es demasiado antigua.");

			}



			effectiveTime = clientAt;

		}



		var activity = await db.Activities

			.FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

		if (activity is null)

			return Error.NotFound(code: "Activity.NotFound", description: "Actividad no encontrada.");



		if (!activity.IsOpenForPlay(effectiveTime))

		{

			return Error.Validation(

				code: "Activity.NotPlayable",

				description: "La actividad no está en ventana de juego (aún no empieza o ya caducó).");

		}



		var isParticipant = await db.ActivityParticipants.AnyAsync(

			p => p.ActivityId == request.ActivityId && p.UserId == body.UserId,

			cancellationToken);

		if (!isParticipant)

		{

			return Error.Validation(

				code: "Activity.NotJoined",

				description: "Debes unirte a la actividad antes de capturar.");

		}



		var link = await db.ActivityPois

			.Include(ap => ap.Poi)

			.FirstOrDefaultAsync(

				ap => ap.ActivityId == request.ActivityId && ap.PoiId == body.PoiId,

				cancellationToken);



		if (link is null)

		{

			return Error.NotFound(

				code: "ActivityPoi.NotFound",

				description: "POI no pertenece a la actividad.");

		}



		var already = await db.Captures.AsNoTracking()

			.FirstOrDefaultAsync(

				c => c.UserId == body.UserId && c.ActivityId == request.ActivityId && c.PoiId == body.PoiId,

				cancellationToken);

		if (already is not null)

		{

			return new CaptureResponse(

				true,

				already.DistanceMeters,

				link.Poi.Clue,

				already.PointsAwarded,

				already.CapturedAt,

				"Ya capturado anteriormente.");

		}



		var distance = GeoMath.DistanceMeters(

			body.Latitude, body.Longitude,

			link.Poi.Latitude, link.Poi.Longitude);



		if (distance > link.Poi.RadiusMeters)

		{

			return new CaptureResponse(

				false,

				Math.Round(distance, 1),

				null,

				0,

				null,

				$"Fuera de rango ({Math.Round(distance, 1)} m). Acércate a {link.Poi.RadiusMeters} m.");

		}



		var points = link.PointsOverride ?? link.Poi.DefaultPoints;

		var capture = Capture.Record(

			body.UserId,

			request.ActivityId,

			body.PoiId,

			Math.Round(distance, 1),

			points,

			body.CapturedAt);



		db.Captures.Add(capture);

		await db.SaveChangesAsync(cancellationToken);



		return new CaptureResponse(

			true,

			capture.DistanceMeters,

			link.Poi.Clue,

			capture.PointsAwarded,

			capture.CapturedAt,

			"POI capturado.");

	}

}

