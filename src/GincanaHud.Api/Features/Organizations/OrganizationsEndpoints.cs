using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Organizations.CreateOrganization;
using GincanaHud.Api.Features.Organizations.ListOrganizations;
using GincanaHud.Shared;

namespace GincanaHud.Api.Features.Organizations;

public static class OrganizationsEndpoints
{
	public static RouteGroupBuilder MapOrganizationsEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/organizations");

		group.MapGet("/", async (ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new ListOrganizationsQuery(), ct);
			return result.ToHttpResult();
		});

		group.MapPost("/", async (CreateOrganizationRequest body, ISender sender, CancellationToken ct) =>
		{
			var result = await sender.Send(new CreateOrganizationCommand(body.Name), ct);
			return result.ToHttpResult(o => Results.Created($"/api/organizations/{o.Id}", o));
		});

		return group;
	}
}
