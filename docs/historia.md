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


## Fase 13 — Tests y CI (cerrada el 2026-10-30)

- [x] Paso a — dominio y CI (2026-10-29): proyecto `tests/GincanaHud.Tests` (xUnit v3, Microsoft.Testing.Platform
  activado en `global.json`) con 44 tests de `Activity` (ventana de juego, UTC, código de unión, participantes,
  POIs), objetos de valor y `GeoMath`. Workflow `CI` (`ci.yml`): en cada PR y en `main`, build con avisos como
  errores y tests. `main` protegida: exige «Build y tests» en verde, también al administrador (D23). Las acciones
  de los tres workflows pasan a `checkout@v7` y `setup-dotnet@v6` (Node 24).
  Validado: en local y en el CI, 0 avisos y 44 tests en verde; un test roto a propósito puso el CI en rojo y la PR
  en `BLOCKED`; al deshacerlo, verde y `CLEAN`. Despliegue y keepalive en verde con las acciones nuevas.
- [x] Paso b — handlers contra Postgres (2026-10-30): 16 tests en `tests/GincanaHud.Tests/Handlers` contra un
  Postgres 17 de Testcontainers con las migraciones aplicadas (`PostgresFixture`, un contenedor para la colección;
  `TestData` siembra por el dominio). `JoinActivityHandler`: código válido (también en minúsculas), inexistente
  (`NotFound`), actividad caducada o inactiva, y el mismo nombre dos veces deja un solo participante.
  `CapturePoiHandler`: lejos sin pista ni puntos, dentro del radio con pista y puntos, orden secuencial, ruta libre
  en cualquier orden, antes de empezar y tras acabar, repetir no suma dos veces, y la hora de la cola sin conexión
  (+01:00) se guarda en UTC. `GetRankingHandler`: orden (quien termina antes, luego más capturas, luego la última
  captura más temprana; sin capturas no sale), puntos, capturas y meta de cada fila, y actividad inexistente.
  - Cambio de comportamiento: **el servidor exige el orden en la ruta secuencial** (D19, decidido por el autor);
    antes solo lo respetaba el HUD. `CapturePoiHandler` reescrito sin las líneas en blanco dobles que tenía.
  - `CLAUDE.md` (sección Repo) y README al día: hay CI, `main` protegida y los tests necesitan Docker.
  Validado: en local, 0 avisos y 60 tests en verde (~14 s con el contenedor). Quitar a propósito el paso a UTC de
  `Capture.Record` pone en rojo el test de la cola con el error de Npgsql de la fase 12.
- [x] Paso c — cliente del panel (2026-10-30): 13 tests de `AdminApiClient` en `tests/GincanaHud.Tests/Panel` con
  un `HttpMessageHandler` falso. 404, 409, 400 y 403 llegan como `ErrorOr` con el código y el motivo de la Api (o un
  texto propio sin cuerpo), también al borrar; 401, 5xx y fallos de red, como `HttpRequestException`; un 200 se lee
  con los enums como texto, un 200 con `null` es excepción y un 204 es `Success`. Respuestas en `Panel/Fixtures`: el
  404 grabado de la nube (el 401 y el 400 se grabaron sin cuerpo); el 409 y el 200, a mano con la misma forma.
  Validado: en local y en el CI (PR #14), 0 avisos y 73 tests en verde; hacer que el 401 llegue como error pone en
  rojo su test.
- [x] Juez: el CI corre en cada PR con los tres grupos (dominio, handlers contra Postgres, cliente del panel) y
  `main` exige «Build y tests» en verde, también al administrador (comprobado el 2026-10-30).
