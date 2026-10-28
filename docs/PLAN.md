# Plan

**Fase actual: 12**

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

- **Paso 1 — Análisis (sin código).** Qué supone pasar el panel a Blazor WebAssembly y la Api a Functions de
  Static Web Apps, pieza a pieza: login (cookie → token en el navegador), ranking en directo, búsqueda de lugares,
  QR, arranque en frío, límites del plan gratuito, y si alguna alternativa sale más barata en esfuerzo.
  Termina con una decisión en `DECISIONS.md` y los pasos siguientes escritos aquí.
- Pasos siguientes: se definen al cerrar el análisis.

**Juez:** con la app desplegada y un evento de prueba completo (unirse, capturar, ranking) jugado desde el móvil
contra la nube, la factura de Azure de ese mes es 0 €.
