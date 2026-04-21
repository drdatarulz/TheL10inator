# TheL10inator — Build Status

Running log of milestones reached and noteworthy build events. `orchestrate-v4` appends entries as milestones complete; humans append entries for ops/infra changes.

---

## 2026-04-21 — Project bootstrapped (Phase 3.1–3.2, 3.6)

- PRD, screen inventory, and decisions log authored via Claude web (Phase 2).
- Repo initialized as `drdatarulz/TheL10inator` (public).
- Solution scaffolded: `TheL10inator.Domain`, `.Infrastructure`, `.Api`, `.Migrator`, plus `.Domain.Tests`, `.Api.Tests`, `.Integration.Tests`, `.Playwright.Tests`, and shared `.Fakes`. All targeting `net10.0`.
- Angular 21 workspace scaffolded under `src/TheL10inator.Web` with `@ng-bootstrap/ng-bootstrap`, Bootstrap 5.3, `@microsoft/signalr`, `ngx-quill`, `openapi-typescript` (dev).
- Baseline: `dotnet build` clean (0 warn / 0 err), smoke test passes, `ng build` clean.
- Standards synced from `../TI-Engineering-Standards/`; skills copied into `.claude/skills/`; commit-msg hook installed via `.githooks/`.
- Backlog generation (Phase 3.3) not yet run.

## 2026-04-21 — L10-1 scaffolding completion

- Seed DbUp migration `src/TheL10inator.Migrator/Scripts/001_Initial.sql` added (comment-only; tables land with feature stories).
- Api wired per `standards/logging.md`: `AspNetCore.HealthChecks.SqlServer` DI, `MapHealthChecks("/health/live" | "/health/ready")`, `CompactJsonFormatter` for non-Development, plain console in Development, `CorrelationIdMiddleware` pushes `X-Correlation-Id` into `LogContext` and echoes it on the response.
- `src/TheL10inator.Web/nginx.conf` extended with a `/health/` reverse-proxy block; `docker-compose.yml` web port mapping changed to `80:80`.
- CI workflow `.github/workflows/ci.yml` added with parallel `backend` (build + unit tests) and `frontend` (npm ci + build) jobs.
- Pre-commit hook (`.githooks/pre-commit`) runs `dotnet format TheL10inator.sln --verify-no-changes --no-restore`.
- README updated with links to `ARCHITECTURE.md` and project board (`https://github.com/users/drdatarulz/projects/11`).
- Smoke verified locally: `curl /health/live` → 200 "Healthy", `/health/ready` → 503 without SQL, correlation id auto-generated when absent and echoed when supplied.
