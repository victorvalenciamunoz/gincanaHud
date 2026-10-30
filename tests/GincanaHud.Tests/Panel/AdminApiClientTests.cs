using System.Net;
using System.Text;
using GincanaHud.Admin.Services;
using GincanaHud.Shared;

namespace GincanaHud.Tests.Panel;

/// <summary>
/// Each Api answer reaches the pages as ErrorOr (4xx but 401) or as an exception (401, 5xx, network), as
/// blazor.md asks. Bodies come from <c>Fixtures</c> (see its README).
/// </summary>
public class AdminApiClientTests
{
	private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> Task.FromResult(respond());
	}

	private static AdminApiClient ClientAnswering(HttpStatusCode status, string? fixture = null) =>
		ClientWith(() =>
		{
			var response = new HttpResponseMessage(status);
			if (fixture is not null)
			{
				var body = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Panel", "Fixtures", fixture));
				response.Content = new StringContent(body, Encoding.UTF8, "application/problem+json");
			}
			return response;
		});

	private static AdminApiClient ClientWith(Func<HttpResponseMessage> respond) =>
		new(new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("http://api.test/") });

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	[Fact]
	public async Task A_404_with_the_api_reason_is_a_not_found_error()
	{
		var client = ClientAnswering(HttpStatusCode.NotFound, "activity-not-found.404.json");

		var result = await client.GetActivityAsync(Guid.NewGuid(), Ct);

		Assert.True(result.IsError);
		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
		Assert.Equal("Activity.NotFound", result.FirstError.Code);
		Assert.Equal("No hay actividad con ese código.", result.FirstError.Description);
	}

	[Fact]
	public async Task A_409_is_a_conflict_with_the_api_reason()
	{
		var client = ClientAnswering(HttpStatusCode.Conflict, "username-taken.409.json");

		var result = await client.CreateOrgAdminAsync(new CreateOrgAdminRequest("ana", "contraseña", Guid.NewGuid()), Ct);

		Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
		Assert.Equal("AdminUser.UsernameTaken", result.FirstError.Code);
		Assert.Equal("Ese usuario ya existe.", result.FirstError.Description);
	}

	[Fact]
	public async Task A_400_without_body_is_a_validation_error_with_the_status()
	{
		// Recorded: a malformed body gets a bare 400.
		var client = ClientAnswering(HttpStatusCode.BadRequest);

		var result = await client.CreateOrganizationAsync("Empresa de prueba", Ct);

		Assert.Equal(ErrorType.Validation, result.FirstError.Type);
		Assert.Equal("Http.400", result.FirstError.Code);
		Assert.Contains("400", result.FirstError.Description);
	}

	[Fact]
	public async Task A_403_without_body_is_forbidden()
	{
		// The JWT middleware answers 403 without a body.
		var client = ClientAnswering(HttpStatusCode.Forbidden);

		var result = await client.GetOrganizationsAsync(Ct);

		Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
		Assert.Equal("No tienes permiso para esto.", result.FirstError.Description);
	}

	[Fact]
	public async Task A_failed_delete_is_an_error_too()
	{
		var client = ClientAnswering(HttpStatusCode.NotFound, "activity-not-found.404.json");

		var result = await client.RemovePoiAsync(Guid.NewGuid(), Guid.NewGuid(), Ct);

		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
	}

	[Fact]
	public async Task A_401_is_an_exception()
	{
		// Recorded: no token gets a bare 401. The session handler has already signed out.
		var client = ClientAnswering(HttpStatusCode.Unauthorized);

		var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetOrganizationsAsync(Ct));

		Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
	}

	[Theory]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.BadGateway)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	public async Task A_5xx_is_an_exception(HttpStatusCode status)
	{
		var client = ClientAnswering(status);

		var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetRankingAsync(Guid.NewGuid(), Ct));

		Assert.Equal(status, ex.StatusCode);
	}

	[Fact]
	public async Task A_network_failure_is_an_exception()
	{
		var client = ClientWith(() => throw new HttpRequestException("Sin conexión."));

		await Assert.ThrowsAsync<HttpRequestException>(() => client.GetActivitiesAsync(ct: Ct));
	}

	[Fact]
	public async Task A_success_reads_the_body_with_enums_as_text()
	{
		var client = ClientAnswering(HttpStatusCode.OK, "activities.200.json");

		var result = await client.GetActivitiesAsync(ct: Ct);

		var activity = Assert.Single(result.Value);
		Assert.Equal("ABC234", activity.JoinCode);
		Assert.Equal(ActivityRouteMode.Free, activity.RouteMode);
		Assert.Equal(new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), activity.StartsAt);
	}

	[Fact]
	public async Task A_success_without_data_is_an_exception()
	{
		var client = ClientWith(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") });

		await Assert.ThrowsAsync<HttpRequestException>(() => client.GetActivitiesAsync(ct: Ct));
	}

	[Fact]
	public async Task A_successful_delete_is_success()
	{
		var client = ClientAnswering(HttpStatusCode.NoContent);

		var result = await client.RemovePoiAsync(Guid.NewGuid(), Guid.NewGuid(), Ct);

		Assert.False(result.IsError);
	}
}
