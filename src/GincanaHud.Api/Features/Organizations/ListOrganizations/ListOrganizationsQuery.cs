using ErrorOr;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Organizations.ListOrganizations;

public sealed record ListOrganizationsQuery : IRequest<ErrorOr<IReadOnlyList<OrganizationDto>>>;
