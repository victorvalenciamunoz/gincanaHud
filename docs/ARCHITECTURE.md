# Architecture (API + Admin)

Target shape for **GincanaHud.Api** and guidance for **GincanaHud.Admin**.  
Decisions: **D13** (CQRS/slices), **D14** (DDD táctico). Status: `docs/STATUS.md`. Domain language: `docs/DOMAIN.md`.

## Principles (apply, don't over-build)

| Idea | How we use it |
|------|----------------|
| **YAGNI** | One deployable Api project. No four-project Clean Architecture until pain forces it. |
| **SOLID** | Handlers with one reason to change; domain rules live on aggregates/VOs, not in endpoints. |
| **Clean Code** | Small methods, clear names, no cleverness. |
| **Early return** | Guard clauses first; happy path last; avoid deep nesting. |
| **DDD (tactical)** | Aggregates, entities, value objects, ubiquitous language. No event sourcing / sagas unless needed. |
| **CQRS** | Separate **commands** (write through aggregates) and **queries** (read models / projections). Endpoints only dispatch. |
| **Mediator (in-house)** | Tiny `ISender` + `IRequest<T>` / `IRequestHandler<TRequest,TResponse>` — **not MediatR**. |
| **Result** | Handlers return **`ErrorOr<T>`**. Domain methods prefer `ErrorOr` / explicit failure over throwing for business rules. |

### What we skip (for now)

- Full Clean Architecture multi-project split (unless STATUS upgrades it).
- MediatR / third-party mediators.
- Language-level union types → use **ErrorOr**.
- Event sourcing, domain event bus, outbox (unless a clear need appears).
- Second CQRS/DDD model inside Admin (Admin = Api client).

## DDD — tactical model

### Ubiquitous language

Use product terms from `docs/DOMAIN.md`: **Organization**, **User**, **Poi**, **Activity**, **ActivityPoi**, **ActivityParticipant**, **Capture**, **JoinCode**. Code names match Spanish product docs where practical; English type names OK if consistent (`Activity`, not `GincanaEvent`).

### Aggregates (consistency boundaries)

| Aggregate root | Contains / owns | Invariants (examples) |
|----------------|-----------------|------------------------|
| **Organization** | Activities | Name required. |
| **Activity** | **ActivityPoi**, **ActivityParticipant** | JoinCode unique; StartsAt &lt; EndsAt; unique Order; capture only linked POIs + participants + play window. |
| **Poi** | — (catalog) | Valid geo, radius &gt; 0, non-empty clue/name when required. |
| **User** | — | DisplayName; optional contact for prizes. |
| **Capture** | — (via Activity play rules) | One capture per (User, Activity, Poi); GPS within radius. |

**Notes**

- Prefer modifying an aggregate **through its root** (e.g. `Activity.AddPoi(...)`, `Activity.RegisterCapture(...)`) rather than mutating `ActivityPoi` from handlers ad hoc.
- Cross-aggregate rules (e.g. “user must exist”) are orchestrated in the **command handler**, which loads aggregates and calls domain methods.
- Queries do **not** need full aggregates: project to DTOs with EF `AsNoTracking` (read side).

### Value objects (prefer over raw primitives)

Introduce when a concept has rules or meaning beyond a primitive:

| VO | Wraps | Rules (sketch) |
|----|--------|----------------|
| `DisplayName` | `string` | Trimmed, non-empty, max length. |
| `GeoCoordinate` | lat/lon | Valid ranges; equality by value. |
| `RadiusMeters` | `double` | &gt; 0, sensible max. |
| `Clue` | `string` | Non-empty when published, max length. |
| `Points` | `int` | &gt; 0. |
| `ActivityTitle` | `string` | Non-empty, max length. |

VOs are **immutable**, equality by value, factory/`TryCreate` / `ErrorOr<T>` creation. Do **not** wrap every `int`/`string` (YAGNI).

### Entities vs persistence

- **Domain model** (`Domain/…`): rich types, invariants, no EF attributes required.
- **Infrastructure** (`Infrastructure/Data`): EF `AppDbContext`, configurations (`IEntityTypeConfiguration`), mapping to/from domain **or** gradual approach: start by enriching current EF classes and extracting VOs, then tighten aggregate APIs.
- Avoid anemic domain forever: new write logic goes on the aggregate, not only in handlers.

### Domain errors

- Prefer `ErrorOr` / domain `Error` codes (`Activity.NotFound`, `Capture.OutOfRange`) mapped in HTTP layer.
- Do not use exceptions for expected business failures.

## Folder layout — Api

```
src/GincanaHud.Api/
  Program.cs
  Common/
    Messaging/                  // ISender, dispatcher
    Http/                       // ErrorOr → IResult
  Domain/
    Users/
      User.cs                   // aggregate root
      DisplayName.cs            // VO
    Pois/
      Poi.cs
      GeoCoordinate.cs
      RadiusMeters.cs
      Clue.cs
    Activities/
      Activity.cs               // root; ActivityPoi as entity inside
      ActivityPoi.cs
      ActivityTitle.cs
    Captures/
      Capture.cs
      // capture may be factory on Activity
  Infrastructure/
    Data/
      AppDbContext.cs
      Configurations/           // EF fluent configs
      Persistence/              // optional mappers / repositories
      DbSeeder.cs
  Features/
    Users/
      ListUsers/
      UpsertUser/
    Activities/
      …
    Pois/
      …
```

**Conventions**

- Features = application use cases (CQRS). Domain = rules. Infrastructure = EF/IO.
- Handlers load aggregates (or query read models), call domain, save, return `ErrorOr`.
- Shared wire DTOs stay in `GincanaHud.Shared`.
- Repositories: optional. Start with `AppDbContext` in handlers; extract `IActivityRepository` when duplication hurts.

## Result → HTTP

| ErrorOr | HTTP |
|---------|------|
| Value | 200 / 201 |
| NotFound | 404 |
| Validation | 400 |
| Conflict | 409 |
| Unexpected | 500 (log) |

## Mediator (hand-rolled)

```csharp
public interface IRequest<TResponse>;
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
```

## Admin

- HTTP client of the Api. No parallel domain model in Blazor.
- Pages by product area; map/geocode = Admin infrastructure.

## Migration strategy

1. Docs D13 + D14 + this file — **current**.
2. ~~Messaging + ErrorOr + Users slice template~~ (done).
3. Introduce `Domain/` + first VOs (`DisplayName`, `GeoCoordinate`) while migrating **Pois** / **Activities**.
4. Move write logic onto aggregates as each command is migrated.
5. Keep queries thin (DTO projections).
6. Update STATUS after each slice.

Do not big-bang rewrite MAUI. Do not rewrite all EF entities in one PR.
