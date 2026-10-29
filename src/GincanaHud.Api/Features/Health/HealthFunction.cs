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
		return report.Status == HealthStatus.Healthy
			? Results.Text("Healthy")
			: Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
	}
}
