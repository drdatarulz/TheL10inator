# TheL10inator

Internal web app for running One Click Contractor's weekly EOS Level 10 (L10) leadership meeting and the artifacts around it — V/TO, Accountability Chart, Scorecard, Rocks, Issues, To-Dos, People Analyzer.

Built by Theoretically Impossible for OCC as a deliberate exercise of the TI v4 agentic development pipeline.

## Documentation

- [`CLAUDE.md`](CLAUDE.md) — project-specific rules and conventions
- [`ARCHITECTURE.md`](ARCHITECTURE.md) — system design, database schema, design rationale
- [`docs/PRD.md`](docs/PRD.md) — product requirements
- [`docs/screen-inventory.md`](docs/screen-inventory.md) — screen inventory
- [`docs/decisions-log.md`](docs/decisions-log.md) — PRD-phase decisions log
- **Project board:** https://github.com/users/drdatarulz/projects/11 — single source of truth for work tracking

---

## Stack

- **Backend:** .NET 10 Minimal APIs, Dapper, SQL Server, DbUp, Serilog, SignalR, Entra ID.
- **Frontend:** Angular 21 standalone components, `@ng-bootstrap/ng-bootstrap`, Bootstrap 5.3 (`data-bs-theme="dark"` default), `@microsoft/signalr`, `ngx-quill`.
- **Infra:** Docker → Azure Container Apps. Bicep IaC, GitHub Actions CI/CD.

Engineering conventions are defined in the sibling repo `../TI-Engineering-Standards/`. See [`CLAUDE.md`](CLAUDE.md) for the project-specific layer.

---

## Getting started

```bash
# .NET
dotnet build TheL10inator.sln
dotnet test

# Angular
cd src/TheL10inator.Web
npm install      # only needed after clone
npm start        # dev server on http://localhost:4200

# Full local stack (API + Web + SQL Server)
docker compose up --build

# Git hooks (after clone)
git config --local core.hooksPath .githooks
```

---

## Repository layout

```
src/
  TheL10inator.Domain/          Zero-dependency domain models + interfaces.
  TheL10inator.Infrastructure/  Dapper repositories, audit log, ACS email.
  TheL10inator.Api/             Minimal API + MeetingHub SignalR.
  TheL10inator.Migrator/        DbUp console runner (zero project refs).
  TheL10inator.Web/             Angular SPA.

tests/
  TheL10inator.Domain.Tests/
  TheL10inator.Api.Tests/
  TheL10inator.Integration.Tests/
  TheL10inator.Playwright.Tests/
  TheL10inator.Fakes/

docs/                           PRD, screen inventory, decisions log, checklists.
.claude/skills/                 v4 agentic pipeline skills (auto-synced from standards repo).
.githooks/                      commit-msg hook (rejects Co-Authored-By trailers).
```
