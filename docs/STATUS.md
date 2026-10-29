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
- [ ] Paso 3 — Api → Functions, partido en 3a (endpoints anónimos del móvil) y 3b (JWT y endpoints del panel).
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
  - [ ] 3b: middleware JWT y endpoints del panel (login, organizaciones, admins, POIs, escrituras, directo). Hasta
    entonces el panel no funciona en esta rama.
  - [ ] Al acabar 3b: capturar y ver el ranking desde el móvil de verdad contra las Functions.

Para el paso 5: las tablas de Supabase se crearon sin migraciones. Hay que comprobar que su esquema coincide con
`InitialCreate` y marcarla como aplicada antes del primer `database update`.
- Nada desplegado en Azure: el grupo de recursos de la demo (`rg-gincanahud-demo`) está borrado.
- Los datos siguen en Supabase. El keepalive diario pasa en verde (último: 2026-10-28) con los tres secretos
  configurados.

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
