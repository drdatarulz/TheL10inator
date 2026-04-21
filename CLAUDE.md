# CLAUDE.md — TheL10inator

## Engineering Standards (Auto-Sync)

This project follows **TI Engineering Standards**. Before starting work:

1. Check for the standards repo at `../TI-Engineering-Standards/`
2. If not found: `git clone https://github.com/drdatarulz/TI-Engineering-Standards.git ../TI-Engineering-Standards/`
3. If found: `cd ../TI-Engineering-Standards && git pull --ff-only && cd -`
4. Read `../TI-Engineering-Standards/CLAUDE.md` and all referenced standards files.
5. Sync skills (skipping archive and any local overrides):
   ```bash
   mkdir -p .claude/skills
   for skill_dir in ../TI-Engineering-Standards/skills/*/; do
     skill_name=$(basename "$skill_dir")
     if [ "$skill_name" = "archive" ]; then continue; fi
     if [ ! -d ".claude/skills/$skill_name" ]; then
       cp -r "$skill_dir" ".claude/skills/$skill_name"
     fi
   done
   ```
6. Verify the git hooks path:
   ```bash
   [ "$(git config core.hooksPath 2>/dev/null)" != ".githooks" ] && git config core.hooksPath .githooks
   ```

All generic engineering rules live in that repo. This file contains only **project-specific** instructions.

---

## Project Overview

TheL10inator is an internal web app that runs One Click Contractor's weekly EOS Level 10 (L10) leadership meeting and the artifacts around it: V/TO, Accountability Chart, Scorecard, Rocks, Issues, To-Dos, and People Analyzer. It replaces the combination of timers, spreadsheets, and shared documents the leadership team currently juggles. TI is building the application for OCC as a deliberate exercise of the v4 agentic development pipeline — shipped end-to-end via `orchestrate-v4`.

Full architectural details: see `ARCHITECTURE.md`

---

## Documentation Map

| File | Purpose |
|------|---------|
| `CLAUDE.md` | Project-specific rules and conventions (this file) |
| `ARCHITECTURE.md` | Full system design, database schema, design rationale |
| `docs/PRD.md` | Product Requirements Document |
| `docs/screen-inventory.md` | Screen Inventory |
| `docs/decisions-log.md` | Decisions Log from PRD phase |
| `STATUS.md` | Build progress history |
| `docs/checklists/` | Deployment runbooks, manual testing checklists |
| `docs/architecture/` | Architecture diagrams (markdown + draw.io) |
| `.github/ISSUE_TEMPLATE/` | Issue templates for stories, bugs, and tasks |
| Project board | https://github.com/users/drdatarulz/projects/<TBD> — single source of truth for work tracking |

---

## Tech Stack (Project-Specific)

Standard TI stack applies (see `../TI-Engineering-Standards/`) with **one documented override**: the SPA framework is **Angular**, not Blazor WASM. Rationale captured in `docs/decisions-log.md` A-1.

### Backend (unchanged from TI standards)
- .NET 10 LTS, Minimal APIs, Dapper, SQL Server, DbUp forward-only migrations
- Serilog, Microsoft.Identity.Web (Entra ID / Azure AD), SignalR
- xUnit + Shouldly + Testcontainers.MsSql + Respawn for tests; hand-rolled fakes only (never Moq)

### Frontend (overrides TI default of Blazor WASM + MudBlazor)
- Angular LTS (currently v21) with standalone components, routing, SCSS
- `@ng-bootstrap/ng-bootstrap` over Bootstrap 5.3+; `data-bs-theme="dark"` as default (set in `index.html`), light-mode toggle
- `@microsoft/signalr` client for the `MeetingHub`
- `openapi-typescript` (types-only, hand-written services) — run `npm run generate:api-types` against a running Api
- `ngx-quill` for rich-text editors (V/TO notes, rock descriptions, meeting notes)
- Tree-diagram library for the Accountability Chart — final choice deferred to M3 (candidates: `@swimlane/ngx-graph`, custom SVG, d3-tree)

### Infrastructure (unchanged from TI standards)
- Docker → Azure Container Apps (production), `docker compose` (local)
- Azure SQL, Entra ID, Key Vault, Azure Communication Services Email
- Bicep IaC, GitHub Actions CI/CD with dev/staging/production environments

---

## Project Structure

```
src/
  TheL10inator.Domain/          # Zero deps. Models + Interfaces only.
  TheL10inator.Infrastructure/  # Dapper repositories, ACS client, audit writer.
  TheL10inator.Api/             # Minimal API endpoints, MeetingHub, DI wiring.
  TheL10inator.Migrator/        # DbUp console app. Zero project refs. SQL embedded.
  TheL10inator.Web/             # Angular 21 SPA.

tests/
  TheL10inator.Domain.Tests/
  TheL10inator.Api.Tests/
  TheL10inator.Integration.Tests/
  TheL10inator.Playwright.Tests/
  TheL10inator.Fakes/           # Shared hand-rolled fakes.
```

---

## Key Domain Rules

- **Audit trail is universal.** Every insert/update/delete on every domain table writes to a single `AuditEntries` table with `EntityType`, `EntityId`, `ActorUserId`, `ChangedAtUtc`, `OperationType`, `FieldName`, `OldValue`, `NewValue`. Implemented as a decorator/pipeline step, not per-repo boilerplate. See decisions log A-8.
- **All deletes are soft.** Every entity has a `DeletedAtUtc` column. No hard deletes in v1.
- **Multi-team schema, single-team UI.** Every entity has a `TeamId` FK. v1 exposes only the leadership team. See P-3.
- **Two roles only.** `Member` and `Admin`. The first admin is seeded from `Administration:FirstAdminEmail` at startup if no admin exists. See P-5.
- **Issues use a discriminator, not two tables.** `Issues.ListType` is `ShortTerm | LongTerm`. See P-12 / A-9.
- **Only one meeting per team can be `InProgress` at a time.** Enforced at insert. See M7.
- **`Authentication:UseDevBypass` defaults to `false` everywhere.** Only `docker-compose.yml` overrides to `true`; never ship a production image with it enabled.
- **Rocks in a past quarter are read-only even for admins.** Closed quarter = historical artifact. See Rocks screen key-states.

---

## Build Commands

```bash
# Build everything
dotnet build TheL10inator.sln

# Unit tests
dotnet test tests/TheL10inator.Domain.Tests/
dotnet test tests/TheL10inator.Api.Tests/

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/TheL10inator.Integration.Tests/

# Playwright tests
dotnet test tests/TheL10inator.Playwright.Tests/

# DbUp migrations
dotnet run --project src/TheL10inator.Migrator/ -- --ConnectionStrings:Sql="<conn>"

# Angular dev server
cd src/TheL10inator.Web && npm start

# Angular production build
cd src/TheL10inator.Web && npm run build

# Regenerate TypeScript DTOs from the running Api's OpenAPI doc
cd src/TheL10inator.Web && npm run generate:api-types

# Full local stack
docker compose up --build
```

---

## Work Tracking

- **GitHub Project board:** https://github.com/users/drdatarulz/projects/11
- **GitHub Project number:** `11`
- **Project ID:** `PVT_kwHOAcXLEM4BVTwF`
- **Story ID prefix:** `L10` (e.g., `L10-7` for issue #7)

### Field IDs (for skills and GraphQL mutations)

| Field | Field ID | Options |
|---|---|---|
| Status | `PVTSSF_lAHOAcXLEM4BVTwFzhQwaq4` | Inbox `4a5ecd39` / Up Next `eb8123d6` / In Progress `cc31ae6f` / Waiting/Blocked `8e8e59cb` / Someday/Maybe `ec92c5f7` / Done `9c7b75b7` |
| Type | `PVTSSF_lAHOAcXLEM4BVTwFzhQwa3o` | Story `66b29e9f` / Bug `2f76dcf0` / Task `5a0b32f3` |
| Priority | `PVTSSF_lAHOAcXLEM4BVTwFzhQwa4U` | High `a6c0ac16` / Medium `40dcf3b4` / Low `effd7293` |
| Story ID | `PVTF_lAHOAcXLEM4BVTwFzhQwa4Y` | text |
| Component | `PVTF_lAHOAcXLEM4BVTwFzhQwa5Q` | text |
| Due Date | `PVTF_lAHOAcXLEM4BVTwFzhQwa5U` | date |

> **One-time manual setup required:** The repo is already linked to the project, but the **Auto-add to project** workflow must be enabled from the board UI (Settings → Workflows → Auto-add to project → turn on, filter: `is:issue`). Also enable **Item closed → Done** and **PR merged → Done** under Workflows. GitHub's API does not expose these toggles programmatically.

---

## Namespace Gotchas

- The brand is **`TheL10inator`** (capital `L`, then digit `10`, then lowercase `inator`). NOT `The10Linator`, NOT `TheL10Inator`. Every assembly, namespace, repo name, and UI string uses `TheL10inator`.
- Angular project name in `package.json` is auto-lowercased to `the-l10inator.web` by the CLI — that's expected and only affects the npm package name.

---

## Things NOT To Do (Project-Specific)

- Do NOT use Blazor / MudBlazor anywhere — Angular is the chosen frontend for this project (see A-1).
- Do NOT use a fully-generated OpenAPI TypeScript client. Only types are generated; services are hand-written (see A-3).
- Do NOT introduce NgRx, Akita, or signal-stores for global state in v1. Feature services with `BehaviorSubject`s only (see A-5).
- Do NOT add scorecard data integrations in v1 — manual entry only. Preserve the `MetricValueSource` seam (see P-6).
- Do NOT support hard delete in v1. All deletes are soft; audit rows are indefinite.
- Do NOT add People Analyzer member self-rating in v1 (admin-initiated only — see P-9).
- Do NOT build Accountability Chart as an indented list — it is a visual tree (see P-11 / M3).
- Do NOT couple to Azure-specific APIs from application code. Azure SDK usage lives only in Infrastructure and speaks standard protocols (SQL wire, OIDC, SMTP/ACS SDK) so the app remains portable.
