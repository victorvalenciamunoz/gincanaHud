# Historia

Fases y pasos cerrados, tal como quedaron en `STATUS.md`.

## Hasta la fase 11 (actualizado 2026-09-14)


### Supabase keepalive (GitHub Action)
- Cada run hace un **burst** (varias queries SQL; REST si hay secrets) — un solo ping no basta
- Cron diario + manual
- Obligatorio: `SUPABASE_DB_URL`
- Recomendado: `SUPABASE_URL` + `SUPABASE_ANON_KEY` (actividad vía API, lo que Supabase cuenta mejor)

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

