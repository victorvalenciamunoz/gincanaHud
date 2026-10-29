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

// Solo en local. La nube es Static Web Apps + Supabase y la despliega el workflow Despliegue (D22).
var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("postgres")
	.WithHostPort(5432)
	.WithDataVolume()
	.AddDatabase("gincanahud");

// user-secrets del AppHost: Parameters:admin-username / Parameters:admin-password.
var adminUsername = builder.AddParameter("admin-username");
var adminPassword = builder.AddParameter("admin-password", secret: true);

// Functions .NET 10 isolated (D20): en local con Core Tools (`func`) y Azurite para el almacenamiento del host.
// La firma JWT usa el valor de desarrollo de la Api.
var api = builder.AddAzureFunctionsProject<Projects.GincanaHud_Api>("api")
	.WithReference(db)
	.WithEnvironment("AdminBootstrap__Username", adminUsername)
	.WithEnvironment("AdminBootstrap__Password", adminPassword)
	.WithExternalHttpEndpoints()
	.WaitFor(db);

// Blazor WebAssembly: en local lo sirve su DevServer y llama a las Functions en :5263 (CORS en launchSettings de la Api).
builder.AddProject<Projects.GincanaHud_Admin>("admin")
	.WaitFor(api)
	.WithExternalHttpEndpoints();

builder.Build().Run();
