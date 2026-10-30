using GincanaHud.Api.Features.Activities.AddActivityPoi;
using GincanaHud.Api.Features.Activities.CreateActivity;
using GincanaHud.Api.Features.Activities.GetLiveProgress;
using GincanaHud.Api.Features.Activities.RemoveActivityPoi;
using GincanaHud.Api.Features.Activities.UpdateActivity;
using GincanaHud.Api.Features.Activities.UpdateActivityPoi;
using GincanaHud.Api.Features.Pois.CreatePoi;
using GincanaHud.Shared;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Tests.Handlers;

/// <summary>An organization admin only reaches its own organization's activities and POIs (D24).</summary>
[Collection(PostgresCollection.Name)]
public class OrganizationScopeTests(PostgresFixture postgres)
{
	private static readonly Guid? AnotherOrganization = Guid.NewGuid();

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	private async Task<SeededActivity> SeedAsync()
	{
		await using var db = postgres.CreateDbContext();
		return await TestData.ActivityAsync(db, poiCount: 1);
	}

	private static UpdateActivityCommand Rename(SeededActivity activity, Guid? scope) =>
		new(activity.Id, "Título nuevo", "", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2),
			OrganizationScope: scope);

	private static UpdateActivityPoiRequest PoiAt(int order) => new("POI movido", 0, 0.005, 30, "Pista", 10, order);

	[Fact]
	public async Task Another_organization_cannot_edit_the_activity()
	{
		var activity = await SeedAsync();

		ErrorOr<ActivitySummaryDto> result;
		await using (var db = postgres.CreateDbContext())
			result = await new UpdateActivityHandler(db).Handle(Rename(activity, AnotherOrganization), Ct);

		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
		await using var check = postgres.CreateDbContext();
		Assert.Equal("Gincana de prueba", (await check.Activities.SingleAsync(a => a.Id == activity.Id, Ct)).Title);
	}

	[Fact]
	public async Task The_own_organization_and_the_super_admin_can_edit_it()
	{
		var activity = await SeedAsync();

		await using var db = postgres.CreateDbContext();
		Assert.False((await new UpdateActivityHandler(db).Handle(Rename(activity, activity.OrganizationId), Ct)).IsError);
		Assert.False((await new UpdateActivityHandler(db).Handle(Rename(activity, scope: null), Ct)).IsError);
	}

	[Fact]
	public async Task Another_organization_cannot_add_a_poi()
	{
		var activity = await SeedAsync();
		var request = new CreatePoiRequest(Guid.Empty, "POI nuevo", 0, 0.004, 30, "Pista", 10);

		await using var db = postgres.CreateDbContext();
		var result = await new AddActivityPoiHandler(db).Handle(
			new AddActivityPoiCommand(activity.Id, request, AnotherOrganization), Ct);

		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
	}

	[Fact]
	public async Task Another_organization_cannot_edit_a_poi()
	{
		var activity = await SeedAsync();

		await using var db = postgres.CreateDbContext();
		var result = await new UpdateActivityPoiHandler(db).Handle(
			new UpdateActivityPoiCommand(activity.Id, activity.Pois[0].Id, PoiAt(1), AnotherOrganization), Ct);

		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
	}

	[Fact]
	public async Task Another_organization_cannot_remove_a_poi()
	{
		var activity = await SeedAsync();

		ErrorOr<Deleted> result;
		await using (var db = postgres.CreateDbContext())
			result = await new RemoveActivityPoiHandler(db).Handle(
				new RemoveActivityPoiCommand(activity.Id, activity.Pois[0].Id, AnotherOrganization), Ct);

		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
		await using var check = postgres.CreateDbContext();
		Assert.True(await check.ActivityPois.AnyAsync(ap => ap.ActivityId == activity.Id, Ct));
	}

	[Fact]
	public async Task Another_organization_cannot_follow_the_live_board()
	{
		var activity = await SeedAsync();

		await using var db = postgres.CreateDbContext();
		var result = await new GetLiveProgressHandler(db).Handle(
			new GetLiveProgressQuery(activity.Id, AnotherOrganization), Ct);

		Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
	}

	[Fact]
	public async Task An_admin_cannot_create_an_activity_for_another_organization()
	{
		var activity = await SeedAsync();

		await using var db = postgres.CreateDbContext();
		var result = await new CreateActivityHandler(db).Handle(
			new CreateActivityCommand(
				activity.OrganizationId, "Otra", "", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2),
				OrganizationScope: AnotherOrganization),
			Ct);

		Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
	}

	[Fact]
	public async Task An_admin_cannot_create_a_poi_for_another_organization()
	{
		var activity = await SeedAsync();

		await using var db = postgres.CreateDbContext();
		var result = await new CreatePoiHandler(db).Handle(
			new CreatePoiCommand(activity.OrganizationId, "POI", 0, 0.004, 30, "Pista", 10, AnotherOrganization), Ct);

		Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
	}
}
