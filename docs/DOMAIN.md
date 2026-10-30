# Domain

Ubiquitous language for GincanaHud. Architecture/DDD: `docs/ARCHITECTURE.md` (D13/D14). Product events: **D16**.

## Aggregates

| Root | Contiene | Invariantes clave |
|------|----------|-------------------|
| **Organization** | Activities (vía FK) | Nombre no vacío. |
| **AdminUser** | — | Username único; SuperAdmin (sin org) u OrganizationAdmin (con org). |
| **Activity** | **ActivityPoi**, **ActivityParticipant** | Título; `JoinCode` único; `StartsAt` &lt; `EndsAt`; `Order` único por actividad. |
| **Poi** | — (catálogo) | Coordenadas válidas; radio &gt; 0. |
| **User** | — | `DisplayName`; contacto opcional (premios). Un jugador por cada unión, no por nombre (D25). |
| **Capture** | — | Única por (User, Activity, Poi); participante; ventana de juego; radio. |

## Value objects

- `DisplayName`, `ActivityTitle`, `Clue`, `GeoCoordinate`, `RadiusMeters`, `Points`, **`JoinCode`**

## Relaciones

```
Organization ──< Activity ──< ActivityPoi >── Poi
                    │
                    ├──< ActivityParticipant >── User
                    └──< Capture >── User, Poi
```

## Ventana temporal (Activity)

| Momento | Join | Capture |
|---------|------|---------|
| Antes de `StartsAt` | Sí (si activa) | No |
| Entre `StartsAt` y `EndsAt` | Sí | Sí |
| Después de `EndsAt` | No | No |

## API (contratos D16)

- `GET|POST /api/organizations`
- `GET|POST|PUT /api/activities` (panel; la lista, solo de la organización del admin), `GET /api/activities/{id}?userId=`
- `POST /api/activities/join` — código + nombre (+ contacto, + id guardado en el móvil para reincorporarse); nombre ya en la partida → 409; la respuesta no devuelve contacto (D24, D25)
- `GET /api/activities/by-code/{code}` — vista previa
- `POST|PUT|DELETE /api/activities/{id}/pois`
- `POST /api/activities/{id}/capture`
- `GET /api/activities/{id}/ranking?userId=` — anónimo, para la app: sin contactos y solo con el id de quien pregunta (D24, D25)
- `GET /api/activities/{id}/ranking/contacts` — panel: con contacto para premios, solo su organización
- `GET|POST /api/users` (panel)
- `GET|POST /api/pois`

## Flujo jugador (evento)

1. Escanea QR o teclea `JoinCode`.
2. Nombre (+ email/teléfono si hay premio).
3. `join` → User + ActivityParticipant.
4. LOCK → `capture` (solo si la actividad está en ventana de juego).
5. Ranking para entregar premios.
