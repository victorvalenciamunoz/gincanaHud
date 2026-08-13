using GincanaHud.Api.Domain.Activities;
using GincanaHud.Api.Domain.Admin;
using GincanaHud.Api.Domain.Organizations;
using GincanaHud.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GincanaHud.Api.Data;

public static class DbSeeder
{
	public static readonly Guid DemoOrgId = Guid.Parse("00000000-0000-0000-0000-0000000000A1");
	public static readonly Guid DemoActivityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
	public static readonly Guid DemoUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
	public const string DemoJoinCode = "DEMO01";

	public static async Task SeedAsync(
		AppDbContext db,
		IPasswordHasher<AdminUser> passwordHasher,
		IOptions<AdminBootstrapOptions> bootstrap,
		CancellationToken ct = default)
	{
		try
		{
			await db.Database.EnsureCreatedAsync(ct);
			_ = await db.Organizations.AnyAsync(ct);
			_ = await db.Activities.Select(a => a.JoinCode).AnyAsync(ct);
			_ = await db.Activities.Select(a => a.RouteMode).AnyAsync(ct);
			_ = await db.Users.Select(u => u.ContactEmail).AnyAsync(ct);
			_ = await db.ActivityParticipants.AnyAsync(ct);
			_ = await db.AdminUsers.AnyAsync(ct);
			_ = await db.Pois.Select(p => p.OrganizationId).AnyAsync(ct);
		}
		catch
		{
			// Side project: sin migraciones. Schema desfasado → recrear limpio.
			await db.Database.EnsureDeletedAsync(ct);
			await db.Database.EnsureCreatedAsync(ct);
		}

		await EnsureSuperAdminAsync(db, passwordHasher, bootstrap.Value, ct);

		if (await db.Activities.AnyAsync(ct))
			return;

		var org = Organization.Create("Demo Organizador", DemoOrgId);
		if (org.IsError)
			throw new InvalidOperationException(org.FirstError.Description);
		db.Organizations.Add(org.Value);

		var now = DateTimeOffset.UtcNow;
		var activity = Activity.Create(
			DemoOrgId,
			"Demo local HUD",
			"Actividad de prueba. Únete con código DEMO01 o crea puntos cerca.",
			startsAt: now.AddHours(-1),
			endsAt: now.AddDays(7),
			joinCode: DemoJoinCode,
			id: DemoActivityId);
		if (activity.IsError)
			throw new InvalidOperationException(activity.FirstError.Description);
		db.Activities.Add(activity.Value);

		var user = User.Register("jugador", contactEmail: null, contactPhone: null, id: DemoUserId);
		if (user.IsError)
			throw new InvalidOperationException(user.FirstError.Description);
		db.Users.Add(user.Value);

		await db.SaveChangesAsync(ct);

		var tracked = await db.Activities.Include(a => a.Participants)
			.FirstAsync(a => a.Id == DemoActivityId, ct);
		var joined = tracked.RegisterParticipant(DemoUserId, now);
		if (joined.IsError)
			throw new InvalidOperationException(joined.FirstError.Description);
		await db.SaveChangesAsync(ct);
	}

	private static async Task EnsureSuperAdminAsync(
		AppDbContext db,
		IPasswordHasher<AdminUser> passwordHasher,
		AdminBootstrapOptions opts,
		CancellationToken ct)
	{
		if (!opts.IsConfigured)
			return;

		var username = opts.Username.Trim();
		var existing = await db.AdminUsers.FirstOrDefaultAsync(a => a.Username == username, ct);

		if (existing is null)
		{
			var created = AdminUser.CreateSuperAdmin(username, "pending");
			if (created.IsError)
				throw new InvalidOperationException(created.FirstError.Description);

			var hash = passwordHasher.HashPassword(created.Value, opts.Password);
			created.Value.ReplacePasswordHash(hash);
			db.AdminUsers.Add(created.Value);
			await db.SaveChangesAsync(ct);
			return;
		}

		// El usuario de bootstrap debe ser SuperAdmin (p. ej. si se creó antes como admin de empresa).
		var changed = false;
		if (existing.Role != AdminRole.SuperAdmin || existing.OrganizationId is not null)
		{
			existing.PromoteToSuperAdmin();
			changed = true;
		}

		var verify = passwordHasher.VerifyHashedPassword(existing, existing.PasswordHash, opts.Password);
		if (verify == PasswordVerificationResult.Failed)
		{
			existing.ReplacePasswordHash(passwordHasher.HashPassword(existing, opts.Password));
			changed = true;
		}

		if (changed)
			await db.SaveChangesAsync(ct);
	}
}
