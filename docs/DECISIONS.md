# Decisions

## D1 — Runtime

- **MAUI App**: .NET **11 Preview** + MAUI 11 (XAML C# Expressions, latest HUD tooling).
- **Api + Aspire AppHost + ServiceDefaults**: .NET **10** — Aspire 13.x AppHost cannot reference net11 projects yet; Npgsql/EF packages align with net10.

`global.json` pins the newest installed SDK for builds; project TFMs differ on purpose.

## D2 — UI

**MAUI XAML + MVVM** (`CommunityToolkit.Mvvm`). Not Blazor Hybrid — HUD overlays need tight sensor/graphics loops.

## D3 — Database + orchestration

**PostgreSQL** via **.NET Aspire** AppHost (container resource). Aspire owns local orchestration, connection strings, and the dashboard.

- Dev connection is injected by Aspire into the Api; do not hardcode secrets in app code.
- Keep a thin `docker-compose.yml` only as a fallback if Aspire is unavailable; prefer `dotnet run --project src/GincanaHud.AppHost`.

## D4 — Arrival validation

Official check is **server-side** (`POST .../capture`). Client may precompute distance for HUD only.

## D5 — HUD lock thresholds (MVP defaults)

- Proximity pulse: distance < **15 m**
- Lock: within checkpoint `RadiusMeters` **and** heading within **±15°** of target bearing

## D6 — MAUI XAML C# Expressions

Prefer **XAML C# Expressions** (MAUI 11 / XamlSourceGen) for HUD formatting, visibility, and simple UI math instead of `IValueConverter` boilerplate or extra ViewModel display properties.

Requirements when scaffolding the App:

- Enable SourceGen inflator: `<MauiXamlInflator>SourceGen</MauiXamlInflator>`
- Enable `<EnablePreviewFeatures>true</EnablePreviewFeatures>`
- Set `x:DataType` on pages/views
- Keep domain/sensor logic in services + ViewModels; expressions are for **presentation** only
- ViewModel members used from XAML expressions must be **explicit properties** (XamlSourceGen does not yet see `[ObservableProperty]` / `[RelayCommand]` generated members)
- Feature is experimental — if a preview breaks builds, fall back to typed bindings temporarily and note it in `STATUS.md`

## D7 — .NET Aspire

Use **Aspire** AppHost for containers and local multi-service run (Postgres + Api + futuros hosts). Optional: ServiceDefaults on Api/MAUI/Admin for HTTP resilience and discovery.

## D8 — Producto: Actividades, POIs, usuarios, ranking

Más allá del HUD técnico:

- Crear/gestionar **POIs** y **Actividades**; asignar POIs a actividades.
- **Usuarios/jugadores** (varios concurrentes).
- Al capturar un POI, persistir **fecha/hora**.
- Con **ruta secuencial** (`Order`), el ranking de premios es **orden de llegada a la meta** (timestamp de captura del último POI), no suma de puntos. Los puntos quedan como feedback de captura / futuro modo libre.
- **Cola offline de capturas**: si falla la red en LOCK, el móvil guarda lat/lon + `CapturedAt`, sigue la ruta, y reenvía al volver `API Online` (el servidor respeta ese timestamp). Tras Unirse se cachea el detalle de la actividad para poder arrancar el HUD al reabrir la app sin cobertura.

El cliente móvil sigue siendo MAUI HUD; la gestión no va en la app de calle.

## D9 — Admin UI: Blazor Server

> Se sustituye por Blazor WebAssembly en la fase 12, paso 4 (D20).

Panel de gestión (**Blazor Server**, host propio en el monorepo, orquestado por Aspire) sobre la misma API/DB:

- CRUD POIs, actividades, asignación, usuarios.
- Consulta de capturas y ranking.

La API REST se mantiene para la app MAUI; Blazor no sustituye el HUD ni Blazor Hybrid en el móvil.

## D10 — HTTP resilience on the MAUI client

Use **`Microsoft.Extensions.Http.Resilience`** (`AddStandardResilienceHandler`) — the current framework stack (Polly underneath). Do not add a raw Polly package unless we need custom policies beyond the standard handler.

## D11 — Admin: mapa para colocar POIs

Los POIs de una actividad se pueden crear **sin estar en el sitio** (preparar desde casa un evento en otra ciudad):

- Mapa **Leaflet** + tiles OSM en el detalle de actividad del Admin.
- Búsqueda de lugar vía **Nominatim** (HttpClient server-side; User-Agent propio).
- Clic en el mapa → Lat/Lon del formulario → `POST .../activities/{id}/pois`.
- El mapa se abre centrado en los POIs de la actividad; si no hay ninguno, en la vista de Europa entera.
  Ningún sitio real como centro por defecto ni como ejemplo de búsqueda: el repo es público.
- La app MAUI no sustituye esto: el HUD sigue siendo captura en calle; el mapa es herramienta de autoría.

## D12 — Aspire y launch desde el IDE

F5 vía IDE a menudo falla con `Executable run request: 500` (DCP → sesión IDE). El AppHost, por defecto, limpia `DEBUG_SESSION_*` para que Api/Admin arranquen como procesos OS. Opt-in: `GINCANA_USE_IDE_DEBUG=1`. Preferir `dotnet run --project src/GincanaHud.AppHost` si hay dudas.

## D13 — Arquitectura Api/Admin: vertical slices + CQRS ligero

Ver detalle en `docs/ARCHITECTURE.md`.

**Elegido**

- Estructura por **entidad/feature** (`Features/{Entity}/{UseCase}/`).
- **CQRS**: commands vs queries; endpoints Minimal API delgados.
- **Mediator propio** (`ISender` + handlers) — **sin MediatR** ni paquetes mediator de terceros salvo decisión explícita futura.
- **Result**: paquete **ErrorOr** (unión discriminada de librería). No hay union types estables en el lenguaje; no inventar un Result ad hoc si ErrorOr cubre el caso.
- Principios: **YAGNI**, **SOLID**, Clean Code, **early return**.
- Clean Architecture **lite**: un proyecto Api con capas por carpetas (`Features` / `Infrastructure` / `Common`). No split multi-proyecto hasta que duela.

**Admin**

- Sigue siendo cliente HTTP de la Api. No segundo bus CQRS en Blazor.

**Rechazado (por ahora)**

- MediatR.
- Solution Clean Architecture de 4+ proyectos.
- Excepciones para NotFound/Validation en el flujo feliz de handlers.

## D14 — DDD táctico (aggregates + value objects)

Ver `docs/ARCHITECTURE.md` y `docs/DOMAIN.md`.

**Elegido**

- DDD **táctico** dentro del proyecto Api (`Domain/` + features CQRS).
- **Aggregates**: `User`, `Poi`, `Activity` (incluye `ActivityPoi`), `Capture` (creado vía reglas de Activity/captura).
- **Value objects** donde hay reglas: p. ej. `DisplayName`, `GeoCoordinate`, `RadiusMeters`, `Clue`, `Points`, `ActivityTitle`.
- Invariantes en el dominio; handlers orquestan; queries proyectan DTOs (sin hidratar agregados de más).
- Errores de negocio vía **ErrorOr**, no excepciones.

**Rechazado (por ahora)**

- Event sourcing, bus de domain events, sagas, outbox.
- Microservicios / bounded contexts físicos separados.
- Value object para cada primitivo sin regla (YAGNI).

## D15 — Auth básica en Admin

Panel Admin con **cookie authentication**. Sustituido parcialmente por **D17** (cuentas en Api).

| Entorno | Origen |
|---------|--------|
| **Desarrollo** | User Secrets Aspire (`Parameters:admin-username` / `admin-password`) → bootstrap SuperAdmin en Api |
| **Azure demo** | Mismos `Parameters` en `aspire deploy` (sin Key Vault aún) |
| **Azure (futuro)** | Key Vault → mismos parámetros |

## D18 — Azure demo: ACA + borrar resource group

> Se retira en la fase 12, paso 5 (D20).

**Contexto:** portfolio / aprendizaje; no queremos coste fijo 24/7.

**Elegido**

- Despliegue con `aspire deploy` a **Azure Container Apps** (Api + Admin).
- **Postgres en Supabase** (connection string `gincanahud` en publish). Local sigue con `AddPostgres` + volumen Docker.
- Api/Admin con `MinReplicas = 0` (scale-to-zero).
- Tras la demo: `az group delete -n rg-gincanahud-demo` → coste Azure ~0; **los datos quedan en Supabase**.

**Rechazado (por ahora)**

- GitHub Actions / CI de deploy (sí hay un workflow de keepalive SQL a Supabase; no CI de build/deploy).
- Azure Database for PostgreSQL Flexible Server.
- Key Vault en el primer deploy.
- Postgres como contenedor en ACA (Azure Files rompe `initdb` / `chmod`).

Login `/account/login` (valida contra Api), logout `/account/logout`, `FallbackPolicy` autenticado.

## D16 — Evento de un día: organización, join code/QR, caducidad, identidad ligera

**Contexto de producto**

- Caso típico: gincana / búsqueda del tesoro **de un día** (empresa de aire libre, fiestas de un pueblo).
- Varias organizaciones; cada una tiene actividades distintas que **caducan**.
- El jugador se une a **una actividad** (no a un SaaS genérico).
- Premios: hace falta identificar al jugador de forma ligera (nombre + contacto opcional).

**Elegido**

| Concepto | Modelo |
|----------|--------|
| **Organization** | Quien organiza (empresa, ayuntamiento…). Aisla datos. |
| **Activity** | Unidad de juego del evento. Tiene `JoinCode`, `StartsAt`, `EndsAt`, `OrganizationId`. |
| **Poi** | Punto del catálogo de la organización (`OrganizationId`). Se enlaza a actividades vía `ActivityPoi`. |
| **Join** | Código corto y/o **QR** (mismo payload). Canales: cartel, email, WhatsApp. |
| **User** | Nombre visible + `ContactEmail` / `ContactPhone` opcionales (premios). |
| **ActivityParticipant** | Usuario unido a esa actividad. Capturar exige participación. |
| **Caducidad** | Tras `EndsAt`: no join ni capture. Antes de `StartsAt`: join OK, capture no. |

Flujo jugador: escanear QR o teclear código → nombre (+ contacto) → jugar → ranking con contacto para entregar premios.

**Rechazado (por ahora)**

- Login/password o magic link como puerta de entrada al juego.
- Una DB por organización.
- QR sin código de respaldo tecleable.
- Identidad fuerte (SSO) en el móvil MVP.

## D17 — Admins de plataforma vs admins de organización

**Roles**

| Rol | Puede |
|-----|--------|
| **SuperAdmin** | Crear organizaciones; crear admins de empresa; ver/gestionar todo (soporte). |
| **OrganizationAdmin** | Solo su `OrganizationId`: actividades, POIs, ranking de sus eventos. |

**Auth**

- Cuentas `AdminUser` en la **Api** (hash de contraseña). Cookie en el host Admin (claims: rol + org + `access_token`).
- **JWT** emitido en `POST /api/admin-auth/login`; el Admin lo reenvía como `Authorization: Bearer` a la Api.
- Rutas de gestión (orgs, admin-users, POIs CRUD Admin, escrituras de actividades, live, clear-players) requieren JWT. Rutas MAUI (join, capture, ranking, upsert user…) siguen anónimas.
- **Bootstrap SuperAdmin**: user-secrets / parámetros Aspire se siembran en DB al arrancar si no hay SuperAdmin.
- Los admins de empresa los crea el SuperAdmin (usuario + contraseña + organización); **no** van en appsettings.
- Firma JWT: `Jwt:SigningKey` (mín. 32 chars). En Development hay fallback; en Azure parámetro `jwt-signing-key`.

**Rechazado (por ahora)**

- OAuth / Entra ID.
- Permisos finos por pantalla más allá de rol + org.
- JWT / auth para el jugador móvil.

## D19 — Modo de ruta: secuencial vs libre

`Activity.RouteMode`:

| Modo | Jugabilidad | Ranking / premios |
|------|-------------|-------------------|
| **Sequential** (default) | Siguiente POI = primer no capturado por `Order`. | Meta = captura del último `Order` (equiv. completar todos si el cliente respeta la ruta). |
| **Free** | Objetivo = POI pendiente más cercano (fase app). | Meta = instante en que se completan **todos** los POIs (`max CapturedAt`), sin importar el orden. |

Fase 1 (esta): dominio + API + Admin. Fase 2: HUD elige por cercanía. Fase 3: copy / live pulidos.

**Schema** (se sustituye por migraciones de EF Core en la fase 12, paso 2, D20): sin migraciones EF; con `EnsureCreated`, si el modelo no cuadra (p. ej. falta columna) se **borra y recrea** la BD en el seeder. Preferible a parches `ALTER` en este side project.

## D20 — Infraestructura a 0 €: Static Web Apps Free + Functions + Supabase

**Contexto:** el despliegue de D18 (Container Apps) cuesta dinero aunque no se use: el registro de imágenes se
cobra por día. Se quiere poder dejar la app siempre desplegada sin coste. Límites comprobados el 2026-10-28 en la
documentación oficial.

**Decisión**

- Panel en **Blazor WebAssembly**, servido por **Azure Static Web Apps Free** (sustituye a D9 en el paso 4).
- Api como **Functions gestionadas** de esa Static Web App, **.NET 10 isolated** (`dotnet-isolated:10.0`). El
  proyecto `GincanaHud.Api` se convierte en proyecto de Functions: dominio, handlers, mediador y ErrorOr no cambian;
  cada endpoint Minimal API pasa a ser una función HTTP. JWT validado en un middleware propio.
- Postgres sigue en **Supabase Free**. El esquema pasa a **migraciones de EF Core** (ya se usa EF; en
  AgendaMonitores se eligió SQL a mano porque usa Dapper). Sustituye a la nota de esquema de D19.
  - En Development la Api aplica las migraciones y siembra la demo al arrancar; una BD de Docker anterior sin
    historial de migraciones se borra y se recrea.
  - Fuera de Development la Api no toca el esquema ni crea la demo: las migraciones las aplica el despliegue.
    Al arrancar solo asegura el SuperAdmin de la configuración (una consulta y una verificación de contraseña,
    ~0,1 s frente a 2–5 s de arranque en frío; no compensa sacarlo a otro proceso).
- El token del panel se guarda en el navegador y dura lo mismo que ahora (12 h).
- Búsqueda de lugares a través de una función intermediaria con caché (la política de Nominatim lo pide).
- La salud de la Api pasa a `/api/health`: en Static Web Apps, `/health` lo contesta la web estática.
- En local, Aspire lanza las Functions con Azure Functions Core Tools.
- Al terminar (paso 5) se retira el despliegue de D18.

**Límites que hay que vigilar**

- Static Web Apps Free: 100 GB/mes de tráfico, sin pago por exceso (deja de servir); 250 MB por app.
- Supabase Free: 500 MB y 2 proyectos activos (GincanaHud y AgendaMonitores ya los ocupan). Pausa tras una semana
  sin uso, cubierta por el keepalive.
- Arranque en frío de unos segundos tras un rato sin uso: el móvil ya reintenta y tiene cola sin conexión; el panel
  despierta la Api al abrir el directo.

**Alternativas descartadas**

- Container Apps sin registro de pago (imágenes en GitHub Container Registry): gratis solo dentro de una cuota, con
  tarjeta y riesgo de cobro.
- App Service Free (F1) con el código tal cual: 60 min de CPU al día para Api + panel; un evento de 100 jugadores
  durante 4 horas se acerca al límite y, si se agota, la web se para hasta el día siguiente. Además, 5 conexiones
  de Blazor Server como mucho y sin dominio propio.
- Panel en Static Web Apps + Api en App Service F1: mismo riesgo de CPU y dos despliegues.
