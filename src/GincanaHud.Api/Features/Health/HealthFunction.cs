using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GincanaHud.Api.Features.Health;

// Under /api because Static Web Apps answers /health from the static site (D20).
public sealed class HealthFunction(HealthCheckService health)
{
	[Function("Health")]
	public async Task<IResult> Get(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
		CancellationToken ct)
	{
		var report = await health.CheckHealthAsync(ct);
		if (report.Status == HealthStatus.Healthy)
			return Results.Text("Healthy");

		var reasons = report.Entries
			.Where(e => e.Value.Status != HealthStatus.Healthy)
			.Select(e => e.Value.Description ?? e.Key);
		return Results.Text(
			$"Unhealthy: {string.Join("; ", reasons)}", statusCode: StatusCodes.Status503ServiceUnavailable);
	}
}
