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

## Azure (demo puntual)

Despliegue a **Azure Container Apps** con Aspire. Pensado para demos: al terminar se **borra el resource group** para no acumular coste. El run local no cambia (Postgres sigue con volumen Docker; en Azure el contenedor Postgres es **efímero**, sin `WithDataVolume`, porque Azure Files rompe `initdb`).

Prerrequisitos: Docker Desktop en marcha, `az login`, Aspire CLI (`aspire`).

```bash
# Una vez (o al prompt de aspire deploy)
aspire secret set "Azure:SubscriptionId" "<tu-subscription-id>" --apphost src/GincanaHud.AppHost/GincanaHud.AppHost.csproj
aspire secret set "Azure:Location" "westeurope" --apphost src/GincanaHud.AppHost/GincanaHud.AppHost.csproj
aspire secret set "Azure:ResourceGroup" "rg-gincanahud-demo" --apphost src/GincanaHud.AppHost/GincanaHud.AppHost.csproj

aspire deploy --apphost src/GincanaHud.AppHost/GincanaHud.AppHost.csproj
```

Los parámetros `admin-username` / `admin-password` son los mismos que en local (user-secrets o prompt).

**Teardown (obligatorio tras la demo):**

```bash
az group delete -n rg-gincanahud-demo --yes --no-wait
```

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
