using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Users.ListUsers;

public sealed record ListUsersQuery(Guid? OrganizationId = null)
	: IRequest<ErrorOr<IReadOnlyList<PlayerDto>>>;
