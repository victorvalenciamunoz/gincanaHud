# Plan

**Fase actual: 13** (la 12 solo espera la factura de octubre de Azure)

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
