using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Users;
using GincanaHud.Api.Domain.ValueObjects;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Activities.JoinActivity;

public sealed class JoinActivityHandler(AppDbContext db)
	: IRequestHandler<JoinActivityCommand, ErrorOr<JoinActivityResponse>>
{
	public async Task<ErrorOr<JoinActivityResponse>> Handle(
		JoinActivityCommand request,
		CancellationToken cancellationToken)
	{
		var body = request.Request;
		var codeResult = JoinCode.Create(body.JoinCode);
		if (codeResult.IsError)
			return codeResult.Errors;

		var activity = await db.Activities
			.Include(a => a.Organization)
			.Include(a => a.Participants).ThenInclude(p => p.User)
			.FirstOrDefaultAsync(a => a.JoinCode == codeResult.Value.Value, cancellationToken);

		if (activity is null)
			return Error.NotFound(code: "Activity.NotFound", description: "No hay actividad con ese código.");

		var now = DateTimeOffset.UtcNow;
		if (!activity.IsOpenForJoin(now))
		{
			return Error.Validation(
				code: "Activity.Closed",
				description: "Esta actividad está caducada o inactiva.");
		}

		var nameResult = DisplayName.Create(body.DisplayName);
		if (nameResult.IsError)
			return nameResult.Errors;

		// A new player on every join; the phone gets its own back by sending the id it keeps (D25).
		var name = nameResult.Value.Value;
		var user = body.UserId is Guid userId
			? activity.Participants.FirstOrDefault(p => p.UserId == userId)?.User
			: null;
		if (user is not null)
		{
			user.UpdateContact(body.ContactEmail, body.ContactPhone);
		}
		else
		{
			if (activity.Participants.Any(p => string.Equals(p.User.DisplayName, name, StringComparison.OrdinalIgnoreCase)))
			{
				return Error.Conflict(
					code: "Player.NameTaken",
					description: "Ese nombre ya está en la partida: añade una inicial o un apellido.");
			}

			var created = User.Register(name, body.ContactEmail, body.ContactPhone);
			if (created.IsError)
				return created.Errors;
			user = created.Value;
			db.Users.Add(user);

			var joined = activity.RegisterParticipant(user.Id, now);
			if (joined.IsError)
				return joined.Errors;
		}

		await db.SaveChangesAsync(cancellationToken);

		// Anonymous answer: no contact (D24). The app keeps what the player typed.
		return new JoinActivityResponse(
			ActivityDtoMapping.ToUserDto(user) with { ContactEmail = null, ContactPhone = null },
			ActivityDtoMapping.ToSummary(activity, activity.Organization.Name));
	}
}
