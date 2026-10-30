# Respuestas de la Api para los tests del panel

- `activity-not-found.404.json`: grabada de la nube el 2026-10-30 (`GET /api/activities/by-code/ZZZ0001`, anónima).
- `username-taken.409.json` y `activities.200.json`: escritas a mano con la misma forma; no se pueden provocar sin
  sesión o traerían datos reales.
- Sin fichero, porque la Api no manda cuerpo (grabado el mismo día): `GET /api/organizations` sin token → 401 y
  `POST /api/activities/{id}/capture` sin cuerpo → 400.
