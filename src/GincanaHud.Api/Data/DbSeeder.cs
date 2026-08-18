using GincanaHud.Api.Domain.Activities;
using GincanaHud.Api.Domain.Admin;
using GincanaHud.Api.Domain.Organizations;
using GincanaHud.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;

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
		IHostEnvironment environment,
		CancellationToken ct = default)
	{
		try
		{
			await EnsureAppSchemaAsync(db, ct);
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
			// Nunca EnsureDeleted contra Supabase / Azure: droparía la BBDD compartida.
			if (!environment.IsDevelopment())
				throw;

			await db.Database.EnsureDeletedAsync(ct);
			await db.Database.EnsureCreatedAsync(ct);
		}

		await EnsureSuperAdminAsync(db, passwordHasher, bootstrap.Value, ct);

		if (await db.Activities.AnyAsync(ct))
		{
			// Dev: DEMO01 se crea con +7 días; si el volumen Docker persiste, caduca y el join da 400.
			if (environment.IsDevelopment())
				await RefreshDemoActivityWindowIfNeededAsync(db, ct);
			return;
		}

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

	private static async Task EnsureAppSchemaAsync(AppDbContext db, CancellationToken ct)
	{
		await db.Database.EnsureCreatedAsync(ct);

		try
		{
			_ = await db.Organizations.AnyAsync(ct);
			return;
		}
		catch (Exception ex) when (IsUndefinedTable(ex))
		{
			// Supabase ya tiene la BBDD `postgres` (auth/storage). EnsureCreated no crea
			// nuestras tablas si el servidor ya tiene cualquier relación.
			var creator = db.GetService<IRelationalDatabaseCreator>();
			await creator.CreateTablesAsync(ct);
		}
	}

	private static bool IsUndefinedTable(Exception ex)
	{
		for (var e = ex; e is not null; e = e.InnerException)
		{
			if (e is PostgresException pg && pg.SqlState == PostgresErrorCodes.UndefinedTable)
				return true;
		}

		return false;
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

	private static async Task RefreshDemoActivityWindowIfNeededAsync(AppDbContext db, CancellationToken ct)
	{
		var demo = await db.Activities.FirstOrDefaultAsync(a => a.Id == DemoActivityId, ct);
		if (demo is null)
			return;

		var now = DateTimeOffset.UtcNow;
		if (demo.IsOpenForJoin(now))
			return;

		var updated = demo.Update(
			demo.Title,
			demo.Description,
			isActive: true,
			startsAt: now.AddHours(-1),
			endsAt: now.AddDays(7),
			demo.RouteMode);
		if (updated.IsError)
			throw new InvalidOperationException(updated.FirstError.Description);

		await db.SaveChangesAsync(ct);
	}
}
