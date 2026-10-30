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
| **Sequential** (default) | Siguiente POI = primer no capturado por `Order`. | Meta = captura del último `Order` (equivale a completar todos: el servidor no deja saltarse ninguno). |
| **Free** | Objetivo = POI pendiente más cercano (fase app). | Meta = instante en que se completan **todos** los POIs (`max CapturedAt`), sin importar el orden. |

**El servidor exige el orden** (fase 13, paso b; decidido por el autor el 2026-10-30): hasta entonces solo lo
respetaba el HUD, y una app modificada podía ir directa a la meta y ganar el ranking. `CapturePoiHandler` rechaza
la captura de un POI si queda alguno anterior sin capturar, con la misma forma que «fuera de rango» (`Success =
false`, sin pista ni puntos, mensaje con el POI pendiente): el HUD lo enseña y la cola sin conexión, que envía por
orden de hora, no lo reintenta. Alternativa descartada: dejarlo en el cliente (contradice D4).

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

## D21 — Cómo pasa la Api a Functions

**Contexto:** paso 3 de la fase 12 (D20). Los endpoints Minimal API pasan a funciones HTTP sin tocar dominio,
handlers ni mediador. Paquetes comprobados en nuget.org el 2026-10-29 (todos MIT y con versiones de 2026).

**Decisión**

- `GincanaHud.Api` usa `Microsoft.NET.Sdk` + `Microsoft.Azure.Functions.Worker` 2.52.0, `.Worker.Sdk` 2.1.0 y
  `.Worker.Extensions.Http.AspNetCore` 2.1.1 (integración ASP.NET Core: las funciones reciben `HttpRequest` y
  devuelven el mismo `IResult` que antes, así que `ToHttpResult` y los ProblemDetails no cambian). Los `using` que
  ponía el SDK web se añaden en el csproj.
- Una clase de funciones por feature (`ActivitiesFunctions`, `UsersFunctions`…) con el `ISender` por constructor.
  Las rutas no llevan `/api`: lo pone Functions, así que las URL no cambian.
- El cuerpo se lee con `ReadFromJsonAsync` (`WithJsonBodyAsync`), que usa las mismas opciones JSON de siempre (enums
  como texto). Cuerpo vacío o roto → 400, como Minimal API. No se usa el `[FromBody]` del worker para no tener dos
  configuraciones de JSON.
- `/api/health` existe en todos los entornos (en la nube el móvil lo usa). Solo comprueba que el proceso responde,
  no expone nada. La app da la Api por viva solo con un 2xx: antes valía cualquier respuesta < 500 porque `/health`
  solo existía en desarrollo.
- En local, Aspire lanza las Functions con `AddAzureFunctionsProject` (`Aspire.Hosting.Azure.Functions` 13.4.6, la
  versión del resto de Aspire), que añade Azurite como almacenamiento del host. El puerto 5263 sale de
  `launchSettings.json` (`--port 5263`). Se quita `PublishAsAzureContainerApp` de la Api: el despliegue de D18 se
  retira en el paso 5.
- Se quita OpenAPI: sin `WebApplication` no hay `MapOpenApi`, y no lo usaba nadie.
- JWT: Functions ignora `[Authorize]`, así que `JwtAuthorizationMiddleware` lo lee de la función o de su clase y lo
  aplica con el esquema JwtBearer y las políticas de `AddGincanaJwtAuth` (sin token o token inválido → 401, sin la
  política → 403). Todos los triggers van con `AuthorizationLevel.Anonymous`: las claves de función no se usan.
  Se usa el `[Authorize]` estándar en vez de un atributo propio para no tener otro tipo.
- Ninguna ruta empieza por `admin`: el host de Functions reserva ese prefijo y descarta la función sin error.
  `admin-auth/login` pasa a `panel-auth/login` y `admin-users` a `panel-users`; solo las usa el panel.
- Búsqueda de lugares (paso 4a): función `GET /api/places?q=`, solo para admins. `NominatimClient` cumple la
  política de Nominatim con un User-Agent que lleva la URL del repo (no un correo), una petición por segundo como
  mucho (en cada instancia) y caché en memoria de un día. Se le quitan los reintentos automáticos de ServiceDefaults
  con `RemoveAllResilienceHandlers` (API experimental, `EXTEXP0001` desactivado solo en esa línea): los reintentos
  se saltarían el límite. Si Nominatim falla, la Api contesta un error con mensaje y el panel lo enseña.
- Panel en WebAssembly (paso 4b): proyecto standalone (`Microsoft.NET.Sdk.BlazorWebAssembly`) con
  `Components.WebAssembly`, `.DevServer` y `Components.Authorization` 10.0.14 (Microsoft, MIT).
  - Sesión: se guarda en `localStorage` la respuesta entera del login (no solo el JWT), así no hay que leer el
    token en el navegador para saber rol y organización. Elegido por el autor frente a `sessionStorage`: la sesión
    sobrevive a cerrar la pestaña durante sus 12 h, como la cookie persistente de antes. Un 401 de la Api la cierra.
  - Todas las páginas llevan `[Authorize]` desde `_Imports.razor` salvo las que dicen `[AllowAnonymous]` (login,
    no encontrado), como la `FallbackPolicy` del servidor. Ocultar menús según el rol es solo comodidad: quien
    decide es la Api.
  - Dirección de la Api: en Static Web Apps es el mismo sitio (`/api`); en desarrollo, `http://localhost:5263/`
    fijo en `Program.cs`. No va en `wwwroot/appsettings.Development.json` porque ese nombre está en el
    `.gitignore` del repo.
  - CORS solo en local y solo para el origen del panel, con `--cors` de Core Tools en el `launchSettings.json` de
    la Api. En Static Web Apps no hace falta.
- Cliente HTTP del panel (paso 4c, regla de `blazor.md`): cada método devuelve `ErrorOr<T>`. Un 4xx que no sea 401
  pasa a `Error` con el `code` y el `detail` del ProblemDetails de la Api (404 → `NotFound`, 409 → `Conflict`,
  403 → `Forbidden`, el resto → `Validation`); si no hay cuerpo, un texto genérico. 401, 5xx y fallos de red siguen
  siendo excepciones: el 401 ya cierra la sesión en `AdminApiAuthHandler` y un 5xx no es una regla de negocio. Se
  hizo en un paso aparte del 4b para no mezclar el cambio de plataforma con el de los errores.

**Alternativas descartadas**

- Lanzar `func start` con `AddExecutable`: sin paquete ni Azurite, pero con el puerto y las conexiones cableados a
  mano (elegido por el autor).
- Un proyecto de Functions nuevo al lado de la Api: dos proyectos para lo mismo; D20 ya decía convertir el actual.

## D22 — Despliegue con una GitHub Action

**Contexto:** paso 5 de la fase 12 (D20). Las migraciones las aplica el despliegue, no la Api al arrancar. Las
tablas de Supabase se habían creado sin migraciones y sus datos eran de prueba (2026-10-29).

**Decisión**

- Un workflow, `Despliegue` (`.github/workflows/deploy.yml`), lanzado a mano (`workflow_dispatch`).
- Migraciones: EF genera un script idempotente (`migrations script --idempotent`, sin conectarse) y `psql` lo
  aplica en una transacción por migración. Volver a lanzarlo no cambia nada.
- La conexión es el secreto `SUPABASE_DB_URL` del keepalive (session pooler), en URI o en formato Npgsql.
  `.github/scripts/pg_env.py` reutiliza el parser del keepalive y oculta usuario y contraseña en los logs, que en
  un repo público son públicos.
- Supabase empezó de cero (elegido por el autor): se borraron una vez las tablas de la app y se crearon con
  `InitialCreate`. `keepalive_ping` es del keepalive y no la toca nadie más.

- Static Web App `gincanahud` con plan Free en West Europe (paso 5b). Sin aviso de presupuesto (elegido por el
  autor): el plan Free no cobra por exceso, deja de servir.
- La Action publica (`dotnet publish`) el panel y las Functions y los sube con `Azure/static-web-apps-deploy`
  con `skip_app_build` y `skip_api_build`: no depende del SDK que tenga la imagen de Static Web Apps. GitHub avisa
  de que `skip_api_build` no está en el `action.yml`, pero la acción lo respeta. El trimmer del panel conserva
  `GincanaHud.Shared` entero (los DTOs se leen por reflexión).
- Los ajustes de las Functions viven en el Static Web App, no en GitHub. La cadena de Supabase y la contraseña del
  SuperAdmin las pone el autor en el portal: así no pasan por la conversación ni por los logs.
- El token del panel va en la cabecera `X-Gincana-Token` (`ApiHeaders.PanelToken` en Shared): Static Web Apps usa
  `Authorization` para su propia autenticación y no lo pasa a las Functions gestionadas. La Api acepta las dos.
- `Referrer-Policy: strict-origin-when-cross-origin` en `staticwebapp.config.json`: con el `same-origin` que pone
  Static Web Apps, el navegador no manda `Referer` y OpenStreetMap bloquea las teselas.
- Fuera de Development, si el sembrado falla al arrancar, las Functions arrancan igual y `/api/health` contesta
  503 con el tipo de error y el código de Postgres (no el mensaje, que puede llevar el usuario). Sin Application
  Insights no hay otra forma de ver por qué no arranca.
- La app MAUI manda los cuerpos con `Content-Length` (`BufferedRequestContentHandler`): Static Web Apps contesta
  «Backend call failure» (500) a los cuerpos por trozos (`Transfer-Encoding: chunked`) dirigidos a sus Functions
  gestionadas. El panel no lo necesita: el navegador siempre manda el cuerpo entero.
- Se retira el despliegue de D18: el AppHost es solo para local.

**Alternativas descartadas**

- `dotnet ef database update` en la Action: necesita la cadena en formato Npgsql y el secreto puede ser una URI.
- Application Insights para ver los errores de las Functions: es de pago fuera de su cuota gratuita.
- Funciones propias enlazadas («bring your own functions»): exigen el plan Standard, que es de pago.
- Conservar las tablas y marcar `InitialCreate` como aplicada: había que comparar el esquema columna a columna para
  conservar datos de prueba.

## D23 — Tests y CI

**Contexto:** fase 13. No había tests. El autor eligió (2026-10-29) una fase propia en tres pasos, Postgres real
para los tests de base de datos y que `main` exija el CI en verde.

**Decisión**

- Un proyecto, `tests/GincanaHud.Tests`, en la solución. xUnit v3 4.0.2, `xunit.runner.visualstudio` 4.0.1 y
  `Microsoft.NET.Test.Sdk` 18.11.0 (Apache 2.0 / MIT, versiones de octubre de 2026). Comprobaciones con los
  `Assert` de xUnit: FluentAssertions es de pago desde la versión 8.
- `global.json` activa Microsoft.Testing.Platform (`"test": { "runner": … }`, elegido por el autor): con el SDK de
  .NET 10, `dotnet test` ya no ejecuta xUnit v3 con VSTest. La versión del SDK no cambia.
- Workflow `CI` en cada PR y en `main`: `dotnet build -warnaserror` y `dotnet test`. La app MAUI no está en la
  solución y no entra. `main` exige el check «Build y tests» en verde para fusionar, también al administrador
  (`enforce_admins`), sin revisiones obligatorias (hay un solo desarrollador).
- Acciones en Node 24 (`actions/checkout@v7`, `actions/setup-dotnet@v6`): las v4 usan Node 20, obsoleto en los
  runners de GitHub.
- Los tests de base de datos (paso b) usan `Testcontainers.PostgreSql` 4.16.0 (MIT, octubre de 2026; sigue
  siendo la última el 2026-10-30): el fallo de fechas de la fase 12 solo aparecía con Postgres de verdad.
  - Un contenedor `postgres:17-alpine` para toda la colección `Postgres` (`PostgresFixture`), con
    `Database.MigrateAsync()`: se prueban las migraciones de verdad. Las clases de la colección corren en serie.
  - Sin limpieza entre tests: cada uno siembra su organización, actividad, POIs y jugadores con ids, códigos y
    nombres nuevos (`TestData`). Los nombres tienen que ser únicos porque unirse busca al jugador por nombre.
  - Un `AppDbContext` nuevo para sembrar, otro para el handler y otro para comprobar, como en peticiones
    distintas: así no se cuela nada de la caché de EF.
  - Los handlers se construyen a mano con el contexto (`new CapturePoiHandler(db)`): el mediador propio solo
    busca el handler en el contenedor de servicios y no añade ningún paso intermedio, así que pasar por él no
    probaría nada más.
  - La siembra crea las actividades con hora +01:00, como el panel, y un test manda una captura sin conexión con
    +01:00: si alguien quita el paso a UTC, Npgsql falla (comprobado quitándolo a propósito).
- Los tests del cliente del panel (paso c) dan a `AdminApiClient` un `HttpClient` con un `HttpMessageHandler`
  falso de pocas líneas: sin paquete de mocks. Las respuestas salen de `Panel/Fixtures`: el 404 grabado de la nube
  con una llamada anónima; el 409 y el 200 escritos a mano con la misma forma (el 409 pide sesión y una lista real
  traería datos reales).
- Un solo proyecto de tests también para el panel: referenciar el proyecto Blazor WebAssembly compila sin avisos.
  Se separaría en dos si los tests de Postgres pasan del minuto, si el CI tiene que tratarlos distinto o si esa
  referencia da problemas.
- En los tests, coordenadas neutras alrededor de (0, 0): el repo es público.

**Alternativas descartadas**

- `xunit.v3.mtp-off` para seguir con VSTest sin tocar `global.json`: el camino que Microsoft va dejando.
- EF InMemory para los handlers: no habría detectado el fallo de las fechas con hora local.

## D24 — Repaso de la visión: qué entra en la fase 14 y qué es una pista

**Contexto:** fase 14, paso a (2026-10-30). Repaso de cada requisito de `VISION.md` contra el código y la nube; la
lista está en `STATUS.md`.

**Decisión** (del autor)

- **Privacidad y permisos, primero** (paso b, se despliega al cerrarlo): la lista de actividades y el ranking
  anónimos dejaban sacar nombre, correo y teléfono de los jugadores, y la Api no comprobaba la organización del
  token, solo el panel filtraba lo que enseña.
- **Una pista es una ayuda para encontrar el POI** al que vas (por ejemplo, «junto a la fuente»), como funciona el
  HUD. Se corrige el principio de `CLAUDE.md`, que decía que no llegaban hasta capturar. El código no cambia: la
  app ya recibe las coordenadas de todos los POIs, que dicen más que cualquier pista, y en ruta libre el servidor
  no sabe a qué POI va el jugador.
- Entran también, como pasos pequeños: descargar el QR y copiar el código en el panel, y vibrar al acercarse.

**Alternativas descartadas**

- Pista como premio al capturar, que lleva al siguiente POI (como en papel): cambia el HUD y el panel y encaja mal
  con la ruta libre.
- Mandar solo la pista del POI objetivo: no protege nada, por lo dicho de las coordenadas.
- Arreglar los permisos dentro del paso de jugadores por actividad: una PR menos, pero tardaría más en llegar a la
  nube.

**Cómo se hizo el paso b1** (técnico, 2026-10-30)

- El middleware solo lee el token en las funciones con `[Authorize]`, así que una función anónima no sabe quién
  llama. Por eso el ranking con contactos es una ruta aparte, `activities/{id}/ranking/contacts`, para admins, y
  el ranking anónimo (el que usa la app al terminar) sale siempre sin correo ni teléfono.
- `AdminScope.OrganizationOf` lee la organización del token: `null` para el SuperAdmin (lo ve todo) y
  `Guid.Empty` para un admin sin organización (no ve nada). Las consultas llevan esa organización y, si la
  actividad es de otra, contestan «no encontrada» en vez de 403: así no se confirma que existe. Es la pieza que
  usará el paso b2.
- `GET /api/activities` y `POST /api/users` pasan a pedir sesión: la app no los usa (su `EnsureUserAsync` quedó
  sin llamadas), y con un nombre ya existente el segundo devolvía el contacto de ese jugador.
- La respuesta de unirse no lleva contacto: el jugador se busca por nombre, así que el guardado puede ser de otra
  persona. La app guarda lo que teclea el jugador, no lo que contesta el servidor.
- Un test recorre todas las funciones y falla si alguna nueva queda anónima sin estar en la lista de las que deben
  serlo (`FunctionAuthorizationTests`).

**Cómo se hizo el paso b2** (técnico, 2026-10-30)

- Cada orden del panel que toca una actividad lleva `OrganizationScope` (la organización del token, o `null` para
  el SuperAdmin), y el handler la compara con la de la actividad (`Excludes`). Se comprueba en el handler, no en
  la función, para que lo cubran los tests contra Postgres.
- Crear una actividad o un POI para otra organización da 403: la organización la manda el panel en el cuerpo y no
  hay nada que ocultar. Leer o cambiar algo de otra da «no encontrada», como en b1.
- En las listas de POIs y jugadores, la organización del token sustituye a la que pide el panel
  (`AdminScope.Restrict`): un admin no puede pedir la de otro.

## D25 — Identidad del jugador: uno nuevo en cada unión

**Contexto:** fase 14, paso c (2026-10-30). Unirse buscaba al jugador por su nombre en toda la base de datos: dos
«Ana» en la misma fiesta eran el mismo jugador, y quien tecleara un nombre ajeno se quedaba con su progreso, su id
y le sobrescribía el contacto.

**Decisión** (el modelo y el nombre repetido, del autor)

- Unirse crea siempre un jugador nuevo. El móvil manda el id que guarda (`JoinActivityRequest.UserId`) y el
  servidor lo reutiliza solo si ese jugador ya está en esa actividad: así se reincorpora con su progreso. Saber un
  nombre ya no da nada.
- Un nombre que ya está en la actividad (sin distinguir mayúsculas) se rechaza con 409 y el aviso «Ese nombre ya
  está en la partida: añade una inicial o un apellido». El ranking se lee sin dudas al dar premios.
- El id pasa a ser la llave del jugador, así que el ranking anónimo solo lleva el de quien pregunta
  (`?userId=`); los demás van vacíos. El panel, con sesión, sigue viéndolos todos.
- La app conserva el id al «Salir de la actividad», para volver a unirse sin chocar con su propio nombre.
- Sin migración: la tabla de jugadores no cambia. El alta de jugadores del panel (`UpsertUser`) sigue buscando
  por nombre: es solo de admins y se revisa en el paso d (jugadores por actividad).
- Dos uniones exactamente a la vez con el mismo nombre podrían colarse las dos: no hay índice único porque el
  nombre vive en el jugador y no en la participación. En un evento de un día es casi imposible; si pasa, el panel
  enseña dos «Ana».

**Alternativas descartadas**

- Nombre único por actividad, buscando por nombre: no hay dos «Ana», pero quien teclea «Ana» sigue quedándose con
  su progreso.
- Permitir nombres repetidos: menos fricción al unirse, peor al dar premios.

## D26 — Limpiar jugadores por actividad

**Contexto:** fase 14, paso d1 (2026-10-30). `VISION.md` pide que el admin vea y limpie los jugadores de su
actividad. Solo existía «Vaciar jugadores», del SuperAdmin, que borraba los de todas las organizaciones, y un alta
manual de jugadores que desde D25 no sirve: cada jugador nace al unirse.

**Decisión** (qué se limpia y qué queda para el SuperAdmin, del autor)

- En el detalle de la actividad, la sección «Jugadores» lista a todos los unidos, también los que no han capturado
  nada, con su contacto, cuándo se unieron y sus capturas. Cada fila tiene «Quitar», y abajo está «Borrar todos los
  jugadores», para después de dar los premios; los dos piden confirmación.
- Quitar a un jugador borra sus capturas en esa actividad, su participación y el propio jugador con su contacto
  (datos mínimos, D16). Un jugador de antes de D25 que siga en otra actividad conserva esa otra.
- Api: `DELETE /api/activities/{id}/players/{userId}` y `DELETE /api/activities/{id}/players`, con sesión de admin
  y la organización del token (otra → «no encontrada»; el SuperAdmin, todas). Todo en una transacción.
- Se quitan «Vaciar jugadores» y el alta manual (`ClearPlayers`, `UpsertUser`): el SuperAdmin limpia por actividad
  como cualquier admin. También el alta de participantes (`POST /api/activities/{id}/participants`), anónima y sin
  uso desde que unirse la hace. Los jugadores de prueba sin actividad se borran a mano en Supabase (paso f).
- La lista sale del directo (`GET /api/activities/{id}/live`), que ya traía a todos los participantes: no hace falta
  otra ruta.

**Alternativas descartadas**

- Solo «Borrar todos»: no deja borrar los datos de una sola persona que lo pida.
- Mantener «Vaciar jugadores» para el SuperAdmin: un botón que vacía la base de datos de la nube entera.
- d2 (la invitación, del autor): junto al QR, «Copiar código» y «Copiar invitación», un texto corto con el título
  y el código para pegar en WhatsApp o en un email. El QR se descarga como PNG de unos 1200 px (40 píxeles por
  módulo), suficiente para un cartel impreso; se genera en el navegador, sin pasar por la Api.

## D27 — Vibrar una vez al acercarse

**Contexto:** fase 14, paso e (2026-10-30). El HUD tenía código para dar toques cada vez más rápidos dentro de los
15 m y una vibración al capturar, pero el manifiesto no declaraba `VIBRATE`: MAUI lanzaba una excepción que el
código se tragaba, y el móvil no vibraba nunca.

**Decisión** (el comportamiento, del autor)

- Una vibración corta (250 ms) al entrar a 15 m de cada POI, y nada más hasta capturar; al capturar, 500 ms. Se
  quitan los toques que se aceleraban.
- Se recuerda el POI que ya vibró: si el GPS baila alrededor de los 15 m no vuelve a vibrar. En ruta libre, al
  cambiar al POI más cercano, el nuevo vibra al entrar en su radio de aviso.
- `Vibration` en vez de `HapticFeedback`: los toques hápticos de Android siguen el ajuste del sistema de vibrar al
  tocar, que muchos móviles traen apagado. El interruptor «Sonido y vibración» de la app sigue apagándolo todo.

**Alternativas descartadas**

- Toques que se aceleran (lo que había): guían sin mirar, pero vibran sin parar mientras se busca y gastan
  batería.
- Una vibración al entrar y después toques: lo mismo, con un aviso más.

## D28 — APK descargable en GitHub Releases, firmado con una clave propia fuera del repo

**Contexto:** fase 14 (2026-10-30). El autor quiere un enlace para que otra persona se descargue la app y la
pruebe, como en VientoHud (su decisión 052). Compilarla exigiría el SDK de .NET 11 Preview y la carga de trabajo
de MAUI.

**Decisión** (del autor)

- Paso g nuevo de la fase 14, después de la prueba final del paso f.
- Igual que VientoHud: `scripts/publish-apk.ps1` compila en Release desde limpio, firma, verifica la firma con
  `apksigner` (falla si no verifica o no la firma `CN=GincanaHud`) y da el SHA-256. Publicación a mano con
  `gh release create`, marcada como *pre-release*.
- Clave propia fuera del repositorio (público), en `%USERPROFILE%\.gincanahud\`, con copia de seguridad del
  autor: sin ella, una versión nueva no se instala encima de la anterior. En el certificado, `CN=GincanaHud`, no
  un nombre personal.
- Versión 0.9.0 (de pruebas, antes de dar el proyecto por terminado). `ApplicationVersion` = mayor×10000 +
  menor×100 + parche (0.9.0 → 900), siempre creciente.
- Con la app sola no se juega: las notas de la versión explican que hace falta el código de una actividad de la
  nube, con los puntos donde vaya a estar quien prueba. Sin nombres de sitios.

**Alternativas descartadas**

- GitHub Actions al crear una etiqueta: necesita la carga de trabajo de MAUI (preview) en el runner y la clave en
  los secretos; merece la pena si se publica a menudo.
- Google Play: cuenta de pago (choca con el coste 0 €) y revisión.
- Firmar con la clave de depuración: es pública y cambia entre máquinas.
