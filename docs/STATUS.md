# Status

**Phase:** 6+ / D16 evento + **D17 roles Admin**  
**Updated:** 2026-08-17

## Done (reciente)

### HUD offline — ruta cacheada
- Tras Unirse se guarda el detalle de la actividad en el teléfono
- Cold start sin red usa esa ruta + cola de capturas; al volver Online se sincroniza

### Estética Unirse / Admin — cerrado
- Unirse: tarjeta de código, error solo cuando falla, sesión en card
- Admin: login centrado, inicio con tarjetas, Chakra Petch + IBM Plex, nav activa mint

### AR marker en calle — cerrado
- FOV/pitch actuales (`ArProjection` 62° / bias 10°) suficientes; no se tocan constantes

### JWT Admin (Api) — cerrado
- Login emite `AccessToken`; Admin cookie + Bearer; 401 sin token verificado en local
- Políticas `Admin` / `SuperAdmin`; endpoints MAUI anónimos

### D17 — SuperAdmin vs OrganizationAdmin
- `AdminUser` en Api (hash); login `POST /api/admin-auth/login`
- Bootstrap SuperAdmin desde Aspire parameters → `AdminBootstrap__*`
- SuperAdmin: organizaciones + crear admins de empresa (`/org-admins`)
- OrganizationAdmin: actividades de su org + **Jugadores** (participantes)
- Cookie Admin con claims rol + org

### D16 — Evento
- Org, JoinCode, caducidad, participantes, contacto premios
- Admin QR; **MAUI Unirse** (código/QR) + sesión; Iniciar carga primer POI sin capturar

### Azure demo + Supabase
- ACA (Api + Admin) con `aspire deploy`; Postgres **fuera** del RG (Supabase session pooler)
- Seeder: `CreateTables` si la BBDD ya existe (Supabase); no `EnsureDeleted` en cloud
- Login Admin validado; tablas visibles en Table Editor
- Tras la demo: borrar `rg-gincanahud-demo` → coste Azure ~0; **datos siguen en Supabase**

## Known issues / ops

| Tema | Nota |
|------|------|
| Schema | seeder recrea si faltan tablas AdminUsers (solo Development) |
| SuperAdmin | user-secrets `Parameters:admin-username` / `admin-password` |
| Join MAUI | **done** — pestaña Unirse + QR + Iniciar carga POI |
| Api auth | JWT Admin cerrado; móvil sigue anónimo |
| Android crash `jumpToEnd` | bug de IDs tras Fast Deployment (.NET 11). `EmbedAssembliesIntoApk=true`. Si vuelve: desinstalar la app del móvil y redesplegar |

## Next

1. Redeploy ACA cuando haga falta (`jwt-signing-key` + Supabase)
2. Key Vault Azure (cuando el entorno cloud deje de ser solo demo)

## Explicitly deferred

- OAuth / Entra ID  
- Magic link jugador  
- JWT jugador (móvil)  
- DB por organización  
