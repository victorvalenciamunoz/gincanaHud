using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace GincanaHud.Api.Data;

/// <summary>
/// Remembers why seeding failed at startup, so /api/health can say it without taking the host down (D22).
/// Only the exception type and the Postgres error code: the message can carry the database user.
/// </summary>
public sealed class StartupHealthCheck : IHealthCheck
{
	private string? _failure;

	public void Fail(Exception ex) =>
		_failure = ex is PostgresException pg ? $"{ex.GetType().Name} {pg.SqlState}" : ex.GetType().Name;

	public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
		Task.FromResult(_failure is null
			? HealthCheckResult.Healthy()
			: HealthCheckResult.Unhealthy($"arranque: {_failure}"));
}
