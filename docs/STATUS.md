# Estado

**Fase:** 14 — Pulido final (la 12 solo espera la factura de octubre)
**Actualizado:** 2026-10-30

Lo cerrado hasta la fase 11 y la fase 13 están en `docs/historia.md`.

## Fase 14 — en marcha

- [x] Paso a — repaso de la visión (2026-10-30), sin código; decisiones del autor en D24.
  - **Cumple:** unirse por código o QR (`ScanPage`); HUD con distancia y dirección; captura validada en el servidor
    por radio y horario (tests de la fase 13); juego sin cobertura (`ActivityRouteCache`, `CaptureQueue`); ruta
    secuencial y libre (`HudViewModel.PickNextPoi`); actividades con horario; POIs en el mapa con búsqueda de
    lugares; directo y ranking con contacto en el panel; organizaciones y admins creados por el SuperAdmin.
  - **Falta:**
    - Sin sesión salen los contactos: `GET /api/activities` (anónima, de todas las organizaciones) da los ids, y
      `GET /api/activities/{id}/ranking` (anónima) trae correo y teléfono. En la nube, el 2026-10-30, una
      actividad y una fila sin contacto: no se ha filtrado nada aún. → paso b.
    - La Api no separa organizaciones: nadie lee `org_id` del token; solo el panel filtra lo que enseña. →
      paso b.
    - Limpiar jugadores por actividad: solo el SuperAdmin, y `ClearPlayersHandler` borra los de todas. → paso d.
    - Dos jugadores con el mismo nombre son uno. → paso c.
    - El QR del panel solo se ve; no se descarga ni se copia el código. → paso d.
    - El aviso a 15 m es solo visual (HUD en ámbar). → paso e.
    - La nube no exige el orden secuencial: último despliegue el 2026-10-29, antes de la PR #13. → paso b
      (despliegue).
  - **Contradicción resuelta:** `CLAUDE.md` decía que las pistas no llegan hasta capturar, y el HUD enseña la del
    POI al que vas. El autor decide que una pista es una ayuda para encontrarlo: se corrige el principio (D24).
- [ ] Paso b — privacidad y permisos, partido en b1 y b2.
  - [x] b1 — contactos sin sesión (2026-10-30). `GET /api/activities` y `POST /api/users` piden sesión de admin, y
    la lista solo trae las actividades de la organización del admin (`AdminScope`; el SuperAdmin, todas). El
    ranking anónimo sale sin correo ni teléfono; el panel usa `GET /api/activities/{id}/ranking/contacts`, con
    sesión y solo para su organización (otra → «no encontrada»). La respuesta de unirse no lleva contacto.
    `GetRankingHandler` reescrito sin las líneas en blanco dobles. `FunctionAuthorizationTests` falla si una
    función nueva queda anónima sin estar en la lista.
    Validado: en local, 0 avisos y 83 tests en verde (10 nuevos: ranking sin contactos, con contactos para su
    organización, otra organización, lista por organización y para el SuperAdmin, unirse sin el contacto
    guardado, funciones anónimas y `AdminScope`). Nube: pendiente del despliegue.
  - [ ] b2 — la Api separa organizaciones en el resto de endpoints del panel (directo, jugadores, POIs, editar).
- [ ] Paso c — identidad del jugador.
- [ ] Paso d — panel: jugadores por actividad y QR.
- [ ] Paso e — vibrar al acercarse.
- [ ] Paso f — despliegue y prueba final.

## Fase 12 — cerrada salvo la factura

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
  - [x] 5b (2026-10-29): Static Web App `gincanahud` (plan **Free**, grupo `rg-gincanahud`, West Europe) en
    https://salmon-stone-0fd14c103.2.azurestaticapps.net. El workflow `Despliegue` publica el panel y las Functions
    tras las migraciones, sin compilar en Static Web Apps, y comprueba `/api/health` (D22). Ajustes de las
    Functions en el Static Web App: `ConnectionStrings__gincanahud` (Session pooler), `Jwt__SigningKey`,
    `AdminBootstrap__Username` y `AdminBootstrap__Password` (distinta de la local). Secreto nuevo en GitHub:
    `AZURE_STATIC_WEB_APPS_API_TOKEN`.
    Problemas de la nube resueltos:
    - Static Web Apps no pasa `Authorization` a las Functions gestionadas: el panel manda el token en
      `X-Gincana-Token` (la Api acepta las dos).
    - OpenStreetMap bloqueaba las teselas: Static Web Apps sirve con `Referrer-Policy: same-origin`; ahora
      `strict-origin-when-cross-origin` en `staticwebapp.config.json`.
    - Si el sembrado del SuperAdmin falla al arrancar en la nube, las Functions arrancan igual y `/api/health`
      contesta 503 con el tipo de error (así se vio un `28P01`, contraseña de Supabase incorrecta).
    - La contraseña de la base de datos de Supabase se cambió (2026-10-29) en Supabase, en el Static Web App y en
      el secreto `SUPABASE_DB_URL`; keepalive y migraciones en verde después.
    Validado: despliegue completo en verde; desde fuera, panel en `/` y `/activities`, `/api/health` 200,
    `/api/organizations` sin token → 401, código inexistente → 404 con ProblemDetails. El autor en la nube: login
    de SuperAdmin, organización, actividad y POIs con el mapa.
  - [x] 5c (2026-10-29): la app MAUI apunta a la nube (`ApiOptions.BaseUrl` en `MauiProgram.cs`).
    `BufferedRequestContentHandler` manda los cuerpos con `Content-Length`: Static Web Apps contesta «Backend call
    failure» (500) a los cuerpos por trozos de `PostAsJsonAsync` (D22). Retirado lo de Container Apps (D18): el
    AppHost es solo local, sin `Aspire.Hosting.Azure.AppContainers`; README y ARCHITECTURE al día.
    Validado (juez, parte práctica): desde el móvil, contra la nube, el autor se unió a una actividad por código,
    capturó un POI y sale en el ranking de la nube con 100 puntos.
  - [ ] Juez, parte de la factura: la de octubre de 2026 en Azure tiene que ser 0 €. El 2026-10-29 Azure aún no
    mostraba consumo (llega con uno o dos días de retraso). Mirar a principios de noviembre.
- El keepalive diario pasa en verde (último: 2026-10-29) con los tres secretos configurados.
- En Supabase quedan usuarios de prueba creados con curl («Prueba nube», «Prueba nube 2», «Prueba chunked»,
  «Prueba Aspire 13.6»).
- Aspire 13.6.4 (2026-10-29): sin paquetes vulnerables (MessagePack 2.5.307); `ASPIRE010` silenciado en el AppHost.
  Probado en local (arranque, salud, unirse a `DEMO01`, panel) y en la nube (despliegue, salud, unirse).

## Problemas conocidos

| Tema | Nota |
|------|------|
| Crash Android `jumpToEnd` | Bug de IDs tras Fast Deployment (.NET 11). `EmbedAssembliesIntoApk=true`. Si vuelve: desinstalar la app y redesplegar. |
| Tests | Dominio, geodesia, handlers contra Postgres (Docker en marcha) y cliente del panel; solo los rápidos: `dotnet test -- --filter-not-namespace GincanaHud.Tests.Handlers`. Workflows: `CI`, keepalive diario y `Despliegue` (a mano). |
| Jugador por nombre | Unirse reconoce al jugador por el nombre en toda la base de datos: dos personas con el mismo nombre en una actividad serían el mismo jugador y compartirían capturas. Por decidir con el autor. |
| Toolchain MAUI | El SDK 11 preview 6 de `global.json` ya no está instalado; `rollForward` coge el 11 RC3. Workload `maui-android` RC2 instalado el 2026-10-29: la app compila con los paquetes de MAUI preview 6. |
| Functions en local | Hace falta Azure Functions Core Tools (`winget install Microsoft.Azure.FunctionsCoreTools`, 4.15.2) en el PATH del proceso que lanza el AppHost. |

## Aplazado

- OAuth / Entra ID en el panel.
- Enlace mágico o JWT para el jugador (el móvil sigue anónimo).
- Una BD por organización.
- Key Vault (depende de la decisión de la fase 12).
- Posición falsa (valorado el 2026-10-30). Con una app de ubicación simulada con joystick se puede «llegar» a cada
  POI siguiendo el propio HUD, sin conocer las coordenadas. Y quien sepa mirar una API las tiene sin sesión:
  `by-code` da el id y el detalle de la actividad trae los POIs. Probabilidad baja (en fiestas y empresas, casi
  nula; plausible con adolescentes y un premio que importe), y el daño es un premio mal dado. Si algún día hace
  falta:
  1. Velocidad entre capturas en el servidor (más de unos 20 km/h a pie no es creíble): marcar al jugador en el
     ranking del panel, sin rechazar, porque el GPS da saltos. Cubre cualquier forma de falsear la posición.
  2. Después, si hiciera falta: mandar `Location.IsFromMockProvider` (MAUI) en la captura y rechazarla.
  Descartados: Play Integrity (proyecto de Google Cloud y clave, demasiado para el riesgo) y un QR en cada POI
  (cambia el juego).
