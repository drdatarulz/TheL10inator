# TheL10inator — Architecture

**Status:** Stub. Will be filled out during Phase 3.2 (before `prd-to-backlog-v4` is run) and evolved as milestones progress.

This document is the authoritative reference for the system's shape: compilable components, deployment topology, data model, cross-cutting concerns, and the rationale behind the choices. For the *requirements* behind this design, see `docs/PRD.md`. For the *decisions and rejected alternatives*, see `docs/decisions-log.md`.

---

## Compilable Components

| Component | Type | Purpose |
|---|---|---|
| `TheL10inator.Domain` | .NET class library | Zero-dependency pure domain: entities, value objects, enums, interfaces. |
| `TheL10inator.Infrastructure` | .NET class library | Dapper repositories, audit-log decorator, Azure Communication Services Email client, MSAL/Entra helpers. |
| `TheL10inator.Api` | .NET Minimal API (`Microsoft.NET.Sdk.Web`) | REST endpoints under `/api/**`, `MeetingHub` SignalR hub under `/hubs/**`, health endpoints, Serilog pipeline, Entra authentication middleware. |
| `TheL10inator.Migrator` | .NET console app | DbUp forward-only SQL migration runner. Zero project references; SQL scripts embedded as resources. |
| `TheL10inator.Web` | Angular 21 SPA | Single-page app served by nginx. Talks to the Api via `/api/**` and `/hubs/**` under the same origin. |

Test projects layer on top: `*.Domain.Tests`, `*.Api.Tests`, `*.Integration.Tests`, `*.Playwright.Tests`, and a shared `*.Fakes` class library for hand-rolled fakes.

---

## Deployment Topology

- **Production:** Azure Container Apps. Two app images (Api, Web). One short-lived pre-deploy job image (Migrator). Backing services: Azure SQL, Entra ID, Key Vault, Azure Communication Services Email.
- **Local:** `docker compose up` spins Api + Web + SQL Server in containers; a `migrator` service runs to completion first.
- **Image tagging:** built once, retagged per environment (`{sha}-dev` → `{sha}-staging` → `{sha}-prod`). See `../TI-Engineering-Standards/standards/environments.md`.

---

## Data Model

To be elaborated as stories land. Headline entities (see PRD §10):

`Teams`, `Users`, `TeamMembers`, `CoreValues`, `VtoSections`, `Seats`, `SeatAssignments`, `ScorecardMetrics`, `ScorecardEntries`, `Rocks`, `RockMilestones`, `Issues` (with `ListType` discriminator), `Todos`, `Meetings`, `MeetingSegments`, `MeetingAttendees`, `MeetingRatings`, `MeetingNotes`, `CascadingMessages`, `PeopleAnalyzerEvaluations`, `AuditEntries`.

Conventions (per `../TI-Engineering-Standards/standards/database.md`):
- All primary keys are `INT IDENTITY(1,1)`.
- All timestamps are `DATETIME2 UTC` with `SYSUTCDATETIME()` defaults.
- Every entity has a `TeamId` FK (multi-team-capable schema; single-team UI in v1).
- Every entity has a `DeletedAtUtc NULL` soft-delete column.
- Unique constraints live in SQL (e.g., `ScorecardEntries` is unique on `(MetricId, WeekStartUtc)`).

---

## Cross-Cutting Concerns

### Audit Log
A generic `AuditEntries` table captures `(EntityType, EntityId, ActorUserId, ChangedAtUtc, OperationType, FieldName, OldValue, NewValue)`. Writes are performed by a decorator around the Dapper repositories (or an equivalent write-side pipeline step), inside the same transaction as the mutation, so the audit row can never diverge from the state it describes.

### Authentication & Authorization
- Entra ID only. MSAL on the Angular side; `Microsoft.Identity.Web` on the Api.
- Only admin-invited emails can authenticate — a `Users.InvitedAt` row must exist.
- First admin is seeded from configuration `Administration:FirstAdminEmail` at startup if no admin exists.
- Role check happens at the endpoint layer (Member vs Admin) plus artifact-level checks where needed (e.g., rock-owner on status change).

### Real-Time (SignalR)
`MeetingHub` at `/hubs/meeting`. Groups keyed by meeting id. Event catalog: `SegmentChanged`, `TimerTick` (throttled to 1 Hz), `IssueActivated`, `IssueResolved`, `TodoAdded`, `TodoCompleted`, `CascadingMessageAdded`, `RatingSubmitted`, `MeetingEnded`. Only one meeting per team may be `InProgress` at a time.

### Logging
Serilog with correlation IDs, `{UserId}` and `{TeamId}` on every request log. Console sink in Dev; structured sink (Application Insights or equivalent) in non-local environments.

### Configuration
- `appsettings.json` for defaults, `appsettings.{Environment}.json` for env-specific, environment variables override both, Key Vault for secrets.
- `Authentication:UseDevBypass` defaults to `false` everywhere; only `docker-compose.yml` sets it `true`.

---

## Open Architecture Questions (carry-over from PRD §12)

- Accountability Chart render library — deferred to M3.
- PDF renderer for post-meeting digest — deferred to M8.
- Facilitator role beyond admin — deferred post-v1.
- Scorecard auto-ingest — post-v1; the `MetricValueSource` seam is in place.
