# GincanaHud

Gincanas GPS con HUD táctico (MAUI) + Api en Azure Functions + panel Blazor WebAssembly + PostgreSQL, con .NET Aspire en local.

## Requisitos

- .NET SDK 10 / 11 Preview
- Docker Desktop (Aspire levanta Postgres)
- Azure Functions Core Tools (`func`) en el PATH
- Workload MAUI (cliente Android)

## Arranque local

```bash
dotnet user-secrets set "Parameters:admin-username" "admin" --project src/GincanaHud.AppHost
dotnet user-secrets set "Parameters:admin-password" "ELIGE_UNA" --project src/GincanaHud.AppHost
dotnet run --project src/GincanaHud.AppHost
```

(En Development la Api usa una clave JWT de desarrollo si no configuras `Jwt:SigningKey`.)

Abre el dashboard de Aspire: Postgres, Functions (puerto 5263) y panel.

En el cliente MAUI, `ApiOptions.BaseUrl` en `MauiProgram.cs` apunta a la nube. Para probar contra las Functions
locales: `http://127.0.0.1:5263/` y `adb reverse tcp:5263 tcp:5263` con el móvil por USB.

## Nube (0 €)

Panel y Functions en **Azure Static Web Apps Free**; Postgres en **Supabase Free** (D20, D22).

El workflow **Despliegue** (`.github/workflows/deploy.yml`), lanzado a mano, aplica las migraciones en Supabase y
publica panel y Functions:

```bash
gh workflow run deploy.yml
```

- Secretos de GitHub: `SUPABASE_DB_URL` (Session pooler, también lo usa el keepalive) y
  `AZURE_STATIC_WEB_APPS_API_TOKEN`.
- Ajustes de las Functions, en el Static Web App (portal → Variables de entorno): `ConnectionStrings__gincanahud`
  (Session pooler en formato Npgsql), `Jwt__SigningKey` (≥ 32 caracteres), `AdminBootstrap__Username` y
  `AdminBootstrap__Password`. No los pegues en el repo.

## Proyectos

| Proyecto | Rol |
|----------|-----|
| `GincanaHud.AppHost` | Aspire en local: Postgres + Functions + panel |
| `GincanaHud.Api` | Azure Functions .NET 10 isolated: dominio, EF Core, JWT |
| `GincanaHud.Admin` | Blazor WebAssembly (eventos, POIs, ranking en vivo) |
| `GincanaHud.App` | MAUI HUD (Android-first) |
| `GincanaHud.Shared` | DTOs / GeoMath |
| `GincanaHud.ServiceDefaults` | OpenTelemetry / health / HttpClient |

Más detalle en `docs/` (`STATUS`, `ARCHITECTURE`, `DOMAIN`, `DECISIONS`).
