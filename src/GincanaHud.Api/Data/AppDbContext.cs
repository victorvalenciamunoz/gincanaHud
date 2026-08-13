using GincanaHud.Api.Domain.Activities;
using GincanaHud.Api.Domain.Admin;
using GincanaHud.Api.Domain.Captures;
using GincanaHud.Api.Domain.Organizations;
using GincanaHud.Api.Domain.Pois;
using GincanaHud.Api.Domain.Users;
using GincanaHud.Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GincanaHud.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<Organization> Organizations => Set<Organization>();
	public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
	public DbSet<User> Users => Set<User>();
	public DbSet<Poi> Pois => Set<Poi>();
	public DbSet<Activity> Activities => Set<Activity>();
	public DbSet<ActivityPoi> ActivityPois => Set<ActivityPoi>();
	public DbSet<ActivityParticipant> ActivityParticipants => Set<ActivityParticipant>();
	public DbSet<Capture> Captures => Set<Capture>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Organization>(e =>
		{
			e.HasKey(x => x.Id);
			e.Property(x => x.Name).HasMaxLength(200).IsRequired();
			e.Property(x => x.CreatedAt).IsRequired();
		});

		modelBuilder.Entity<AdminUser>(e =>
		{
			e.HasKey(x => x.Id);
			e.Property(x => x.Username).HasMaxLength(100).IsRequired();
			e.HasIndex(x => x.Username).IsUnique();
			e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
			e.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
			e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId)
				.OnDelete(DeleteBehavior.Restrict);
			e.Property(x => x.CreatedAt).IsRequired();
		});

		modelBuilder.Entity<User>(e =>
		{
			e.HasKey(x => x.Id);
			e.Property(x => x.DisplayName).HasMaxLength(DisplayName.MaxLength).IsRequired();
			e.Property(x => x.ContactEmail).HasMaxLength(200);
			e.Property(x => x.ContactPhone).HasMaxLength(40);
			e.Property(x => x.CreatedAt).IsRequired();
		});

		modelBuilder.Entity<Poi>(e =>
		{
			e.HasKey(x => x.Id);
			e.Property(x => x.Name).HasMaxLength(PoiName.MaxLength).IsRequired();
			e.Property(x => x.Clue).HasMaxLength(ClueText.MaxLength).IsRequired();
			e.HasOne(x => x.Organization).WithMany(x => x.Pois).HasForeignKey(x => x.OrganizationId)
				.OnDelete(DeleteBehavior.Restrict);
			e.HasIndex(x => x.OrganizationId);
			e.Ignore(x => x.Location);
		});

		modelBuilder.Entity<Activity>(e =>
		{
			e.HasKey(x => x.Id);
			e.Property(x => x.Title).HasMaxLength(ActivityTitle.MaxLength).IsRequired();
			e.Property(x => x.Description).HasMaxLength(2000);
			e.Property(x => x.JoinCode).HasMaxLength(JoinCode.MaxLength).IsRequired();
			e.HasIndex(x => x.JoinCode).IsUnique();
			e.Property(x => x.RouteMode).HasConversion<string>().HasMaxLength(32);
			e.Property(x => x.StartsAt).IsRequired();
			e.Property(x => x.EndsAt).IsRequired();
			e.HasOne(x => x.Organization).WithMany(x => x.Activities).HasForeignKey(x => x.OrganizationId);
		});

		modelBuilder.Entity<ActivityPoi>(e =>
		{
			e.HasKey(x => new { x.ActivityId, x.PoiId });
			e.HasOne(x => x.Activity).WithMany(x => x.Pois).HasForeignKey(x => x.ActivityId);
			e.HasOne(x => x.Poi).WithMany(x => x.ActivityLinks).HasForeignKey(x => x.PoiId);
			e.HasIndex(x => new { x.ActivityId, x.Order }).IsUnique();
		});

		modelBuilder.Entity<ActivityParticipant>(e =>
		{
			e.HasKey(x => new { x.ActivityId, x.UserId });
			e.HasOne(x => x.Activity).WithMany(x => x.Participants).HasForeignKey(x => x.ActivityId);
			e.HasOne(x => x.User).WithMany(x => x.Participations).HasForeignKey(x => x.UserId);
			e.Property(x => x.JoinedAt).IsRequired();
		});

		modelBuilder.Entity<Capture>(e =>
		{
			e.HasKey(x => x.Id);
			e.HasIndex(x => new { x.UserId, x.ActivityId, x.PoiId }).IsUnique();
			e.HasOne(x => x.User).WithMany(x => x.Captures).HasForeignKey(x => x.UserId);
			e.HasOne(x => x.Activity).WithMany(x => x.Captures).HasForeignKey(x => x.ActivityId);
			e.HasOne(x => x.Poi).WithMany(x => x.Captures).HasForeignKey(x => x.PoiId);
		});
	}
}
