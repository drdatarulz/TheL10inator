# Decisions Log — TheL10inator

Companion artifact to `PRD.md` and `screen-inventory.md`. Captures the key decisions made during the PRD phase, the alternatives that were on the table, and the reasoning behind each choice. Claude Code should read this alongside the PRD so it understands the *why* behind the requirements, not just the *what*.

Entries are grouped into Product Decisions, UX Decisions, Architectural Decisions, and Deferred / Open Questions.

---

## Product Decisions

### P-1: Project name is "TheL10inator"

**Decision:** The product is named "TheL10inator." Code namespace and solution name use `TheL10inator` (no spaces or hyphens).
**Alternatives considered:** Generic names like "EOSrunner," "L10App," "OCC.LeadershipMeeting." Plain `L10inator` without the leading "The."
**Rationale:** Internal product, internally chosen, with personality. The leading "The" is part of the brand. Single-word PascalCase namespace keeps assembly names clean (`TheL10inator.Api`, `TheL10inator.Web`).

### P-2: First-class scope is the EOS Level 10 weekly meeting only

**Decision:** Scope is the weekly L10 leadership meeting and the artifacts that feed it (V/TO, Accountability Chart, Rocks, Scorecard, Issues, To-Dos, People Analyzer). No quarterly off-sites, annual planning sessions, or other EOS rhythms in v1.
**Alternatives considered:** Build the full EOS rhythm calendar (weekly + quarterly + annual) up front. Build only the meeting runner and skip the artifact management.
**Rationale:** L10 is the highest-frequency, highest-pain meeting and the natural anchor for the rest of the toolkit. Quarterlies and annuals happen 4–5x per year and are tolerable on a whiteboard; weekly L10s aren't. Building artifact management *with* the meeting runner is necessary because L10 reviews each artifact every week — they can't be deferred.

### P-3: Single-team UI in v1, multi-team data model from day one

**Decision:** Database schema supports multiple teams (every entity has a `TeamId` FK). UI in v1 is hard-scoped to the leadership team; no team picker, no team admin UI. Bootstrap creates one team at install.
**Alternatives considered:** Single-team schema with a migration path later. Full multi-team UI from day one (team picker, team admin, cross-team reporting).
**Rationale:** The leadership team is the only confirmed user, but other OCC teams ("departmental L10s") will almost certainly want this once they see it. Adding `TeamId` to schema later is a painful migration; hiding the team picker until needed is trivial. This is the cheapest hedge.

### P-4: Clean-slate launch — no historical data import

**Decision:** No import of prior L10 history, scorecard data, V/TO content, or rock history. Users key in current artifacts at first use.
**Alternatives considered:** Import from existing spreadsheets / Ninety.io / Bloom Growth exports.
**Rationale:** OCC's prior tooling is fragmented (some Excel, some whiteboard, some none). Cost of building importers exceeds value when the team can re-key current state in under an hour. Historical meetings are not reviewed anyway.

### P-5: Two roles only — Member and Admin

**Decision:** RBAC is two roles. **Member** can view all team artifacts, participate in meetings, mark their own to-dos done, submit People Analyzer ratings. **Admin** can do everything Member can, plus manage team membership, edit V/TO and Accountability Chart, run People Analyzer evaluations, view/export audit log, archive/edit historical meetings.
**Alternatives considered:** Three roles (Member / Facilitator / Admin) where Facilitator can run meetings but not change membership. Per-artifact permissions.
**Rationale:** OCC leadership is 5–10 people who all trust each other. Per-artifact permissions and a separate Facilitator role are over-engineered for that population. Any team member can present the meeting; only Admin can change *people* and *evaluation* data because those are the sensitive bits.

### P-6: Manual scorecard entry in v1 with explicit integration seam

**Decision:** Scorecard metrics are entered manually each week by the metric owner via a simple form. Schema and API are designed so a future integration (e.g., reading from Azure DevOps, Stripe, a SQL warehouse) can populate weekly values without changing the consumer code.
**Alternatives considered:** Build integrations now (Stripe, GA4, Azure DevOps, custom SQL). Skip the integration seam and add it later.
**Rationale:** OCC's metrics today live in heterogeneous systems with no consistent API surface. Building integrations is open-ended scope; manual entry covers 100% of metrics on day one. The seam (a `MetricValueSource` enum on `ScorecardValues` plus a stable upsert endpoint) costs nothing now and unblocks later automation.

### P-7: All four EOS meeting outputs are first-class

**Decision:** Every concluded meeting produces and persists: (1) updated to-do list with new owners and due dates, (2) updated short-term issues list, (3) IDS resolution notes captured against issues, (4) cascading message text. Cascading message can optionally be emailed to a specified distribution list.
**Alternatives considered:** Capture only some outputs (e.g., skip cascading message). Treat cascading message as freeform note rather than first-class entity.
**Rationale:** Each output corresponds to a distinct EOS protocol and a real downstream action (cascading messages get sent to direct reports the same day; to-dos get worked on during the week). Treating any of them as second-class breaks the protocol.

### P-8: Full audit log for every mutation

**Decision:** Every insert/update/delete on every domain table writes a row to a generic `AuditEntries` table capturing: `EntityType`, `EntityId`, `ActorUserId`, `ChangedAtUtc`, `OperationType` (Create/Update/Delete), `FieldName`, `OldValue`, `NewValue`. Admin can query/export.
**Alternatives considered:** Audit log only on "important" entities (people, ratings, evaluations). Use SQL Server Temporal Tables instead of a hand-rolled audit table. Use change data capture / triggers.
**Rationale:** It's an internal trust app — leadership wants to see who changed a rock's status or a People Analyzer rating without having to dig. Generic table is the simplest implementation that covers everything; Temporal Tables couple us to one storage engine and are awkward to query across entity types. A single table with a typed enum `EntityType` column lets the audit screen be one query.

### P-9: People Analyzer is admin-initiated and ad hoc

**Decision:** People Analyzer evaluations are initiated by an Admin (no recurring schedule). Admin selects the subject and the raters, raters submit ratings asynchronously through the app, results are visible to admins only and to the subject themselves once Admin marks the evaluation complete.
**Alternatives considered:** Quarterly automated cycle. Public results visible to whole team. Anonymous ratings.
**Rationale:** OCC runs People Analyzer on demand (new hire 90-day, promotion, performance concern) — not on a calendar. Results being limited to admin + subject matches EOS facilitator guidance and protects psychological safety. Ratings are *attributed* (not anonymous) because the EOS protocol expects raters to discuss their ratings face-to-face.

### P-10: V/TO is hybrid structured + free-text escape

**Decision:** V/TO has structured fields for the eight standard EOS sections (Core Values, Core Focus, 10-Year Target, Marketing Strategy, 3-Year Picture, 1-Year Plan, Quarterly Rocks, Issues List). Each section also has a free-text "additional notes" field for anything that doesn't fit the structured slots.
**Alternatives considered:** Pure structured form (every field typed). Pure free-text page (one big markdown editor).
**Rationale:** Structured fields make the V/TO scannable in meetings and queryable in reports (e.g., "show me the 1-Year Plan goals across all teams"). Pure free-text is what V/TOs become in spreadsheets — beautiful at first, unusable in 6 months. Structured-only is too rigid because every real V/TO has at least one weird thing that doesn't fit.

### P-11: Accountability Chart rendered as a visual tree

**Decision:** The Accountability Chart shows a top-down org tree with seats as nodes and accountability lines drawn between them. Each seat shows a title, the holder's name (if any), and key accountabilities as bullets on hover/click.
**Alternatives considered:** Indented list (cheaper to build). Spreadsheet-style table.
**Rationale:** EOS literally calls it the *Accountability Chart* — practitioners expect it to look like a chart, not a list. Visual tree is the format the leadership team draws on whiteboards today; matching that representation is what makes the tool feel like the real thing.

### P-12: Issues come in two flavors — short-term and long-term

**Decision:** Single `Issues` table with a `ListType` discriminator (`ShortTerm` | `LongTerm`). Short-term issues are reviewed and IDS'd in the L10. Long-term issues live on a separate list reviewed quarterly (out of scope for v1 facilitation, but in scope for storage/listing). Issues can be moved between lists.
**Alternatives considered:** Two separate tables. Single list with a "deferred" flag.
**Rationale:** Same shape, same lifecycle, same fields — there's no reason to duplicate the schema. The `ListType` discriminator keeps the queries trivial and lets long-term issues remain visible without building a separate UI for them.

### P-13: Rocks have a single goal per metric, auto-status, configurable trailing window

**Decision:** Each rock has one quantifiable target (e.g., "Ship feature X by EOQ"). Status (On Track / Off Track / Done) auto-computes from the owner's weekly check-in updates against the target. Scorecard metric chart shows a trailing window configurable by the metric owner; default is 13 weeks (one quarter).
**Alternatives considered:** Multiple sub-goals per rock. Manual status only. Fixed 13-week chart.
**Rationale:** EOS is dogmatic that rocks are single, achievable, quantifiable goals — multi-goal rocks are an anti-pattern. Auto-status reduces meeting friction (no one has to remember to update the indicator). Configurable trailing window matters because some metrics are inherently noisy and look terrible at 13 weeks but fine at 26.

### P-14: Empty states are guided CTAs, not blank screens

**Decision:** Every list view (Rocks, Issues, To-Dos, Scorecard, etc.) has a designed empty state with explanatory text and a clear primary CTA ("Add your first rock"). No blank screens.
**Alternatives considered:** Generic "No items" placeholders. Onboarding wizard that walks through every empty list at first login.
**Rationale:** Most users will encounter most empty states exactly once (right after install). A wizard is overkill; raw empty placeholders are confusing for users who haven't used EOS tooling before. Per-list guided CTAs split the difference and don't get in the way after first use.

### P-15: Access is admin-invite only via email

**Decision:** New members are added by Admin entering their OCC email address. The system sends an email invite; the invitee clicks the link, signs in via Azure AD (which must match OCC's tenant), and lands on the dashboard. No self-service signup, no domain-wide auto-enrollment.
**Alternatives considered:** Self-service signup gated by Azure AD tenant match. Auto-enroll everyone in OCC's tenant. Manual user creation by DBA.
**Rationale:** The team is small enough that admin invite is one click per person, not a real burden. Auto-enroll would put irrelevant OCC staff (e.g., contractors) into leadership team data by accident. Self-service is the worst of both — the same admin work plus a confusing "request access" flow.

---

## UX Decisions

### U-1: Single big-screen presenter view + individual laptop participant views

**Decision:** Active L10 meetings have two distinct UI modes. The **Presenter View** is optimized for projection on a TV/conference room display: large fonts, current section highlighted, current timer prominent. The **Participant View** is what every other attendee sees on their laptop: same data but laptop-density, with quick-add forms for issues, headlines, to-dos, and rating their meeting at conclude time.
**Alternatives considered:** One responsive view that adapts to screen size. Presenter-only (everyone watches the screen). Participant-only (no projector mode).
**Rationale:** EOS meetings literally have a "screen" the facilitator drives and "laptops" everyone takes notes on; building one view that does both jobs always compromises one. Two views, same backing data, synchronized over SignalR is the clean split.

### U-2: Default to dark mode

**Decision:** UI defaults to dark mode. Light mode is available as a per-user toggle. Bootstrap 5's default theme is used; no custom design system.
**Alternatives considered:** Default light mode. Custom design system. System-preference auto-detect with no per-user override.
**Rationale:** Internal app, leadership team is dark-mode-by-default. Bootstrap default theme keeps build simple and avoids the trap of building a custom design system for a team of 10. Per-user toggle is a 30-minute feature; system-preference auto-detect is a nice-to-have we can add later.

### U-3: Timers are app-run, not facilitator-run

**Decision:** The L10 meeting runner advances the agenda and runs the section timers automatically. Facilitator presses "Start Meeting" once and "Conclude" once; everything in between is driven by the system. Facilitator can manually advance/extend a section if needed.
**Alternatives considered:** Facilitator manually advances each section. No timers (informational only). Strict timers that auto-advance regardless of state.
**Rationale:** The whole point of L10 is the discipline of the timing — every facilitator who runs one with a phone stopwatch loses 5–10 minutes per meeting. App-run timers free the facilitator to focus on content. Manual override exists because real meetings sometimes need to spend an extra minute on a hot issue.

---

## Architectural Decisions

### A-1: Angular frontend instead of standards-prescribed Blazor WASM

**Decision:** Use Angular 17+ with standalone components for the SPA. This is an explicit override of the TI Engineering Standards' default of Blazor WASM + MudBlazor.
**Alternatives considered:** Blazor WASM + MudBlazor (per standards). React. Vue. Server-rendered Razor.
**Rationale:** Kevin's team has Angular experience and wants this project to also serve as Angular skill-building. Standards explicitly allow per-project overrides documented in the project's CLAUDE.md and decisions log; this entry is the trail. Backend stack (Minimal APIs, Dapper, SQL Server, DbUp, xUnit, Playwright, Azure AD) is unchanged from standards.

### A-2: ng-bootstrap over alternatives

**Decision:** Use `ng-bootstrap` as the Angular component library backed by Bootstrap 5 styling.
**Alternatives considered:** Plain Bootstrap 5 with hand-rolled directives. Angular Material (different design language, requires its own theming). NG-ZORRO. PrimeNG.
**Rationale:** Kevin specified Bootstrap 5. ng-bootstrap is the maintained native-Angular wrapper; it implements Bootstrap components without jQuery and integrates with Angular forms/change detection cleanly. Plain Bootstrap requires re-implementing modal/dropdown/tooltip imperative APIs. Material/Zorro/Prime would override Bootstrap's look-and-feel.

### A-3: Hand-written Angular services + generated TypeScript types from OpenAPI

**Decision:** API client TypeScript types are generated from the .NET API's OpenAPI document via `openapi-typescript` (types only, no client code). Angular HTTP services are hand-written, importing those types.
**Alternatives considered:** Fully generated client (e.g., `openapi-generator-cli` typescript-angular). Hand-written types and services.
**Rationale:** Generated clients are noisy, hard to read, and produce verbose code that doesn't match Angular idioms. Hand-written services let us shape the surface (e.g., return `Observable<Foo>`, use BehaviorSubjects, retry/error policies) the way the team wants. Generated types are the safety net — schema drift between API and client breaks the build instead of breaking at runtime.

### A-4: SignalR for meeting live-sync

**Decision:** Use SignalR (`MeetingHub`) for real-time meeting state synchronization (current section, timer state, new headlines/issues/todos arriving during the meeting). Authenticate connections with the same JWT the REST API uses.
**Alternatives considered:** Polling every 2 seconds. Server-Sent Events. Native WebSocket without SignalR.
**Rationale:** Meeting state changes ~10x per minute during an active meeting (timer ticks, item adds, presenter clicks); polling either burns network or feels laggy. SignalR is the standard .NET answer and handles connection lifecycle, reconnection, and group broadcast (one group per active meeting) natively.

### A-5: Feature services + RxJS BehaviorSubjects for state management

**Decision:** Each Angular feature module owns a service holding its state in `BehaviorSubject`s. No NgRx, no Akita, no Signals-store-based global state in v1. Cross-feature communication goes through services injected at root.
**Alternatives considered:** NgRx (Redux-pattern global store). Akita / Elf. Angular Signals-based state (signal stores).
**Rationale:** App is small; NgRx ceremony (actions, reducers, effects, selectors) is more code than the features themselves. RxJS in feature services is idiomatic Angular and what most teams reach for first. If state complexity grows past what services can handle, NgRx or Signals can be introduced later, feature by feature.

### A-6: Docker containers, Azure Container Apps deployment, runtime portability

**Decision:** Both `TheL10inator.Api` and `TheL10inator.Web` (Angular served via nginx) ship as Docker images. Production deployment target is Azure Container Apps. Local dev uses `docker compose` with SQL Server in a sibling container. No assumption of Azure-specific services beyond Azure AD and Azure Communication Services Email.
**Alternatives considered:** Azure App Service (no containers). Bare VMs. Azure Kubernetes Service. Self-host Linux server.
**Rationale:** Containers give us identical local-dev and prod environments and let us move off Azure if needed (Container Apps is just K8s under the hood). App Service couples us to Azure, AKS is overkill for a leadership-team app. The dual-container split lets us scale API and Web independently and keeps the SPA cacheable behind nginx.

### A-7: Azure Communication Services for transactional email

**Decision:** Use Azure Communication Services Email for sending invite emails and (optional) cascading-message emails.
**Alternatives considered:** SendGrid. Microsoft Graph (send-as a service mailbox). Local SMTP relay through OCC's mail server.
**Rationale:** Already in the Azure tenant; no third-party billing relationship. Graph send-as requires standing up a service mailbox and granting it Mail.Send, which is administratively heavier than ACS. SMTP relay through the corporate mail server is fragile when used for app-generated mail.

### A-8: Generic AuditEntries table over Temporal Tables

**Decision:** Audit history is stored in a single `AuditEntries` table with `EntityType` discriminator, written by application code (Dapper, in the same transaction as the mutation).
**Alternatives considered:** SQL Server System-Versioned Temporal Tables. Database triggers. Change Data Capture.
**Rationale:** A single discriminated table makes the admin audit screen one query. Temporal Tables produce one history table per source table, which makes "show me everything Kevin changed last week" awkward. Triggers and CDC are operational hassles for an internal app. Application-side write also captures `ActorUserId` from the request context, which triggers can't easily do.

### A-9: Issues as a single table with discriminator over two tables

**Decision:** One `Issues` table, `ListType` enum column distinguishes short-term from long-term. Same constraints, same audit surface, same APIs (filtered by `ListType`).
**Alternatives considered:** Separate `ShortTermIssues` and `LongTermIssues` tables.
**Rationale:** See P-12 — same shape, same lifecycle, no benefit to splitting. Discriminator keeps cross-list moves to a single UPDATE.

### A-10: TheL10inator project layering follows TI standard

**Decision:** Solution layout: `TheL10inator.Domain` (entities, value objects, zero deps), `TheL10inator.Infrastructure` (Dapper repositories, ACS client, audit writer), `TheL10inator.Api` (Minimal API endpoints, hubs, DI wiring), `TheL10inator.Migrator` (DbUp console), `TheL10inator.Web` (Angular app), plus `*.Tests` and `*.Fakes` projects per standards.
**Alternatives considered:** Single `TheL10inator.Api` project (skip layering). Vertical-slice folder structure inside one project.
**Rationale:** Standards prescribe this layering and the test infrastructure (Testcontainers, hand-rolled fakes) assumes it. Following standards is the default; deviations need explicit justification (see A-1 for the only one).

---

## Deferred / Open Questions

These were raised, considered, and intentionally deferred. Each will become a future decision when the triggering condition arises.

### D-1: Multi-team UI rollout
**Question:** When and how do we expose the team picker and team admin UI?
**Trigger:** First request from a non-leadership OCC team to use the app.
**Notes:** Schema is ready (P-3); only the UI work and a "Manage Teams" admin page remain.

### D-2: Quarterly off-site / annual planning support
**Question:** Do we add a quarterly meeting runner (rock-setting) and an annual planning runner?
**Trigger:** Leadership team uses the app for two consecutive quarters and wants the same structure for off-sites.
**Notes:** Would reuse Issues, Rocks, V/TO, but needs new agenda/section logic.

### D-3: Scorecard data integrations
**Question:** Which scorecard metrics get auto-populated, in what order, and from which source systems?
**Trigger:** Manual entry becomes a meeting friction point (likely after 3 months of weekly use).
**Notes:** API surface for upserting weekly values is in place from day one (P-6); no schema change needed when integrations land.

### D-4: Anonymous People Analyzer ratings
**Question:** Should we add an option for anonymous ratings?
**Trigger:** Admin requests it after a real People Analyzer cycle.
**Notes:** Default in v1 is attributed (P-9). Anonymity changes the audit story and the in-meeting discussion protocol.

### D-5: Mobile / tablet form factor
**Question:** Should we ship a tablet-optimized presenter view or a phone participant view?
**Trigger:** Someone tries to run a meeting from an iPad and it's painful.
**Notes:** Bootstrap 5 + ng-bootstrap is responsive by default; nothing breaks on a tablet, but the meeting UX isn't tuned for it.

### D-6: Cross-team reporting (rocks/issues rollups)
**Question:** When multiple teams use the app, do we add a "company view" rolling up rocks, scorecards, issues across teams?
**Trigger:** Three or more teams in active use.
**Notes:** Requires a new role (Owner / VC / Visionary?) above Admin and a cross-team aggregate API. Not blocked by data model.

### D-7: Notifications (email digests, in-app badges)
**Question:** What proactive notifications should the app send?
**Trigger:** First missed-to-do or missed-meeting incident.
**Notes:** ACS Email is wired (A-7), so digest infrastructure is mostly ready. Decision is product (what to send, how often), not technical.

### D-8: Test data seeding for empty environments
**Question:** Do we ship a `--seed` flag on the migrator that creates a synthetic team + meetings for dev/demo?
**Trigger:** First time someone has to manually click through 30 forms to demo the app.
**Notes:** Easy to add to the migrator; deferred because real leadership data exists by week 2.

---

<!-- Decisions log version: 1.0 -->
<!-- Last updated: 2026-04-21 -->
