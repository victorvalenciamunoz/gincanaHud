# Status

**Phase:** 6+ / D16 evento + **D17 roles Admin**  
**Updated:** 2026-08-05

## Done (reciente)

### D17 — SuperAdmin vs OrganizationAdmin
- `AdminUser` en Api (hash); login `POST /api/admin-auth/login`
- Bootstrap SuperAdmin desde Aspire parameters → `AdminBootstrap__*`
- SuperAdmin: organizaciones + crear admins de empresa (`/org-admins`)
- OrganizationAdmin: actividades de su org + **Jugadores** (participantes)
- Cookie Admin con claims rol + org

### D16 — Evento
- Org, JoinCode, caducidad, participantes, contacto premios
- Admin QR; **MAUI Unirse** (código/QR) + sesión; Iniciar carga primer POI sin capturar

## Known issues / ops

| Tema | Nota |
|------|------|
| Schema | seeder recrea si faltan tablas AdminUsers |
| SuperAdmin | user-secrets `Parameters:admin-username` / `admin-password` |
| Join MAUI | **done** — pestaña Unirse + QR + Iniciar carga POI |
| Api auth | scoping Admin en UI; JWT en Api diferido |

## Next

1. Probar AR marker en calle (FOV/pitch finos si hace falta)
2. Estética Unirse / Admin
3. Key Vault Azure

## Explicitly deferred

- JWT en cada endpoint Api  
- OAuth / Entra ID  
- Magic link jugador  
- DB por organización  
