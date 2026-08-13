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

// Azure Files (volumen en ACA) no permite el chmod que exige initdb de Postgres.
// Volumen solo en run local; en publish/deploy los datos son efímeros (vale para demos).
var postgresServer = builder.AddPostgres("postgres")
	.WithHostPort(5432);

if (!builder.ExecutionContext.IsPublishMode)
{
	postgresServer.WithDataVolume();
}

var postgres = postgresServer.AddDatabase("gincanahud");

// Dev: user-secrets AppHost Parameters:*. Azure demo: mismos Parameters en aspire deploy.
var adminUsername = builder.AddParameter("admin-username");
var adminPassword = builder.AddParameter("admin-password", secret: true);

static void ScaleToZero(Azure.Provisioning.AppContainers.ContainerApp app)
{
	// Solo afecta al publish/deploy; el run local no usa este callback.
	app.Template.Scale.MinReplicas = 0;
}

var api = builder.AddProject<Projects.GincanaHud_Api>("api")
	.WithReference(postgres)
	.WaitFor(postgres)
	.WithEnvironment("AdminBootstrap__Username", adminUsername)
	.WithEnvironment("AdminBootstrap__Password", adminPassword)
	.WithExternalHttpEndpoints()
	.PublishAsAzureContainerApp((_, app) => ScaleToZero(app));

builder.AddProject<Projects.GincanaHud_Admin>("admin")
	.WithReference(api)
	.WaitFor(api)
	.WithExternalHttpEndpoints()
	.PublishAsAzureContainerApp((_, app) => ScaleToZero(app));

builder.Build().Run();
