using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(string Name)
	: IRequest<ErrorOr<OrganizationDto>>;
