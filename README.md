# GincanaHud

Gincanas GPS con HUD táctico (MAUI) + API ASP.NET Core + Admin Blazor + PostgreSQL vía .NET Aspire.

## Requisitos

- .NET SDK 10 / 11 Preview
- Docker Desktop (Aspire levanta Postgres)
- Workload MAUI (cliente Android)

## Arranque local

```bash
dotnet user-secrets set "Parameters:admin-username" "admin" --project src/GincanaHud.AppHost
dotnet user-secrets set "Parameters:admin-password" "ELIGE_UNA" --project src/GincanaHud.AppHost
dotnet run --project src/GincanaHud.AppHost
```

Abre el dashboard de Aspire; Api y Admin quedan cableados a Postgres.

En el cliente MAUI, configura `ApiOptions.BaseUrl` en `MauiProgram.cs` (Dev Tunnel, IP de LAN o URL de Azure).

## Proyectos

| Proyecto | Rol |
|----------|-----|
| `GincanaHud.AppHost` | Aspire: Postgres + Api + Admin |
| `GincanaHud.Api` | REST + EF Core + dominio |
| `GincanaHud.Admin` | Blazor Server (eventos, POIs, ranking en vivo) |
| `GincanaHud.App` | MAUI HUD (Android-first) |
| `GincanaHud.Shared` | DTOs / GeoMath |
| `GincanaHud.ServiceDefaults` | OpenTelemetry / health / HttpClient |

Más detalle en `docs/` (`STATUS`, `ARCHITECTURE`, `DOMAIN`, `DECISIONS`).
