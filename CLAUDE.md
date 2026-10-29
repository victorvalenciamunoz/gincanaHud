# GincanaHud

Gincanas GPS de un día (fiestas, empresas de aire libre). El jugador se une con un código o un QR y el móvil le
guía hasta cada punto (POI) con un HUD sobre la cámara; al llegar, el servidor valida la captura. El organizador
prepara la actividad desde un panel web, con un mapa, y sigue el ranking para dar los premios.

Lee `docs/VISION.md` (el porqué y los requisitos), `docs/DOMAIN.md` (modelo), `docs/ARCHITECTURE.md` (piezas) y
`docs/PLAN.md` (fases). El estado está en `docs/STATUS.md`; las decisiones, en `docs/DECISIONS.md`.

@~/.claude/tipos/maui.md
@~/.claude/tipos/api.md
@~/.claude/tipos/blazor.md

## Principios

- **El servidor decide la captura** (D4). El HUD solo orienta; las pistas no llegan al jugador hasta capturar.
- **Coste de infraestructura: 0 €.** Solo planes gratuitos; si algo requiere pagar, se para y se pregunta.
- **Datos personales mínimos.** Del jugador, nombre y, si hay premio, un contacto (D16). Nada más.
- **La gestión va en el panel, no en el HUD.** El móvil es para jugar en la calle.
- **Sin ARKit/ARCore.** La marca del POI se pinta con GPS + brújula (D5).
- **Repo público.** Ni sitios reales ni coordenadas en ejemplos, seeds, tests o capturas: etiquetas neutras.

## Stack

- `GincanaHud.App`: MAUI, Android, **.NET 11 Preview** (D1). MVVM + XAML C# Expressions (D6). No está en la
  solución: se compila aparte.
- `GincanaHud.Api`: Azure Functions .NET 10 isolated con integración ASP.NET Core y JWT en un middleware (D20,
  D21). Ninguna ruta puede empezar por `admin`. Escalón 3 de `api.md` (dominio): D13, D14.
- `GincanaHud.Admin`: Blazor WebAssembly (D20, D21), cliente HTTP de la Api. Login con el JWT de la Api guardado en
  `localStorage` (12 h).
- `GincanaHud.Shared`: DTOs y geodesia. `GincanaHud.AppHost`: Aspire en local.
- Postgres: en local, contenedor de Aspire; en la nube, Supabase Free (D18).

## Arranque local

```bash
# Una vez: credenciales del SuperAdmin inicial
dotnet user-secrets set "Parameters:admin-username" "admin" --project src/GincanaHud.AppHost
dotnet user-secrets set "Parameters:admin-password" "ELIGE_UNA" --project src/GincanaHud.AppHost

dotnet run --project src/GincanaHud.AppHost
```

- Hace falta Docker Desktop y Azure Functions Core Tools (`func`) en el PATH. Un solo AppHost a la vez (puertos
  `17017` / `22198`).
- F5 desde el IDE suele fallar con `Executable run request: 500` (D12): mejor `dotnet run`.
- Los admins de empresa se crean desde el panel (SuperAdmin → Admins empresa), no en secretos.

## Repo

`victorvalenciamunoz/gincanaHud`, público. No tiene CI de build ni `main` protegida: aun así, cada paso en una rama
y por pull request.
