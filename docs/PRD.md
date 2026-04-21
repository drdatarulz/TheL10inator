# TheL10inator — Product Requirements Document

**Status:** Draft for backlog decomposition
**Owner:** Kevin Phifer (kevin.phifer@theoreticallyimpossible.org)
**Last updated:** 2026-04-21
**Companion artifacts:** `screen-inventory.md`, `decisions-log.md`

---

## 1. Summary

TheL10inator is an internal web application that runs and manages One Click Contractor's weekly EOS Level 10 (L10) leadership meeting and the EOS artifacts that surround it — the Vision/Traction Organizer (V/TO), Accountability Chart, Scorecard, Rocks, Issues list, To-Dos, and People Analyzer. It replaces the combination of timers, spreadsheets, and shared documents that the leadership team currently juggles. TI is building the application for OCC as a deliberate exercise of the TI v4 agentic development pipeline — built end-to-end via `orchestrate-v4` rather than hand-written.

## 2. Problem / Motivation

The OCC leadership team runs a weekly L10 meeting following the *Traction* / EOS framework. Today the meeting is stitched together from multiple tools (timer on someone's phone, scorecard in a spreadsheet, issues list in a shared doc, V/TO in another doc). Every week a few minutes are lost to context switching and tool-wrangling. Historical lookup ("what was our Q3 revenue rock?") is slow. There is no durable audit trail of changes to sensitive artifacts like the V/TO or People Analyzer ratings.

Off-the-shelf EOS tools (Ninety.io, Bloom Growth, Traction Tools) exist and solve most of this, but OCC is having it built custom as a deliberate test case for the TI agentic-development workflow — an application small enough to ship end-to-end via `orchestrate-v4`, domain-rich enough to meaningfully exercise decomposition and review skills, and useful enough to OCC that the team will actually operate it daily.

## 3. Users & Access

- **Audience:** OCC leadership team only at v1. Typical size 5–10 people.
- **Authentication:** Azure AD / Entra ID only. No username-password auth.
- **Provisioning:** Admin invites members by email address. Only invited emails can authenticate. The first admin is seeded via application configuration at initial deployment.
- **Roles:**
  - **Member** — full read access to team artifacts (V/TO, Accountability Chart, Rocks, Scorecard, Issues, To-Dos, meeting history). Can add and resolve issues, complete to-dos, enter their own scorecard numbers, view their own People Analyzer history.
  - **Admin** — everything a member can do, plus edit V/TO, edit Accountability Chart, manage team membership, initiate People Analyzer evaluations for any member, see all People Analyzer history, and configure scorecard metric definitions.

## 4. Goals

1. Run the weekly L10 entirely inside the app — agenda, segment timers, scorecard review, rocks review, issue identification/discussion/solving, to-do capture, meeting rating — with no external tooling.
2. Maintain a single source of truth for every EOS artifact (V/TO, Accountability Chart, Scorecard, Rocks, Issues short and long-term, To-Dos, People Analyzer).
3. Produce a complete audit trail of every write for every entity, so that sensitive changes (V/TO edits, People Analyzer ratings, seat reassignments, rock ownership changes) can be reconstructed at any point.
4. Deliver a post-meeting PDF summary and emailed digest automatically within 5 minutes of meeting conclusion.
5. Exercise the full TI v4 agentic development pipeline — the project is built via `prd-to-backlog-v4`, `refine-story-v4`, `implement-ticket-v4`, `engineering-review-v4`, `security-review-v4`, `integration-test-v4`, `ui-test-v4`, and `orchestrate-v4`, with milestone review gates throughout.

## 5. Non-Goals (Out of Scope for v1)

- **Quarterly Pulsing and Annual Planning meeting runners.** Rocks and V/TO updates are entered manually by admins at the start of each quarter. The app supports the weekly rhythm only.
- **Multi-tenant SaaS.** The data model is multi-team-ready (see section 10) but only one team is exposed in the UI at launch.
- **External integrations.** No Slack, Teams, Outlook, Google Calendar, PagerDuty, or BI-tool integrations in v1. The only outbound network call is email delivery for the post-meeting digest.
- **Automated scorecard data ingestion.** Weekly scorecard numbers are entered manually. A "metric source" seam is included so a future version can add external pulls without schema changes.
- **Mobile-optimized UI.** The app targets laptops, plus a "presenter view" optimized for a single big-screen in-room display. Responsive design is only robust enough to not break on a tablet; phones are explicitly not supported.
- **Peer evaluations.** People Analyzer ratings are admin-initiated only. No member-to-member ratings.
- **Client-side state persistence across sessions.** All state is server-authoritative. No offline support.
- **Migration from another EOS tool.** Starting state is a clean slate — no historical data is imported from Ninety, Traction Tools, spreadsheets, etc.

## 6. Tech Stack

The project follows the **TI Engineering Standards** without exception, with **one documented override**: the UI framework is Angular (not Blazor WASM). See `decisions-log.md` for the rationale. All other standards — Minimal APIs, Dapper, SQL Server, DbUp, Serilog, xUnit/Shouldly/Testcontainers/Playwright, hand-rolled fakes, built-in DI, Bicep, Azure AD — apply as written.

UI-specific additions to the standard stack:

- **Angular** (current LTS at implementation time)
- **ng-bootstrap** for Bootstrap 5 component integration
- **Bootstrap 5.3+** with `data-bs-theme="dark"` at the root as the default; light-mode toggle available via user preference
- **@microsoft/signalr** for the live-meeting hub client
- **openapi-typescript** to generate TypeScript DTO types from the API's OpenAPI doc; services are hand-written
- **ngx-quill** for any rich-text editing (V/TO free-text escape fields, meeting notes)
- A tree-diagram library (to be chosen during M3 implementation — `@swimlane/ngx-graph` is the leading candidate) for the Accountability Chart visual rendering

## 7. Compilable Components

| Component | Description |
|---|---|
| **TheL10inator.Api** | .NET Minimal API. Hosts all REST endpoints, the `MeetingHub` SignalR hub, background services (post-meeting email digest generator), authentication middleware, and the audit log sink. |
| **TheL10inator.Web** | Angular single-page application. Served from its own container (nginx) behind the same hostname as the API via reverse proxy / ingress, so browsers hit `/api/...` and `/hubs/...` without CORS in production. |
| **TheL10inator.Migrator** | DbUp console application. Ships as its own container image, run as a short-lived pre-deploy job in Azure Container Apps. Owns the forward-only SQL schema. |
| **TheL10inator.Domain / .Infrastructure / tests** | Per TI standards — Domain has zero dependencies; Infrastructure hosts Dapper repositories and cloud-service clients; Fakes project hand-rolls test doubles. |

Deployment target: **Docker containers on Azure Container Apps** initially. The application is written to stay portable — no ACA-specific APIs or configuration bleeds into application code. Azure-specific infrastructure bindings (Azure SQL, Entra ID, Key Vault, Azure Communication Services Email) are consumed via standard protocols (SQL Server wire protocol, OpenID Connect, SMTP/ACS SDK) so that infrastructure swaps are a Bicep change, not an application change.

## 8. Functional Requirements by Milestone

### Milestone 1 — Foundations

Authentication, app shell, cross-cutting infrastructure. After M1, an admin can log in, see an empty app, and invite members.

- Azure AD authentication via MSAL Angular. Only admin-invited emails are permitted to authenticate.
- First admin seeded via `Administration:FirstAdminEmail` configuration. Seeded on first startup if no admin exists.
- `Authentication:UseDevBypass` feature flag defaulting to `false` everywhere; enabled only in local `docker-compose.yml`.
- Application shell: top bar (app name, team name, user menu with sign-out and theme toggle), left sidebar (sections for each major feature area), main content area.
- Dark-mode default, light-mode toggle, preference persisted per user.
- Health endpoints: `/health/live` (process check) and `/health/ready` (DB check).
- Serilog configured per standards. Correlation IDs, structured logging, `{UserId}` and `{TeamId}` on every request log.
- **Audit log cross-cutting concern** — generic `AuditEntries` table (columns: `Id`, `EntityType`, `EntityId`, `ActorUserId`, `ChangedAtUtc`, `OperationType` (`Create`/`Update`/`Delete`), `FieldName`, `OldValue`, `NewValue`). All repository writes record entries. Implemented as a decorator around repositories or as a write-side pipeline step, not per-repo boilerplate.
- Team settings page (minimal): team name, members list, invite/remove member flow, first-admin-only-at-seed rules visualized.
- Smoke test for M1: admin logs in via Entra, sees the dashboard shell with their name and team name, invites a second user by email, signs out. That user receives no email in M1 (no notifications scope), but their email is recorded and they can now authenticate on subsequent logins.

### Milestone 2 — Team & V/TO

After M2, the team has its V/TO captured and visible.

- V/TO screen with the eight standard sections: Core Values, Core Focus (Purpose/Cause/Passion + Niche), 10-Year Target, Marketing Strategy (Target Market + 3 Uniques + Proven Process + Guarantee), 3-Year Picture (future date, measurables, bullet list), 1-Year Plan (year-end goal, measurables, rocks summary), Quarterly Rocks (read-only summary from M5), Issues (read-only summary from M6).
- Each section has **structured fields** matching the standard EOS shape above, plus a free-text `Notes` field per section for anything that doesn't fit the structure.
- Core Values list: admin-only edit. Each value has a short label and a longer description.
- Every V/TO edit writes to the audit log (field-level diff).
- Only admins can edit V/TO content. Members see read-only.
- V/TO is viewable on both the participant view and the presenter view during the "Review V/TO" segment of an L10 (though V/TO review is not part of the standard L10 agenda, the navigation exists).

### Milestone 3 — Accountability Chart

After M3, the team's accountability chart is captured as a visual hierarchy.

- Accountability Chart rendered as a **visual tree diagram**. Each node is a "seat" showing: seat name, 5 major roles/responsibilities (free-text bullets), current assignee (a team member or "unassigned").
- Add, edit, delete, reposition seats. Seats can have parent/child relationships. A team member can hold multiple seats.
- Seat detail view (in a side drawer or modal) for editing the 5 roles and assignee.
- Admin-only edit. Members see read-only.
- Every seat change writes to the audit log.
- The same tree is rendered in a large, legible format on the presenter view so it's readable across a conference room when referenced during a meeting.

### Milestone 4 — Scorecard

After M4, the leadership team can define their weekly metrics and enter numbers.

- Scorecard view showing one row per metric, one column per week. Default view: trailing 13 weeks. Switcher for trailing 4 / 13 / 26 / 52 weeks.
- Each metric has: name, owner (one team member), goal value, comparison operator (`>=` or `<=`), unit (text label, e.g., "$", "%", "count"), is-active flag.
- Cells are auto-colored: green if the comparison passes, red if it fails, gray if no entry yet.
- Inline entry: click a cell, type a value, enter. Saves immediately. Edits allowed (audit-logged).
- Admin-only: add/edit/archive metric definitions. Members can enter/edit values on any metric.
- Weeks are labeled by the Monday date (UTC-normalized).
- Scorecard is shown on both participant and presenter views during the "Scorecard" segment of an L10.

### Milestone 5 — Rocks

After M5, quarterly rocks per person are trackable with on/off-track status and milestones.

- Rocks list view filtered by quarter (default: current quarter). Columns: rock title, owner, status (`On Track` / `Off Track` / `Done` / `Not Done`), milestone progress.
- Each rock has: title, description (rich-text via ngx-quill), owner, quarter (year + quarter number), status, milestones (each: description, target date, completed flag).
- Admin-only: add, reassign, archive rocks. Any member: update status on their own rocks, mark milestones complete.
- Quarter navigation: previous/current/upcoming quarter tabs.
- Rocks are shown on both views during the "Rock Review" segment of an L10.
- Every rock change writes to the audit log.

### Milestone 6 — Issues & To-Dos

After M6, issues and to-dos can be tracked independently of a running meeting.

- **Issues**: one `Issues` table with a `ListType` discriminator (`ShortTerm` or `LongTerm`). Two views in the UI, one for each list type.
- Issue record: title, description, list type, status (`Open` / `Solved` / `Dropped`), created-by, created-at, solved-by, solved-at, solution-notes, linked-cascading-message-id (nullable).
- Any member can add an issue. Only the issue owner (creator) or an admin can mark it solved.
- **To-Dos**: title, description, owner, due-at (defaults to 7 days from creation), status (`Open` / `Done`).
- To-Dos visually flag overdue (past due-at, still open) with a red indicator.
- Any member can add a to-do for themselves or another member. Only the owner or an admin can mark it done.
- No notifications in v1 — overdue to-dos are visible only in the app.

### Milestone 7 — L10 Meeting Runner

After M7, the weekly L10 meeting runs inside the app with live sync across attendees.

- **Meeting start flow**: any admin or the designated facilitator (a member granted `can-run-meeting` capability — deferred post-v1; any admin can start in v1) clicks "Start L10". Attendance check-in: each joining participant confirms presence. The meeting enters "In Progress" state with a start timestamp.
- **Segment timer state machine**: Segue (5 min) → Scorecard (5 min) → Rock Review (5 min) → Customer/Employee Headlines (5 min) → To-Do List (5 min) → IDS (60 min) → Conclude (5 min). App displays the current segment, time elapsed, time remaining. Facilitator can advance manually; segments do not auto-advance when time expires (the app flags the overrun but doesn't force a transition — this is a "guided system of record," not a police officer).
- **Two views over the same SignalR state**:
  - **Participant view** (laptop, each attendee): interactive. Shows the current segment, timer, and the corresponding content (scorecard, rocks, issues in IDS, etc.). Members can add issues, take to-dos, mark rocks, enter their rating.
  - **Presenter view** (big-screen in-room display): read-mostly, visually compact and legible from across a room. Shows the current segment name, a large timer, and the active content of the segment (e.g., the issue currently being IDS'd, the rock being reviewed). No interactive chrome.
- **IDS flow**: during the IDS segment, facilitator selects an issue from the short-term list; it becomes the "active issue" visible on both views. Discussion happens; attendees may add to-dos or identify new issues. Facilitator marks the issue Solved (with optional solution notes) or moves on.
- **Meeting output capture during the meeting**:
  - Freeform meeting notes (one rich-text field per meeting, always-visible sidebar on participant view).
  - Cascading messages (structured: audience, message text, owner). Added during Conclude segment.
  - Meeting rating per attendee (1–10 integer, optional comment) in Conclude.
- **Meeting close**: facilitator clicks "End Meeting". Meeting state transitions to `Completed`. Triggers the post-meeting digest pipeline (M8).
- SignalR hub: `/hubs/meeting`. Events: `SegmentChanged`, `TimerTick` (throttled), `IssueActivated`, `IssueResolved`, `TodoAdded`, `TodoCompleted`, `CascadingMessageAdded`, `RatingSubmitted`, `MeetingEnded`.
- Only one L10 meeting per team can be "In Progress" at a time.

### Milestone 8 — Reporting

After M8, every completed meeting produces a PDF summary and an emailed digest.

- **Weekly summary PDF**: server-side-rendered (likely via PuppeteerSharp or a headless-Chromium container). Contents: team name, meeting date, attendees, final rating (average and individual), scorecard snapshot for the week, rocks status summary, issues solved in the meeting, issues added, to-dos created, cascading messages, freeform meeting notes.
- **Emailed digest**: triggered by `MeetingEnded`. Sent via Azure Communication Services Email (SMTP fallback for non-Azure deployments). Recipients: all attendees plus admin-configurable extra addresses (per team). Contains a short summary and the PDF as an attachment.
- **Meeting history screen**: browse any past meeting, view the full archived state, download the PDF.

## 9. Non-Functional Requirements

- **Audit trail**: every write to every entity recorded in `AuditEntries`. Queryable by admin. No entity is deleted — all deletes are soft (a `DeletedAtUtc` column on every entity); audit records remain indefinitely.
- **Performance**: interactive operations complete in under 300 ms p95 for typical team sizes (≤ 20 users, ≤ 500 issues, ≤ 200 rocks, ≤ 30 metrics × 52 weeks). Post-meeting digest delivered within 5 minutes of meeting close.
- **Availability**: target 99% monthly uptime in production. Internal tool — no financial SLAs.
- **Security** (per `standards/security.md`): all endpoints require authentication, authorization checked on every read and write, rate limiting at 60 req/min/user, CORS restricted to known origins in production (`AllowCredentials`), dev bypass off everywhere except local docker-compose, input validation at the API boundary only.
- **Accessibility**: keyboard navigation throughout; ARIA attributes provided by ng-bootstrap; contrast meets WCAG AA in both themes. Not formally audited in v1.
- **Browser support**: latest two versions of Edge, Chrome, and Firefox. No IE. No Safari optimization in v1.
- **Data retention**: meetings retained indefinitely. Audit entries retained indefinitely. Soft-deleted entities retained indefinitely with a `DeletedAtUtc` timestamp. Hard-delete utilities, if ever needed, are admin-only, audit-logged, and post-v1.
- **Portability**: application code makes no calls to Azure-only APIs. Infrastructure (SQL, auth, email, secrets) is consumed via standard protocols so that a swap of target cloud is a Bicep/parameter change, not an application-code change.

## 10. Data Model (Headline)

Full details will emerge in `ARCHITECTURE.md` during Phase 3. Key entities and relationships:

- `Teams` (with multi-team-capable schema but one row in v1)
- `Users` (Azure AD `oid` is the stable identity; email, display name captured on first login; team assignment via `TeamMembers`)
- `TeamMembers` (team ↔ user link + role: `Member` / `Admin`)
- `CoreValues`, `VtoSections` (V/TO structured content per team)
- `Seats`, `SeatAssignments` (Accountability Chart)
- `ScorecardMetrics`, `ScorecardEntries` (unique on `MetricId + WeekStartUtc`)
- `Rocks`, `RockMilestones`
- `Issues` (single table with `ListType` discriminator)
- `Todos`
- `Meetings` (L10 instances)
- `MeetingSegments` (per-meeting timing data)
- `MeetingAttendees`
- `MeetingRatings`
- `MeetingNotes`
- `CascadingMessages` (meeting-scoped)
- `PeopleAnalyzerEvaluations` (per-person, per-evaluation-date; admin-initiated; stores per-core-value ± rating and GWC)
- `AuditEntries` (the single audit table for everything)

All primary keys are `INT IDENTITY(1,1)`. All timestamps are `DATETIME2 UTC` with `SYSUTCDATETIME()` defaults, per `standards/database.md`.

## 11. Success Criteria

1. The OCC leadership team uses TheL10inator for every L10 meeting starting the week after M7 ships.
2. No meeting falls back to the previous spreadsheet/doc/timer stack.
3. Average weekly meeting rating is ≥ 8 after four weeks of full use.
4. For any sensitive artifact change surfaced in an audit review, the audit log yields the complete change history without ambiguity.
5. The post-meeting digest email arrives within 5 minutes of meeting close for 95% of meetings.
6. The project is shipped entirely through the v4 agentic pipeline. Every merge to `main` passes engineering and security review agents; every feature milestone is gated by human smoke-test approval; no ad-hoc code is committed outside the pipeline except for triage-initiated fixes.

## 12. Open Questions & Deferred Decisions

- **Email digest template design** — deferred to M8 implementation. Starting point: simple text-plus-logo template with the PDF attached.
- **Facilitator role beyond admin** — v1: any admin can start a meeting. A dedicated `Facilitator` role (neither Member nor Admin, but with `can-run-meeting`) is a likely post-v1 addition.
- **People Analyzer member self-rating** — v1: admin-only initiation. Self-rating is a candidate for v1.1.
- **Notification of overdue to-dos** — v1: in-app flag only. Email-on-overdue deferred to post-v1 (likely alongside any broader notification work).
- **Hard delete capability** — v1: soft delete only. If admin ever needs hard delete (GDPR-style user erasure), that's a dedicated story post-v1 with its own review gates.
- **Accountability Chart rendering library** — final choice deferred to M3 implementation. Candidates: `@swimlane/ngx-graph`, custom SVG, d3-tree. Must support interactive edit and large-screen rendering.
- **PDF renderer** — final choice deferred to M8. Candidates: PuppeteerSharp (headless Chrome in the API container), DinkToPdf (wkhtmltopdf-based), or server-side rendering to HTML + emailed as HTML only. Decision depends on Azure Container Apps container-size constraints and rendering fidelity needs.

## 13. References

- **TI Engineering Standards repo** (sibling at `../TI-Engineering-Standards/`): authoritative stack, conventions, skills.
- **Companion artifacts**: `docs/screen-inventory.md`, `docs/decisions-log.md`.
- **Agentic development workflow**: `../TI-Engineering-Standards/workflow/agentic-development-workflow.md`.
- **Story writing standards**: `../TI-Engineering-Standards/standards/story-writing-standards.md`.
