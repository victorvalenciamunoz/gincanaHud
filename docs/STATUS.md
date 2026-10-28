# Estado

**Fase:** 12 — Infraestructura a 0 €
**Actualizado:** 2026-10-28

Lo cerrado hasta la fase 11 está en `docs/historia.md`.

## Fase 12 — en marcha

- [x] Paso 0 — documentación base (2026-10-28): `CLAUDE.md` sin `AGENTS.md` ni reglas de Cursor, `VISION.md`,
  `PLAN.md`, `historia.md`; D18 duplicada pasa a D19. Validado con `dotnet build` (no hay tests).
- [ ] Paso 1 — análisis de la opción B (panel en WebAssembly + Api en Functions de Static Web Apps).
- Nada desplegado en Azure: el grupo de recursos de la demo (`rg-gincanahud-demo`) está borrado.
- Los datos siguen en Supabase. El keepalive diario pasa en verde (último: 2026-10-28) con los tres secretos
  configurados.

## Problemas conocidos

| Tema | Nota |
|------|------|
| Esquema | Sin migraciones: si faltan tablas, el seeder recrea la BD (solo Development). |
| Crash Android `jumpToEnd` | Bug de IDs tras Fast Deployment (.NET 11). `EmbedAssembliesIntoApk=true`. Si vuelve: desinstalar la app y redesplegar. |
| Sin tests ni CI de build | No hay proyecto de tests; el único workflow es el keepalive. |
| Paquetes vulnerables | El build avisa: `Microsoft.OpenApi` 2.0.0 (alta, Api) y `MessagePack` 2.5.302 (moderada, viene con Aspire en el AppHost). |

## Aplazado

- OAuth / Entra ID en el panel.
- Enlace mágico o JWT para el jugador (el móvil sigue anónimo).
- Una BD por organización.
- Key Vault (depende de la decisión de la fase 12).
