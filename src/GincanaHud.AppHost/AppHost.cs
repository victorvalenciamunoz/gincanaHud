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

var postgres = builder.AddPostgres("postgres")
	.WithHostPort(5432)
	.WithDataVolume()
	.AddDatabase("gincanahud");

// Dev: user-secrets AppHost Parameters:*. Azure: Key Vault (D15/D17).
var adminUsername = builder.AddParameter("admin-username");
var adminPassword = builder.AddParameter("admin-password", secret: true);

var api = builder.AddProject<Projects.GincanaHud_Api>("api")
	.WithReference(postgres)
	.WaitFor(postgres)
	.WithEnvironment("AdminBootstrap__Username", adminUsername)
	.WithEnvironment("AdminBootstrap__Password", adminPassword)
	.WithExternalHttpEndpoints();

builder.AddProject<Projects.GincanaHud_Admin>("admin")
	.WithReference(api)
	.WaitFor(api)
	.WithExternalHttpEndpoints();

builder.Build().Run();
