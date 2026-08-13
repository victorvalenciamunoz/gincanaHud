using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.AdminUsers.ListAdminUsers;

public sealed record ListAdminUsersQuery : IRequest<ErrorOr<IReadOnlyList<AdminUserDto>>>;
