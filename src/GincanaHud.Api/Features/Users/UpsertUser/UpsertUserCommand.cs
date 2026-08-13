using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Users.UpsertUser;

public sealed record UpsertUserCommand(
	string DisplayName,
	string? ContactEmail = null,
	string? ContactPhone = null) : IRequest<ErrorOr<UpsertUserResult>>;

public sealed record UpsertUserResult(UserDto User, bool Created);
