using GincanaHud.Api.Common.Http;
using GincanaHud.Api.Common.Messaging;
using GincanaHud.Api.Features.Organizations.CreateOrganization;
using GincanaHud.Api.Features.Organizations.ListOrganizations;
using GincanaHud.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace GincanaHud.Api.Features.Organizations;

[Authorize(Policy = JwtAuthExtensions.SuperAdminPolicy)]
public sealed class OrganizationsFunctions(ISender sender)
{
	[Function("ListOrganizations")]
	public async Task<IResult> List(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organizations")] HttpRequest req,
		CancellationToken ct)
	{
		var result = await sender.Send(new ListOrganizationsQuery(), ct);
		return result.ToHttpResult();
	}

	[Function("CreateOrganization")]
	public Task<IResult> Create(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "organizations")] HttpRequest req,
		CancellationToken ct) =>
		req.WithJsonBodyAsync<CreateOrganizationRequest>(async body =>
		{
			var result = await sender.Send(new CreateOrganizationCommand(body.Name), ct);
			return result.ToHttpResult(o => Results.Created($"/api/organizations/{o.Id}", o));
		}, ct);
}
