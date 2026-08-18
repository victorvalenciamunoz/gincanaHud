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
			.Include(a => a.Participants)
			.FirstOrDefaultAsync(a => a.JoinCode == codeResult.Value.Value, cancellationToken);

		if (activity is null)
			return Error.NotFound(code: "Activity.NotFound", description: "No hay actividad con ese código.");

		var now = DateTimeOffset.UtcNow;
		if (!activity.IsOpenForJoin(now))
		{
			return Error.Validation(
				code: "Activity.Closed",
				description: "Esta actividad está caducada o inactiva).");
		}

		var nameResult = DisplayName.Create(body.DisplayName);
		if (nameResult.IsError)
			return nameResult.Errors;

		var name = nameResult.Value.Value;
		var user = await db.Users.FirstOrDefaultAsync(u => u.DisplayName == name, cancellationToken);
		if (user is null)
		{
			var created = User.Register(name, body.ContactEmail, body.ContactPhone);
			if (created.IsError)
				return created.Errors;
			user = created.Value;
			db.Users.Add(user);
		}
		else
		{
			user.UpdateContact(body.ContactEmail, body.ContactPhone);
		}

		if (!activity.Participants.Any(p => p.UserId == user.Id))
		{
			var joined = activity.RegisterParticipant(user.Id, now);
			if (joined.IsError)
				return joined.Errors;
		}

		await db.SaveChangesAsync(cancellationToken);

		return new JoinActivityResponse(
			ActivityDtoMapping.ToUserDto(user),
			ActivityDtoMapping.ToSummary(activity, activity.Organization.Name));
	}
}
