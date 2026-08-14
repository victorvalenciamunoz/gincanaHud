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

(En Development la Api usa una clave JWT de desarrollo si no configuras `Jwt:SigningKey`.)

Abre el dashboard de Aspire; Api y Admin quedan cableados a Postgres.

En el cliente MAUI, configura `ApiOptions.BaseUrl` en `MauiProgram.cs` (Dev Tunnel, IP de LAN o URL de Azure).

## Azure (demo puntual)

Despliegue a **Azure Container Apps** (Api + Admin). Postgres en Azure va a **Supabase** (fuera del resource group), así al borrar el RG de demo los datos siguen. En local el AppHost sigue levantando Postgres con Docker.

Prerrequisitos: Docker Desktop en marcha, `az login`, Aspire CLI (`aspire`), proyecto en [supabase.com](https://supabase.com).

Connection string de Supabase: **Project Settings → Database → Connection string → URI**, modo **Session** del pooler (puerto `5432` en el host `*.pooler.supabase.com`). Tradúcela a Npgsql:

```text
Host=aws-0-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<PROJECT_REF>;Password=<DB_PASSWORD>;SSL Mode=Require;Trust Server Certificate=true
```

(La región del pooler sale en el panel de Supabase; no copies esta de ejemplo.)

```bash
# Una vez
aspire secret set "Azure:SubscriptionId" "<tu-subscription-id>" --apphost src/GincanaHud.AppHost/GincanaHud.AppHost.csproj
aspire secret set "Azure:Location" "westeurope" --apphost src/GincanaHud.AppHost/GincanaHud.AppHost.csproj
aspire secret set "Azure:ResourceGroup" "rg-gincanahud-demo" --apphost src/GincanaHud.AppHost/GincanaHud.AppHost.csproj

# PowerShell: el '-' en el nombre de la variable de entorno no vale con $env:
Set-Item -Path "Env:ConnectionStrings__gincanahud" -Value "Host=...;SSL Mode=Require;Trust Server Certificate=true"
# o: aspire secret set "ConnectionStrings:gincanahud" "<npgsql>" --apphost src/GincanaHud.AppHost/GincanaHud.AppHost.csproj

aspire deploy --apphost src/GincanaHud.AppHost/GincanaHud.AppHost.csproj
```

Los parámetros `admin-username` / `admin-password` son los mismos que en local. En Azure hace falta también la clave JWT (≥32 caracteres):

```powershell
Set-Item -Path "Env:Parameters__admin-username" -Value "admin"
Set-Item -Path "Env:Parameters__admin-password" -Value "TU_PASSWORD"
Set-Item -Path "Env:Parameters__jwt-signing-key" -Value "UNA_CLAVE_LARGA_DE_AL_MENOS_32_CHARS"
```

**Teardown (el RG de Azure; Supabase no se toca):**

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
