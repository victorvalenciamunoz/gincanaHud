# Plan

**Fase actual: 14** (la 12 solo espera la factura de octubre de Azure)

---

## Fases cerradas (1–11)

Detalle en `docs/historia.md`.

1. Api + Postgres.
2. Sensores en MAUI (GPS, brújula).
3. Cámara + HUD.
4. App conectada a la Api y prueba en la calle (MVP técnico).
5. Dominio: usuarios, POIs, actividades, capturas y ranking.
6. Panel Admin en Blazor: CRUD, mapa para colocar POIs, ranking.
7. Api con dominio, CQRS ligero y ErrorOr (D13, D14).
8. Login del panel (D15).
9. Evento de un día: organización, código de unión, caducidad (D16).
10. SuperAdmin y admins de organización, con JWT entre panel y Api (D17).
11. Unirse desde la app (código o QR) y empezar por el primer POI pendiente; juego sin cobertura.

## Fase 12 — Infraestructura a 0 €

Hoy el despliegue en Azure Container Apps cuesta dinero aunque no se use: el registro de imágenes se cobra por día.
Se quiere un despliegue que se pueda dejar siempre puesto sin coste, como AgendaMonitores (Static Web Apps Free +
Supabase Free + GitHub Actions).

- **Paso 1 — Análisis (sin código).** Hecho: decisión D20.
- **Paso 2 — Esquema y seeder.** Tablas con migraciones de EF Core. En local la Api las aplica y crea la demo al
  arrancar; en la nube no toca el esquema ni crea la demo, solo asegura el SuperAdmin (D20).
  *Bueno cuando:* el AppHost arranca como hoy y la app móvil juega contra él.
- **Paso 3 — Api → Functions.** `GincanaHud.Api` pasa a proyecto de Functions .NET 10 isolated; los 28 endpoints,
  a funciones; JWT en un middleware; salud en `/api/health`; Aspire lanza las Functions en local.
  Antes de empezar, se propone cómo partirlo.
  *Bueno cuando:* la app móvil se une, captura y ve el ranking contra las Functions en local, y el panel actual
  sigue funcionando contra ellas.
- **Paso 4 — Panel → WebAssembly.** Login con token en el navegador (12 h), fuera los usos de `HttpContext`,
  función intermediaria para Nominatim. Antes de empezar, se propone cómo partirlo.
  *Bueno cuando:* en local, login con los dos roles, crear actividad, colocar POIs, ver el QR y seguir el directo.
- **Paso 5 — Despliegue.** GitHub Action a Static Web Apps Free que aplica también las migraciones en Supabase;
  la app MAUI apunta a la URL nueva; se retira lo de Container Apps (D18).
  *Bueno cuando:* se cumple el juez de la fase.

**Juez:** con la app desplegada y un evento de prueba completo (unirse, capturar, ranking) jugado desde el móvil
contra la nube, la factura de Azure de ese mes es 0 €.

## Fase 13 — Tests y CI

No hay ningún test: cada cambio se valida a mano, y la fase 12 encontró fallos (fechas con hora local, rutas
`admin`, cabeceras de Static Web Apps) que un test habría visto antes o que conviene no volver a romper. Se quiere
poder cambiar el código sin miedo y que una PR en rojo no se pueda fusionar.

- **Paso a — Dominio y CI.** Proyecto de tests (xUnit v3) con las reglas del dominio y la geodesia; workflow que
  compila (avisos como errores) y pasa los tests en cada PR; `main` exige el CI en verde.
  *Bueno cuando:* el CI está en verde en la PR, y un test roto a propósito lo pone en rojo y bloquea la fusión.
- **Paso b — Handlers contra Postgres.** Tests de los casos de uso contra un Postgres real (Testcontainers):
  unirse, capturar (lejos, cerca, orden secuencial, fuera de horario, pista solo tras capturar) y ranking.
  *Bueno cuando:* corren en local y en el CI.
- **Paso c — Cliente del panel.** Tests de `AdminApiClient`: cada respuesta de la Api llega como `ErrorOr` o como
  excepción según `blazor.md`.
  *Bueno cuando:* corren en el CI.

**Juez:** el CI corre en cada PR con los tres grupos de tests, y `main` no acepta una PR en rojo.

## Fase 14 — Pulido final

El proyecto casi cumple la visión, pero un repaso rápido (2026-10-30) encontró un requisito sin cumplir: el admin
de organización no puede limpiar los jugadores de su actividad; solo el SuperAdmin, y borra los de todas. Y un
riesgo: unirse reconoce al jugador por el nombre en toda la base de datos, así que dos «Ana» en la misma fiesta
serían un solo jugador. Además, el cambio del orden secuencial (fase 13) no está desplegado. Se quiere poder dar el
proyecto por terminado, a falta de ideas nuevas.

- **Paso a — Repaso de la visión (sin código).** Cada requisito de `VISION.md` contra el código y la nube: cumple
  (dónde) o falta (qué). Lo que falte se añade como paso de esta fase o sale de alcance, siempre con el autor.
  *Bueno cuando:* la lista está en `STATUS.md` y el autor ha decidido qué entra.
- **Paso b — Identidad del jugador.** Al empezar, el autor elige el modelo (por ejemplo, un jugador por actividad
  en vez de uno global por nombre); puede pedir una migración.
  *Bueno cuando:* dos jugadores con el mismo nombre en la misma actividad capturan y puntúan por separado (test de
  handler contra Postgres), y la app sigue uniéndose y recuperando su sesión.
- **Paso c — Jugadores por actividad.** El admin de organización ve y limpia, desde el panel, los jugadores de
  una actividad de su organización: capturas, participación y su contacto (datos personales mínimos). Qué queda
  para el SuperAdmin se decide al empezar.
  *Bueno cuando:* tests de que un admin limpia solo lo suyo (otra organización → 403) y el autor lo prueba en el
  panel.
- **Paso d — Despliegue y prueba final.** Workflow `Despliegue` (con migraciones si el paso b las trae), fuera los
  jugadores de prueba de Supabase y una actividad de prueba jugada desde el móvil contra la nube.
  *Bueno cuando:* se cumple el juez.

**Juez:** cada requisito de `VISION.md` cumple o ha salido de alcance con una decisión del autor, y desde el móvil,
contra la nube, una actividad de prueba completa (unirse, capturar en orden, ranking, limpiar jugadores) funciona.
