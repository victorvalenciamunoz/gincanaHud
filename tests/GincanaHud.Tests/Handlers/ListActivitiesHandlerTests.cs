using GincanaHud.Api.Features.Activities.ListActivities;
using GincanaHud.Shared;

namespace GincanaHud.Tests.Handlers;

[Collection(PostgresCollection.Name)]
public class ListActivitiesHandlerTests(PostgresFixture postgres)
{
	private async Task<IReadOnlyList<ActivitySummaryDto>> ListAsync(Guid? organizationId)
	{
		await using var db = postgres.CreateDbContext();
		var result = await new ListActivitiesHandler(db).Handle(
			new ListActivitiesQuery(IncludeInactive: true, organizationId),
			TestContext.Current.CancellationToken);
		return result.Value;
	}

	private async Task<(SeededActivity Mine, SeededActivity Theirs)> SeedTwoOrganizationsAsync()
	{
		await using var db = postgres.CreateDbContext();
		return (await TestData.ActivityAsync(db), await TestData.ActivityAsync(db));
	}

	[Fact]
	public async Task An_organization_admin_lists_only_its_activities()
	{
		var (mine, _) = await SeedTwoOrganizationsAsync();

		var listed = await ListAsync(mine.OrganizationId);

		Assert.Equal(mine.Id, Assert.Single(listed).Id);
	}

	[Fact]
	public async Task The_super_admin_lists_every_organization()
	{
		var (mine, theirs) = await SeedTwoOrganizationsAsync();

		var listed = (await ListAsync(organizationId: null)).Select(a => a.Id).ToList();

		Assert.Contains(mine.Id, listed);
		Assert.Contains(theirs.Id, listed);
	}
}
