# Status

**Phase:** 6+ / D16 evento + **D17 roles Admin**  
**Updated:** 2026-08-14

## Done (reciente)

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

### JWT Admin (Api)
- Login emite `AccessToken`; Admin cookie guarda el token y `AdminApiAuthHandler` lo manda como Bearer
- Políticas `Admin` / `SuperAdmin`; endpoints MAUI siguen `AllowAnonymous`

## Known issues / ops

| Tema | Nota |
|------|------|
| Schema | seeder recrea si faltan tablas AdminUsers |
| SuperAdmin | user-secrets `Parameters:admin-username` / `admin-password` |
| Join MAUI | **done** — pestaña Unirse + QR + Iniciar carga POI |
| Api auth | JWT Admin en rutas de gestión; móvil sigue anónimo |

## Next

1. Redeploy ACA cuando haga falta (mismos secrets + connection string Supabase; no recrear el proyecto)
2. Probar AR marker en calle (FOV/pitch finos si hace falta)
3. Estética Unirse / Admin
4. Key Vault Azure (cuando el entorno cloud deje de ser solo demo)

## Explicitly deferred

- OAuth / Entra ID  
- Magic link jugador  
- JWT jugador (móvil)  
- DB por organización  
