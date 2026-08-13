using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.AdminUsers.CreateOrgAdmin;

public sealed record CreateOrgAdminCommand(string Username, string Password, Guid OrganizationId)
	: IRequest<ErrorOr<AdminUserDto>>;
