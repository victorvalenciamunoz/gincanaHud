using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Data;
using GincanaHud.Api.Domain.Users;
using GincanaHud.Api.Features.Activities;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Features.Users.UpsertUser;

public sealed class UpsertUserHandler(AppDbContext db)
	: IRequestHandler<UpsertUserCommand, ErrorOr<UpsertUserResult>>
{
	public async Task<ErrorOr<UpsertUserResult>> Handle(
		UpsertUserCommand request,
		CancellationToken cancellationToken)
	{
		var nameResult = Domain.ValueObjects.DisplayName.Create(request.DisplayName);
		if (nameResult.IsError)
			return nameResult.Errors;

		var name = nameResult.Value.Value;
		var existing = await db.Users.FirstOrDefaultAsync(u => u.DisplayName == name, cancellationToken);
		if (existing is not null)
		{
			return new UpsertUserResult(ActivityDtoMapping.ToUserDto(existing), Created: false);
		}

		var created = User.Register(name, request.ContactEmail, request.ContactPhone);
		if (created.IsError)
			return created.Errors;

		db.Users.Add(created.Value);
		await db.SaveChangesAsync(cancellationToken);

		return new UpsertUserResult(ActivityDtoMapping.ToUserDto(created.Value), Created: true);
	}
}
