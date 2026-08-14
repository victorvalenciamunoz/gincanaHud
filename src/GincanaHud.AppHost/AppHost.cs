// Workaround: con F5 desde el IDE, DCP a menudo falla al lanzar Api/Admin
// ("Executable run request: 500"). Sin sesión de debug, DCP arranca los
// proyectos como procesos normales (como `dotnet run`).
// Opt-in al launch vía IDE: GINCANA_USE_IDE_DEBUG=1
if (!string.Equals(Environment.GetEnvironmentVariable("GINCANA_USE_IDE_DEBUG"), "1", StringComparison.Ordinal))
{
	Environment.SetEnvironmentVariable("DEBUG_SESSION_PORT", null);
	Environment.SetEnvironmentVariable("DEBUG_SESSION_TOKEN", null);
	Environment.SetEnvironmentVariable("DEBUG_SESSION_INFO", null);
	Environment.SetEnvironmentVariable("DEBUG_SESSION_SERVER_CERTIFICATE", null);
}

var builder = DistributedApplication.CreateBuilder(args);

// Solo se usa en publish/deploy a Azure; el run local sigue con Docker + procesos.
builder.AddAzureContainerAppEnvironment("aca-env");

// Local: Postgres en Docker. Azure: connection string externa (Supabase).
IResourceBuilder<IResourceWithConnectionString> db;
if (builder.ExecutionContext.IsPublishMode)
{
	db = builder.AddConnectionString("gincanahud");
}
else
{
	db = builder.AddPostgres("postgres")
		.WithHostPort(5432)
		.WithDataVolume()
		.AddDatabase("gincanahud");
}

// Dev: user-secrets AppHost Parameters:*. Azure demo: mismos Parameters en aspire deploy.
var adminUsername = builder.AddParameter("admin-username");
var adminPassword = builder.AddParameter("admin-password", secret: true);

static void ScaleToZero(Azure.Provisioning.AppContainers.ContainerApp app)
{
	// Solo afecta al publish/deploy; el run local no usa este callback.
	app.Template.Scale.MinReplicas = 0;
}

var api = builder.AddProject<Projects.GincanaHud_Api>("api")
	.WithReference(db)
	.WithEnvironment("AdminBootstrap__Username", adminUsername)
	.WithEnvironment("AdminBootstrap__Password", adminPassword)
	.WithExternalHttpEndpoints()
	.PublishAsAzureContainerApp((_, app) => ScaleToZero(app));

// Firma JWT: en Azure vía parámetro; en Development la Api tiene fallback local.
if (builder.ExecutionContext.IsPublishMode)
{
	var jwtSigningKey = builder.AddParameter("jwt-signing-key", secret: true);
	api.WithEnvironment("Jwt__SigningKey", jwtSigningKey);
}

if (!builder.ExecutionContext.IsPublishMode)
	api.WaitFor(db);

builder.AddProject<Projects.GincanaHud_Admin>("admin")
	.WithReference(api)
	.WaitFor(api)
	.WithExternalHttpEndpoints()
	.PublishAsAzureContainerApp((_, app) => ScaleToZero(app));

builder.Build().Run();
