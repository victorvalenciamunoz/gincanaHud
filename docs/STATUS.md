# Estado

**Fase:** 12 — Infraestructura a 0 €
**Actualizado:** 2026-10-29

Lo cerrado hasta la fase 11 está en `docs/historia.md`.

## Fase 12 — en marcha

- [x] Paso 0 — documentación base (2026-10-28): `CLAUDE.md` sin `AGENTS.md` ni reglas de Cursor, `VISION.md`,
  `PLAN.md`, `historia.md`; D18 duplicada pasa a D19. Validado con `dotnet build` (no hay tests).
- [x] Paso 1 — análisis (2026-10-28): decisión D20 (Static Web Apps Free + Functions .NET 10 + Supabase).
  Límites comprobados en la documentación oficial; pasos 2–5 en `PLAN.md`.
- [x] Paso 2 — esquema con migraciones (2026-10-28): migración `InitialCreate` en `Api/Data/Migrations`.
  `DbSeeder`: en Development aplica migraciones (una BD de Docker sin historial se borra y recrea), SuperAdmin y
  demo; fuera de Development solo el SuperAdmin.
  Validado en local, sin el móvil:
  - Development: el volumen antiguo se recrea; segundo arranque sin cambios; unirse a `DEMO01` → 200.
  - Production contra una BD vacía: la Api no crea tablas y falla al arrancar. Tras `dotnet ef database update`,
    arranca, el login del SuperAdmin → 200 y no hay actividad de demo.
- [x] Paso 3 — Api → Functions (2026-10-29), partido en 3a (endpoints anónimos del móvil) y 3b (JWT y endpoints del
  panel).
  - [x] 3a (2026-10-29): `GincanaHud.Api` es un proyecto de Functions .NET 10 isolated con integración ASP.NET Core
    (D21). Funciones en `ActivitiesFunctions` (lista, unirse, por código, detalle, participantes, captura, ranking),
    `UsersFunctions` (alta de jugador) y `HealthFunction` (`/api/health`, también en `ApiHealthMonitor` de la app).
    Aspire las lanza con `AddAzureFunctionsProject` en el puerto 5263 (con Azurite). Sin OpenAPI: se va también el
    aviso de `Microsoft.OpenApi`.
    Validado en local:
    - HTTP contra las Functions: salud 200; por código, lista, unirse, detalle, participantes (204) y alta (201);
      captura lejos rechazada y cerca aceptada con pista y puntos; ranking; cuerpo roto → 400; código inexistente →
      404 con ProblemDetails como antes. La demo no trae POIs: se metió uno de prueba a mano en la BD local.
    - Emulador: la app se une a `DEMO01` y el HUD carga el POI. La captura no salta porque la brújula simulada no
      mueve el HUD.
  - [x] 3b (2026-10-29): todos los endpoints son funciones; ya no hay Minimal API. `JwtAuthorizationMiddleware`
    aplica el `[Authorize(Policy = …)]` de cada función o clase con el esquema y las políticas de siempre (sin token
    → 401, sin rol → 403). Funciones nuevas en `ActivitiesFunctions` (crear, editar, POIs de la actividad, directo),
    `UsersFunctions` (lista, vaciar jugadores), `OrganizationsFunctions`, `PoisFunctions` y `AdminAuthFunctions`.
    - Rutas del panel renombradas: `admin-auth/login` → `panel-auth/login` y `admin-users` → `panel-users`.
      Functions reserva el prefijo `admin` y descartaba esas funciones sin avisar (D21).
    - Arreglo: `Activity` y `Capture` guardan las fechas en UTC. El panel manda la hora local y Npgsql la
      rechazaba: crear un evento daba 500 en local (en Azure no se notaba porque el servidor está en UTC).
    - El panel enseña el motivo que manda la Api (por ejemplo, la contraseña corta), no solo el código de estado.
    Validado en local:
    - HTTP: SuperAdmin → 200 en todo; admin de empresa → 403 en organizaciones, admins y vaciar jugadores, y
      200/201/204 al crear y editar actividades y POIs y en el directo; sin token o con token falso → 401; las rutas
      del móvil siguen anónimas.
    - El autor en el panel: login de SuperAdmin, crear admin de empresa (con el mensaje de contraseña corta) y, con
      él, crear actividades y POIs.
    - Arreglo previo a Functions: un login fallido en el panel daba una excepción (redirigía con `failed=1` y la
      página espera un booleano); ahora redirige con `failed=true` y muestra el aviso.
  - [x] Móvil de verdad (2026-10-29): por USB con `adb reverse tcp:5263 tcp:5263` y la app apuntando a
    `http://127.0.0.1:5263/` solo para la prueba. El autor colocó un POI de `DEMO01` cerca desde el panel, se unió
    desde la app, capturó (100 puntos en la BD) y sale en el ranking de las Functions.
- [x] Paso 4 — Panel → WebAssembly (2026-10-29), partido en 4a (búsqueda de lugares en la Api), 4b (WebAssembly con token en
  `localStorage`) y 4c (cliente HTTP con `ErrorOr`).
  - [x] 4a (2026-10-29): `GET /api/places?q=` (`PlacesFunctions`, solo admins) busca en Nominatim a través de
    `NominatimClient`: User-Agent con la URL del repo, una petición por segundo como mucho, sin reintentos
    automáticos y resultados en caché un día (D21). El panel ya no llama a Nominatim.
    - Arreglo previo: el mapa del panel reencuadraba los POIs en cada repintado y deshacía el salto al lugar
      buscado. Ahora solo reencuadra cuando cambia la lista de POIs.
    Validado en local:
    - HTTP: sin token → 401; primera búsqueda ~300 ms y la misma otra vez de caché en 25 ms; dos búsquedas nuevas
      seguidas tardan más de 1 s; nombres con ñ y tilde bien; búsqueda vacía → lista vacía.
    - El autor en el panel: buscar un lugar con un resultado y con varios, y el mapa va al sitio.
  - [x] 4b (2026-10-29): `GincanaHud.Admin` es Blazor WebAssembly standalone (D21). `AdminSession` guarda en
    `localStorage` la respuesta del login (JWT, rol, organización, caducidad) y hace de `AuthenticationStateProvider`;
    `AdminApiAuthHandler` manda el token y cierra la sesión si la Api contesta 401. Las páginas leen el usuario del
    estado de autenticación en vez de `HttpContext`; `[Authorize]` en `_Imports.razor` sustituye a la
    `FallbackPolicy`. Fuera: login por formulario al servidor, cookie, página de error de servidor y aviso de
    reconexión, y la referencia a ServiceDefaults. En local llama a las Functions en `:5263`, con CORS solo para el
    origen del panel (`--cors` en `launchSettings.json` de la Api).
    Validado en local:
    - Edge sin ventana: `/activities` sin sesión acaba en el login pintado por Blazor; CORS deja pasar al panel y
      no a otro origen.
    - El autor en el panel: login con los dos roles (y fallido), crear actividad, colocar POIs (también buscando un
      lugar), QR, directo, sesión que sobrevive a cerrar la pestaña, «Salir» y vuelta a la página pedida tras el
      login.
  - [x] 4c (2026-10-29): `AdminApiClient` devuelve `ErrorOr<T>` (D21, `blazor.md`): un 4xx que no sea 401 llega
    como error con el código y el motivo de la Api; 401, 5xx y fallos de red siguen siendo excepciones. Las 26
    llamadas de las páginas enseñan `FirstError.Description`.
    Validado en local por el autor: contraseña corta, usuario repetido, actividad inexistente («Actividad no
    encontrada.»), login fallido y un repaso de actividades, POIs, búsqueda y directo.
- [ ] Paso 5 — Despliegue, partido en 5a (migraciones en Supabase), 5b (Static Web Apps Free) y 5c (móvil contra
  la nube, juez y retirar Container Apps).
  - [x] 5a (2026-10-29): workflow `Despliegue` (`deploy.yml`, lanzado a mano), trabajo «Migraciones en Supabase»:
    genera el script idempotente de EF y lo aplica con `psql`. La conexión sale de `SUPABASE_DB_URL` (el secreto del
    keepalive) con `.github/scripts/pg_env.py`, que oculta usuario y contraseña en el log (D22).
    - Supabase empezó de cero: las tablas de la app (creadas sin migraciones, datos de prueba) se borraron una vez
      y las creó `InitialCreate`. `keepalive_ping` no se tocó. El borrado no queda en el workflow.
    Validado: primera ejecución (desde la rama) con las 8 tablas recreadas y `InitialCreate` en el historial;
    segunda ejecución sin cambios; keepalive en verde después.
  - [ ] 5b: Static Web Apps Free con panel y Functions; login y crear actividad en la nube.
  - [ ] 5c: app MAUI a la URL nueva, partida de prueba desde el móvil (juez) y retirar Container Apps.
- Nada desplegado en Azure: el grupo de recursos de la demo (`rg-gincanahud-demo`) está borrado.
- El keepalive diario pasa en verde (último: 2026-10-29) con los tres secretos configurados.

## Problemas conocidos

| Tema | Nota |
|------|------|
| Crash Android `jumpToEnd` | Bug de IDs tras Fast Deployment (.NET 11). `EmbedAssembliesIntoApk=true`. Si vuelve: desinstalar la app y redesplegar. |
| Sin tests ni CI de build | No hay proyecto de tests; el único workflow es el keepalive. |
| Paquetes vulnerables | El build avisa: `MessagePack` 2.5.302 (moderada, viene con Aspire en el AppHost). |
| Toolchain MAUI | El SDK 11 preview 6 de `global.json` ya no está instalado; `rollForward` coge el 11 RC3. Workload `maui-android` RC2 instalado el 2026-10-29: la app compila con los paquetes de MAUI preview 6. |
| Functions en local | Hace falta Azure Functions Core Tools (`winget install Microsoft.Azure.FunctionsCoreTools`, 4.15.2) en el PATH del proceso que lanza el AppHost. |

## Aplazado

- OAuth / Entra ID en el panel.
- Enlace mágico o JWT para el jugador (el móvil sigue anónimo).
- Una BD por organización.
- Key Vault (depende de la decisión de la fase 12).
