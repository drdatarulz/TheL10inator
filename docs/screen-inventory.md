# TheL10inator — Screen Inventory

**Companion to:** `PRD.md`, `decisions-log.md`
**Last updated:** 2026-04-21

---

## Application: TheL10inator

### Navigation Model

**Primary navigation:** Left sidebar with sections grouped by EOS artifact (Dashboard, V/TO, Accountability Chart, Scorecard, Rocks, Issues, To-Dos, People Analyzer, Meetings), plus an Admin group (Team Settings, Audit Log) visible only to admins.

**Top bar:** App name on the left, current team name in the center, user menu on the right (displays user name/initials, contains links to Profile, Theme Toggle, Sign Out).

**Authentication boundary:** `/login` is the only unauthenticated route. All other routes require an authenticated session; unauthenticated visits redirect to `/login`. On the API side, every endpoint except `/health/live` and `/health/ready` requires a valid Azure AD bearer token.

**Default landing:** `/dashboard` after successful login.

**Route conventions:** All routes are under the root origin. API at `/api/**`, SignalR at `/hubs/**`, everything else is the Angular app. Presenter views use a `?presenter=true` query parameter on the same route rather than a separate URL tree, so the presenter and participant see the same logical screen with different layouts.

---

### Screen Definitions

---

#### Login

**Route:** `/login`
**Purpose:** Direct the user through Azure AD authentication. Not a form — a single "Sign in with Microsoft" button that initiates the MSAL redirect flow.

**User arrives here from:**
- Any route → automatic redirect when unauthenticated
- Explicit Sign Out → redirects here

**What the user sees:**
- App logo and name
- "Sign in with Microsoft" button
- If the attempted email is not in the invited list, a post-redirect error banner: "Your account doesn't have access to this team. Ask an admin to invite you."

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click "Sign in with Microsoft" | Initiates MSAL redirect to Entra ID | Dashboard (on success) / Login with error (on failure) |

**Data dependencies:** None (pre-auth)

**Key states:**
- **Default:** logo + sign-in button
- **Post-redirect error:** inline banner with the specific error (unauthorized email, consent required, etc.)

**Notes:** Dev bypass, when enabled, replaces the sign-in button with a simple email input — this UI only exists when `Authentication:UseDevBypass` is `true` and is never built into production images per the security standard.

---

#### Dashboard

**Route:** `/dashboard`
**Purpose:** Landing page after login. Quick-scan of the team's current state and an entry point into every feature area.

**User arrives here from:**
- Login → successful authentication
- Top-bar home link or app logo click
- Sidebar "Dashboard" link

**What the user sees:**
- Welcome banner with user name and team name
- Summary cards: "Next L10" (date/time placeholder in v1 since there's no calendar integration — just "No meeting in progress" or "Meeting in progress since 9:04"), "Open Issues (short-term)", "Open Issues (long-term)", "Open To-Dos for you", "Current-quarter rocks on/off track"
- Recent activity: last 5 meeting ratings with dates, last 5 completed to-dos, last 3 rocks with status changes
- Quick actions: "Start L10" (admin only), "Add Issue", "Add To-Do"

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click a summary card | Jumps to that section's main screen | Respective section |
| Click "Start L10" | Validates no meeting in progress, opens the Start Meeting flow | Active Meeting — Pre-Start |
| Click "Add Issue" / "Add To-Do" | Opens the relevant add modal | (Modal over Dashboard) |
| Click a recent activity item | Jumps to that item in context | Relevant detail screen |

**Data dependencies:**
- `GET /api/dashboard/summary` — aggregate counts and "meeting in progress" flag
- `GET /api/meetings/recent?limit=5` — last 5 meetings with ratings
- `GET /api/todos?ownerId={me}&status=Open`
- `GET /api/rocks?quarter=current&summary=true`

**Key states:**
- **Empty state (new team, no data yet):** "Welcome to TheL10inator. Start by adding your team's Core Values." with a CTA to V/TO. Summary cards show zero counts with friendly placeholders.
- **Loading:** skeleton placeholders for cards and activity feed.
- **Error:** inline banner above content with a retry button. Cards that failed individually show inline error with per-card retry.

**Notes:** Not a data-heavy screen — it's the jumping-off point. Keep content above the fold on 1280px width.

---

#### Team Settings (Admin only)

**Route:** `/team/settings`
**Purpose:** Manage team name and members. Invite new members, change roles, remove members.

**User arrives here from:**
- Sidebar Admin → Team Settings
- Top bar → team name link (admin only)

**What the user sees:**
- Editable team name field
- Members table: columns for Avatar/Initials, Display Name, Email, Role (Member/Admin), Last Login, Actions
- "Invite Member" button at the top right of the table

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Edit team name (admin) | Inline edit, save on blur | Stays on screen |
| Click "Invite Member" | Opens the Invite Member modal | (Modal) |
| Change a member's role | Inline dropdown (Member ↔ Admin) — disabled for the current user on themselves | Stays on screen |
| Click "Remove" on a row | Opens Confirm Delete modal; on confirm, soft-deletes the membership | (Modal then stays on screen) |
| Click a row | Expands to a detail drawer showing People Analyzer history, audit trail for this user's role/membership changes | Drawer on same screen |

**Data dependencies:**
- `GET /api/teams/current`
- `GET /api/teams/current/members`
- `POST /api/teams/current/members` (invite)
- `PATCH /api/teams/current/members/{userId}` (role change)
- `DELETE /api/teams/current/members/{userId}` (soft remove)

**Key states:**
- **Empty members (only first admin):** "Invite your first team member to get started."
- **Loading / Error:** standard skeleton / banner patterns.
- **Self-modification blocked:** current user cannot demote or remove themselves — UI shows role dropdown and remove button disabled with tooltip "You can't change your own role."

---

#### User Profile

**Route:** `/profile`
**Purpose:** Personal settings — display name, theme preference, notification preferences (placeholder in v1), People Analyzer history (self-view).

**User arrives here from:**
- Top bar user menu → Profile

**What the user sees:**
- Display name (editable; defaults to Entra-provided name)
- Email (read-only, from Entra)
- Theme toggle (Dark / Light / System) — persists to user settings
- "Your People Analyzer history" section: list of evaluations about you, most recent first, each showing date, evaluator, core-value ratings, GWC ratings. No peers' ratings visible here.

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Edit display name | Inline, save on blur | Stays |
| Change theme | Immediate apply + persist | Stays |
| Click a PA evaluation row | Opens full evaluation detail in a drawer | Drawer |

**Data dependencies:**
- `GET /api/users/me`
- `PATCH /api/users/me`
- `GET /api/people-analyzer/me` (self-view only)

**Key states:**
- **No PA evaluations:** "You haven't been evaluated yet."
- **Loading / Error:** standard.

---

#### V/TO

**Route:** `/vto`
**Purpose:** View and (for admins) edit the team's Vision/Traction Organizer.

**User arrives here from:**
- Sidebar → V/TO
- Dashboard → "Go to V/TO" CTA in empty state

**What the user sees:**
- Eight section cards laid out vertically, each expandable/collapsible:
  1. **Core Values** — list of values with descriptions
  2. **Core Focus** — Purpose/Cause/Passion + Niche (two fields)
  3. **10-Year Target** — one line
  4. **Marketing Strategy** — Target Market + 3 Uniques + Proven Process + Guarantee
  5. **3-Year Picture** — future date + measurables (key/value pairs) + bullet list + free-text notes escape
  6. **1-Year Plan** — year-end goal + measurables + summary of rocks
  7. **Quarterly Rocks** — read-only summary linking out to the Rocks screen
  8. **Issues List** — read-only summary linking out to the Issues screens
- Each section (1-6) has structured fields plus a "Notes" free-text escape field (ngx-quill rich text).
- Edit controls visible only to admins. Members see read-only cards.

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Expand/collapse a section | Toggles visibility | Stays |
| Click "Edit" on a section (admin) | Enters edit mode for that section only | Stays (inline edit) |
| Save a section | Persists edits, writes to audit log, returns to view mode | Stays |
| Cancel edit | Reverts, no save | Stays |
| Click "Add Core Value" (admin, in Core Values section) | Opens Add/Edit Core Value modal | (Modal) |
| Click a core value row | Opens Edit Core Value modal (admin) or read-only detail (member) | (Modal) |
| Click the Rocks summary | Jumps to `/rocks` filtered to current quarter | Rocks list |
| Click the Issues summary | Jumps to `/issues/short-term` | Short-Term Issues |

**Data dependencies:**
- `GET /api/vto` — the whole V/TO for the current team
- `PATCH /api/vto/core-values/{id}`, `POST /api/vto/core-values`, `DELETE /api/vto/core-values/{id}`
- `PATCH /api/vto/sections/{sectionKey}` — updates structured fields for a named section
- `GET /api/rocks?quarter=current&summary=true`
- `GET /api/issues?listType=ShortTerm&status=Open&summary=true`

**Key states:**
- **Empty V/TO (new team):** each section shows empty-state CTA: "No core values yet. Add your first one." etc. Admin-only CTAs are disabled for members with a tooltip "Only admins can edit the V/TO."
- **Loading:** skeleton cards.
- **Error:** section-level error banners with per-section retry.

---

#### Accountability Chart

**Route:** `/accountability-chart`
**Route (presenter variant):** `/accountability-chart?presenter=true`
**Purpose:** View and (for admins) edit the team's accountability chart as a visual tree.

**User arrives here from:**
- Sidebar → Accountability Chart
- V/TO → "View full chart" link (future, not v1)

**What the user sees:**
- A visual tree diagram, rendered full-width. Root seat at the top (typically Integrator/Visionary), departmental seats branching below, individual roles nested further down.
- Each node shows: seat name, assignee name (or "Unassigned"), up to 5 role bullets (truncated if long, full on hover).
- Zoom and pan controls (bottom right).
- "Add Seat" button (admin only) in the toolbar.

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click a seat | Opens Seat Detail drawer | Drawer |
| Drag a seat (admin) | Reparents it to a new location | Stays, confirm with toast |
| Click "Add Seat" (admin) | Opens Add/Edit Seat modal in "add" mode | (Modal) |
| Click "Edit" in Seat Detail drawer (admin) | Opens Add/Edit Seat modal in "edit" mode | (Modal) |
| Click "Delete" in Seat Detail drawer (admin) | Opens Confirm Delete modal; must reassign or delete children first | (Modal) |
| Zoom / pan | Adjusts the viewport | Stays |

**Data dependencies:**
- `GET /api/accountability-chart` — full tree
- `POST /api/seats`, `PATCH /api/seats/{id}`, `DELETE /api/seats/{id}`
- `POST /api/seats/{id}/reparent`

**Key states:**
- **Empty chart:** centered "No seats defined yet. Add your first seat to get started." + CTA (admin only).
- **Loading:** diagram area shows spinner; layout doesn't shift on load.
- **Error:** full-screen error banner with retry.
- **Presenter variant:** zoom controls hidden, nodes sized up for readability, no drag/edit affordances, no drawer/modal triggers.

**Notes:** Tree library TBD at M3 implementation time. Must support: ≥ 30 seats rendering without jank, interactive edit in normal view, read-only enlarged in presenter variant.

---

#### Scorecard

**Route:** `/scorecard`
**Route (presenter variant):** `/scorecard?presenter=true`
**Purpose:** View weekly metrics, enter numbers, define and manage metrics (admin).

**User arrives here from:**
- Sidebar → Scorecard
- Active Meeting (during Scorecard segment) → automatically

**What the user sees:**
- Header with period switcher (Trailing 4 / 13 / 26 / 52 weeks, default 13)
- Grid: rows are metrics; first column is metric name + owner + goal; subsequent columns are weeks (most recent rightmost), each with the entered value and a green/red/gray cell background
- Footer row: "Add Metric" button (admin only)

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click a cell | Opens inline editor; type value; Enter to save | Stays |
| Click metric name | Opens Metric Detail drawer (goal, owner, comparison, history, audit trail) | Drawer |
| Click "Edit" in drawer (admin) | Opens Add/Edit Metric modal | (Modal) |
| Click "Archive" in drawer (admin) | Soft-deletes (hides) the metric — its history remains queryable | Stays |
| Change period switcher | Reloads grid for new range | Stays |
| Click "Add Metric" (admin) | Opens Add/Edit Metric modal in add mode | (Modal) |

**Data dependencies:**
- `GET /api/scorecard/metrics` — active metric definitions
- `GET /api/scorecard/entries?from={weekStart}&to={weekStart}` — entries for the range
- `PUT /api/scorecard/entries` — upsert a value for (metricId, weekStartUtc)
- `POST /api/scorecard/metrics`, `PATCH /api/scorecard/metrics/{id}`, `DELETE /api/scorecard/metrics/{id}`

**Key states:**
- **No metrics yet:** centered "No metrics yet. Admins can add the first metric to get started." + CTA (admin only).
- **Metric with no entries:** gray cells with "—" placeholder.
- **Loading:** row-by-row skeletons.
- **Error:** banner at top; inline errors on failed cell saves with retry chip.
- **Presenter variant:** larger font, no inline edit, no "Add Metric" button, period switcher hidden (locked to trailing 13).

---

#### Rocks

**Route:** `/rocks`
**Route (presenter variant):** `/rocks?presenter=true`
**Purpose:** Track quarterly rocks per person with status and milestones.

**User arrives here from:**
- Sidebar → Rocks
- V/TO → Quarterly Rocks summary link
- Active Meeting (Rock Review segment) → automatically

**What the user sees:**
- Quarter navigator (Previous / Current / Next tabs; label shows year + quarter, e.g., "Q2 2026")
- Rocks list grouped by owner. Each rock shows: title, status pill (On Track / Off Track / Done / Not Done), milestone progress bar (x of y milestones done), truncated description on hover.
- Summary bar at top: count of rocks in each status for the selected quarter.
- "Add Rock" button (admin only).

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click a rock | Opens Rock Detail drawer (full description, milestones, audit) | Drawer |
| Click "Edit" in drawer (admin) | Opens Add/Edit Rock modal | (Modal) |
| Click status pill (rock owner or admin) | Opens status selector (On Track / Off Track / Done / Not Done) | Stays |
| Click milestone checkbox in drawer (rock owner or admin) | Toggles milestone done state | Stays |
| Click "Add Rock" (admin) | Opens Add/Edit Rock modal in add mode | (Modal) |
| Change quarter | Reloads list | Stays |

**Data dependencies:**
- `GET /api/rocks?quarter={year-q}` — list for a quarter
- `POST /api/rocks`, `PATCH /api/rocks/{id}`, `DELETE /api/rocks/{id}`
- `PATCH /api/rocks/{id}/status`
- `POST /api/rocks/{rockId}/milestones`, `PATCH /api/rocks/{rockId}/milestones/{id}`, `DELETE /api/rocks/{rockId}/milestones/{id}`

**Key states:**
- **No rocks for quarter:** "No rocks set for Q2 2026 yet. Admins can add the first rock." + CTA (admin).
- **Past quarter:** read-only banner "This quarter is closed. Changes are disabled." — even admins can't edit rocks from a past quarter (rocks from the past are historical artifacts).
- **Loading / Error:** standard.
- **Presenter variant:** larger type, no "Add Rock", no drawer edit. Used during Rock Review segment.

---

#### Short-Term Issues

**Route:** `/issues/short-term`
**Route (presenter variant):** `/issues/short-term?presenter=true`
**Purpose:** The IDS queue — the living list of issues to work during L10 meetings.

**User arrives here from:**
- Sidebar → Issues → Short-Term
- Dashboard → Open Issues card
- Active Meeting (IDS segment) → automatically (as "Issues to Work")

**What the user sees:**
- Issues table: columns for Title, Owner/Raised-By, Raised-On, Status (Open/Solved/Dropped), Age (days), Actions
- Filters at top: status (default Open), owner (default all)
- "Add Issue" button

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click a row | Opens Issue Detail drawer (description, audit, link to resolving meeting if solved) | Drawer |
| Click "Add Issue" | Opens Add/Edit Issue modal | (Modal) |
| Click "Edit" in drawer (owner or admin) | Opens Add/Edit Issue modal | (Modal) |
| Click "Mark Solved" in drawer (owner or admin) | Prompts for solution notes; saves; triggers audit entry | Stays |
| Click "Move to Long-Term" in drawer (owner or admin) | Changes list type | Stays |
| Click "Drop" in drawer (owner or admin) | Soft-closes with Dropped status | Stays |

**Data dependencies:**
- `GET /api/issues?listType=ShortTerm&status={filter}&ownerId={filter}`
- `POST /api/issues`, `PATCH /api/issues/{id}`, `DELETE /api/issues/{id}`
- `PATCH /api/issues/{id}/status`

**Key states:**
- **Empty:** "No short-term issues. Nice work." (friendly).
- **During meeting (IDS active issue):** active issue is visually emphasized with an outline and "Discussing now" pill.
- **Loading / Error:** standard.
- **Presenter variant:** larger type, no "Add Issue", no drawer. Shows the active issue (if one is selected by the facilitator) prominently at top.

---

#### Long-Term Issues

**Route:** `/issues/long-term`
**Purpose:** Parking lot for strategic issues not for weekly IDS.

(Structurally identical to Short-Term Issues with `listType=LongTerm`. Same screen template, same modals, only difference is the filter applied to `GET /api/issues` and the header text.)

**Notes:** No presenter variant needed — long-term issues aren't reviewed during L10.

---

#### To-Dos

**Route:** `/todos`
**Route (presenter variant):** `/todos?presenter=true`
**Purpose:** Track 7-day action items from the L10.

**User arrives here from:**
- Sidebar → To-Dos
- Dashboard → Open To-Dos card
- Active Meeting (To-Do List segment) → automatically

**What the user sees:**
- To-dos table: columns for Title, Owner, Due Date, Age, Status, Actions
- Overdue to-dos visually flagged red (past due-at, still open)
- Filters: owner (default me), status (default Open)
- "Add To-Do" button

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click a row | Opens To-Do Detail drawer | Drawer |
| Click "Add To-Do" | Opens Add/Edit To-Do modal | (Modal) |
| Click checkbox in row (owner or admin) | Marks done | Stays |
| Click "Edit" in drawer (owner or admin) | Opens Add/Edit To-Do modal | (Modal) |

**Data dependencies:**
- `GET /api/todos?ownerId={filter}&status={filter}`
- `POST /api/todos`, `PATCH /api/todos/{id}`, `DELETE /api/todos/{id}`

**Key states:**
- **Empty:** "No open to-dos. Nice work."
- **Overdue rows:** red left border + red due-date text.
- **Loading / Error:** standard.
- **Presenter variant:** larger type, no "Add To-Do", no drawer. Used during To-Do List segment.

---

#### People Analyzer

**Route:** `/people-analyzer`
**Purpose:** (Admin only) View all team members' rating history, initiate new evaluations.

**User arrives here from:**
- Sidebar → People Analyzer (admin only in sidebar)

**What the user sees:**
- Grid: one row per team member. Columns: member, last evaluation date, latest core-value scores (compact + / ± / − icons per value), latest GWC (three icons), actions.
- "Evaluate" button per row.
- Link to filter archived / inactive members.

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click a row | Opens Member Evaluations drawer — full history of that person's evaluations | Drawer |
| Click "Evaluate" | Opens Start People Analyzer Evaluation modal | (Modal) |
| Click an evaluation row in drawer | Opens full Evaluation Detail | Drawer (nested or same) |

**Data dependencies:**
- `GET /api/people-analyzer` — admin view of all members with latest eval
- `GET /api/people-analyzer/{userId}` — full history for that user
- `POST /api/people-analyzer/{userId}` — create a new evaluation

**Key states:**
- **No evaluations yet:** "No evaluations yet. Click Evaluate on any member to start."
- **Loading / Error:** standard.

**Notes:** Members access their own evaluations through `/profile`, not here. This screen is admin-only.

---

#### Meeting Start (Pre-Meeting)

**Route:** `/meetings/start`
**Purpose:** Attendance check-in and meeting kickoff. Only reachable when no meeting is in progress; otherwise redirects to the live meeting.

**User arrives here from:**
- Dashboard → Start L10 (admin only)
- Direct route (admin only)

**What the user sees:**
- Team name and today's date
- "Who's here?" grid of team members with a checkbox per person. Current user is auto-checked.
- Start button (disabled until at least the current user is checked in)
- "Not ready — cancel" link returning to Dashboard

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Check/uncheck an attendee | Tracks attendance set locally | Stays |
| Click "Start L10" | Creates a new `Meeting` with `InProgress` status, records attendance, transitions everyone to the participant view | Active Meeting (Participant) |
| Click Cancel | No meeting created | Dashboard |

**Data dependencies:**
- `GET /api/teams/current/members`
- `POST /api/meetings` — creates the meeting with attendees

**Key states:**
- **Another meeting in progress:** page redirects to the in-progress meeting with a banner "There's already a meeting in progress. Join?"
- **Loading / Error:** standard.

---

#### Active Meeting — Participant View

**Route:** `/meetings/active`
**Purpose:** The primary UI during a live L10. One per attendee.

**User arrives here from:**
- Meeting Start → on Start
- Any route while a meeting is in progress → "Join" link in top-bar banner
- Deep link from digest email of a past meeting (redirects to archive if meeting no longer in progress)

**What the user sees:**
- **Top bar (meeting-scoped):** team name, current segment name, big segment timer (counting down), cumulative meeting timer, "End Meeting" button (facilitator/admin only)
- **Main content area (segment-dependent):**
  - *Segue:* freeform "good news" entry field per attendee (optional), visible to all
  - *Scorecard:* scorecard grid for the current week (see Scorecard screen, embedded)
  - *Rock Review:* current-quarter rocks list with inline status updates (see Rocks)
  - *Headlines:* list of customer/employee headlines (add/view)
  - *To-Do List:* to-dos from last week (see To-Dos)
  - *IDS:* short-term issues list with "active issue" emphasis + active issue detail panel; facilitator has "select as active" on any issue
  - *Conclude:* rating widget (1–10) per attendee, cascading messages entry, final "End Meeting" button
- **Right sidebar:** freeform meeting notes (rich text, visible to all, shared editing)
- **SignalR state indicators:** current segment, timer, active issue during IDS, other attendees' presence

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Facilitator: Click "Advance Segment" | Transitions to next segment; timer resets | Stays |
| Facilitator: Click a segment name in the top bar | Jumps to that segment | Stays |
| Add an issue during any segment | Opens Add Issue modal; new issue lands on short-term list | (Modal, stays) |
| Add a to-do during any segment | Opens Add To-Do modal | (Modal, stays) |
| Edit meeting notes | In-place rich text editing, synced via SignalR | Stays |
| Facilitator: Click an issue during IDS | Sets it as active issue (broadcast) | Stays |
| Facilitator: Click "Mark Solved" on active issue | Prompts solution notes, solves the issue, deactivates | Stays |
| Submit rating | Records rating + optional comment; cannot be re-submitted for same meeting | Stays |
| Add cascading message | Opens Add Cascading Message modal | (Modal, stays) |
| Facilitator: Click "End Meeting" | Confirmation dialog, then transitions meeting to `Completed`, triggers digest pipeline | Meeting Detail (archived) |

**Data dependencies:**
- `GET /api/meetings/active` — current in-progress meeting state
- `POST /hubs/meeting/connect` (SignalR) — receives events in real time
- `POST /api/meetings/{id}/segments/advance` — facilitator only
- `POST /api/meetings/{id}/notes` — upsert notes
- `POST /api/meetings/{id}/ratings` — submit rating
- `POST /api/meetings/{id}/cascading-messages`
- `PATCH /api/issues/{id}/status` (when solving active issue)
- `POST /api/issues` / `POST /api/todos` (inline adds during meeting)

**Key states:**
- **Late-join (user arrives after meeting started):** joins the hub, sees current state immediately
- **Connection lost:** banner "Reconnecting…" at top; UI stays on last known state until reconnect
- **Non-facilitator trying facilitator action:** button disabled with tooltip "Only the facilitator can advance segments."
- **Loading / Error:** minimal — this screen assumes fast join; failures are full-screen error with retry.

**Notes:** Highest-concurrency screen. SignalR message flow needs to be robust to quick-fire events (timer ticks throttled to 1Hz, other events unthrottled). No offline mode.

---

#### Active Meeting — Presenter View

**Route:** `/meetings/active?presenter=true`
**Purpose:** The in-room big-screen display during a live L10. Read-mostly; no interactive chrome.

**User arrives here from:**
- Manual navigation on the shared-display device (e.g., open the URL on the conference-room laptop connected to the TV)

**What the user sees:**
- **Full-bleed layout optimized for ~10-foot viewing distance:**
  - Top: current segment name (very large) and countdown timer (very large)
  - Center: the "active content" of the segment — during Scorecard, the grid; during Rocks, the list; during IDS, the active issue's full title and description; during Conclude, cumulative ratings
- No sidebars, no modals, no forms.
- Subscribes to the same `/hubs/meeting` SignalR stream.

**What the user can do:** Nothing interactive — it's a passive display. The only user "interaction" is navigating to the URL on the shared display.

**Data dependencies:** Same SignalR subscription as Participant view; subset of REST calls (read-only).

**Key states:**
- **No meeting in progress:** screen shows "No meeting in progress. Return to dashboard."
- **Connection lost:** full-screen banner "Reconnecting…"

**Notes:** The presenter variant of individual feature screens (Scorecard, Rocks, Issues, To-Dos) mentioned in their sections is a simpler "bigger font, less chrome" render used when someone opens that specific section on the shared display outside of a live meeting. The full Active Meeting Presenter view is specifically for in-meeting use and automatically follows segment changes.

---

#### Meeting History

**Route:** `/meetings`
**Purpose:** List of past and in-progress meetings.

**User arrives here from:**
- Sidebar → Meetings

**What the user sees:**
- Table of meetings: columns for Date, Duration, Attendance Count, Average Rating, Status, Actions
- Filters: date range, attendee, rating threshold
- "Start L10" button (admin only, disabled if a meeting is in progress)

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click a meeting row | Opens Meeting Detail | Meeting Detail |
| Click "Start L10" | Opens Meeting Start flow | Meeting Start |
| Filter / paginate | Reloads table | Stays |

**Data dependencies:**
- `GET /api/meetings?from={date}&to={date}&attendee={id}&minRating={n}`

**Key states:**
- **No past meetings:** "No meetings yet. Admins can start the first L10 from the dashboard." + CTA (admin).
- **Meeting in progress:** highlighted row at the top with "Join" link (to Active Meeting).
- **Loading / Error:** standard.

---

#### Meeting Detail (Archive)

**Route:** `/meetings/{id}`
**Purpose:** Read-only archive of a past meeting.

**User arrives here from:**
- Meeting History → click row
- Digest email → deep link
- Active Meeting → End Meeting transitions here

**What the user sees:**
- Header: date, duration, attendees, average rating
- Section-by-section summary matching the original agenda:
  - Segue entries
  - Scorecard snapshot for that week
  - Rocks status at time of meeting
  - Headlines
  - To-Dos created during the meeting
  - IDS — issues solved, issues added, active-issue timeline
  - Cascading messages
  - Freeform meeting notes
  - Individual ratings
- Download PDF button

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Click Download PDF | Downloads the rendered PDF for this meeting | Stays |
| Click an attendee | Jumps to their profile (if permitted) | Profile |
| Click an issue referenced | Jumps to the current state of that issue (not the historical state) | Issue detail |

**Data dependencies:**
- `GET /api/meetings/{id}` — the fully archived state
- `GET /api/meetings/{id}/pdf` — streams the rendered PDF

**Key states:**
- **PDF not yet generated** (edge case, right after meeting close): "Generating PDF…" with poll.
- **Loading / Error:** standard.

---

#### Audit Log (Admin only)

**Route:** `/admin/audit-log`
**Purpose:** Searchable view of every audited change.

**User arrives here from:**
- Sidebar Admin → Audit Log (admin-only sidebar group)

**What the user sees:**
- Filters: entity type, entity id, actor (user), date range, operation type (Create/Update/Delete), field name
- Paginated table: timestamp, actor, entity type + id, operation, field, old value, new value
- Export CSV button

**What the user can do:**
| Action | Behavior | Navigates To |
|---|---|---|
| Apply filters | Reloads table | Stays |
| Click entity id cell | Jumps to the current state of that entity (if still exists) | Respective detail |
| Click actor cell | Jumps to that user's profile | Profile |
| Click Export CSV | Downloads filtered results | Stays |

**Data dependencies:**
- `GET /api/audit-entries?filters…&page=&pageSize=` — paginated (default 25 per page, max 100 per standards)

**Key states:**
- **No matches for filter:** "No audit entries match your filters."
- **Large result set:** standard pagination per TI standards.

---

### Modals & Overlays

---

#### Invite Member

**Triggered from:** Team Settings → "Invite Member"
**Purpose:** Add an email to the allowed list for the team.

**Fields:**
- Email (text, required, email-format validation)
- Role (dropdown: Member or Admin, default Member)
- Display name (optional; populated from Entra on first login if blank)

**Actions:**
| Action | Behavior |
|---|---|
| Invite | `POST /api/teams/current/members`, closes modal, refreshes members table, shows success toast |
| Cancel | Closes, no changes |

**Validation:**
- Email must not already be a member of this team
- Email must be valid RFC 5322

---

#### Add / Edit Metric

**Triggered from:** Scorecard → "Add Metric" or Metric Detail drawer → "Edit"
**Purpose:** Define or edit a scorecard metric.

**Fields:**
- Name (text, required, max 100)
- Owner (user picker, required)
- Goal value (decimal, required)
- Comparison operator (dropdown: `>= goal is green` or `<= goal is green`, default `>=`)
- Unit label (text, optional, max 20)
- Active (checkbox, default true)

**Actions:**
| Action | Behavior |
|---|---|
| Save | Upsert, close modal, refresh grid |
| Cancel | Close, no changes |

**Validation:**
- Name unique within the team's active metrics

---

#### Add / Edit Rock

**Triggered from:** Rocks → "Add Rock" or Rock Detail drawer → "Edit"
**Purpose:** Define or edit a quarterly rock.

**Fields:**
- Title (text, required, max 200)
- Description (rich text via ngx-quill, optional)
- Owner (user picker, required)
- Quarter (year + quarter number, required; defaults to current quarter when adding)
- Initial status (dropdown, default "On Track" when adding; hidden when editing — use status action in drawer instead)
- Milestones (dynamic list: each has description, target date, completed flag)

**Actions:**
| Action | Behavior |
|---|---|
| Save | Upsert, close modal, refresh list |
| Cancel | Close, no changes |

**Validation:**
- Title non-empty
- Owner must be a current team member
- Milestone dates must be within the rock's quarter (warning not error)

---

#### Add / Edit Issue

**Triggered from:** Short-Term Issues → "Add Issue"; Long-Term Issues → "Add Issue"; Active Meeting (inline); Issue Detail drawer → "Edit"
**Purpose:** Create or edit an issue.

**Fields:**
- Title (text, required, max 200)
- Description (rich text, optional)
- List type (radio: Short-Term / Long-Term, defaults to the context the modal was opened from)
- Owner/Raised-By (user picker, defaults to current user when adding)

**Actions:**
| Action | Behavior |
|---|---|
| Save | Upsert, close, refresh list |
| Cancel | Close, no changes |

**Validation:**
- Title non-empty

---

#### Add / Edit To-Do

**Triggered from:** To-Dos → "Add To-Do"; Active Meeting (inline); To-Do Detail drawer → "Edit"
**Purpose:** Create or edit a to-do.

**Fields:**
- Title (text, required, max 200)
- Description (plain text, optional)
- Owner (user picker, required, defaults to current user when adding)
- Due date (date picker, required, defaults to 7 days from today when adding)

**Actions:**
| Action | Behavior |
|---|---|
| Save | Upsert, close, refresh list |
| Cancel | Close, no changes |

**Validation:**
- Title non-empty
- Due date in the future when adding (warning not error if past)

---

#### Add / Edit Seat

**Triggered from:** Accountability Chart → "Add Seat" or Seat Detail drawer → "Edit"
**Purpose:** Create or edit an accountability chart seat.

**Fields:**
- Seat name (text, required, max 100)
- Parent seat (seat picker, optional; empty for root seats)
- 5 roles (5 text fields, optional individually; at least one required)
- Assignee (user picker, optional; empty for unassigned seats)

**Actions:**
| Action | Behavior |
|---|---|
| Save | Upsert, close, refresh tree |
| Cancel | Close, no changes |

**Validation:**
- Seat name non-empty
- Cannot set parent to a descendant of self (prevents cycles)

---

#### Add / Edit Core Value

**Triggered from:** V/TO Core Values section → "Add Core Value" or clicking a value (admin)
**Purpose:** Create or edit a core value.

**Fields:**
- Label (text, required, max 60)
- Description (text, optional, max 500)
- Display order (number, auto-assigned but editable)

**Actions:**
| Action | Behavior |
|---|---|
| Save | Upsert, close, refresh V/TO |
| Cancel | Close, no changes |

---

#### Start People Analyzer Evaluation

**Triggered from:** People Analyzer → "Evaluate" on a member
**Purpose:** Capture a new evaluation for a team member.

**Fields:**
- Evaluation date (date picker, defaults to today)
- Per core value: rating (+, ±, −)
- GWC:
  - Gets it (Yes / No / Sometimes)
  - Wants it (Yes / No / Sometimes)
  - Capacity (Yes / No / Sometimes)
- Notes (rich text, optional, admin-only visible)

**Actions:**
| Action | Behavior |
|---|---|
| Save | `POST /api/people-analyzer/{userId}`, close, refresh grid |
| Cancel | Close, no changes |

---

#### Add Cascading Message

**Triggered from:** Active Meeting (Conclude segment) → "Add Cascading Message"
**Purpose:** Capture a message to cascade after the meeting.

**Fields:**
- Audience (text, required — e.g., "All OCC", "Engineering", "Sales")
- Message (text, required)
- Owner (user picker, defaults to current user — the one responsible for cascading it)

**Actions:**
| Action | Behavior |
|---|---|
| Save | Attaches to current meeting, broadcasts via SignalR to all attendees |
| Cancel | Close, no changes |

---

#### Submit Rating

**Triggered from:** Active Meeting (Conclude segment) automatically or via "Submit Rating" button
**Purpose:** Capture this attendee's meeting rating.

**Fields:**
- Rating (1–10 integer, required; rendered as a row of 10 buttons)
- Comment (text, optional, max 500)

**Actions:**
| Action | Behavior |
|---|---|
| Submit | Saves, disables the widget (one rating per attendee per meeting), shows thanks |
| Cancel | Closes modal without saving |

**Validation:**
- Rating 1–10

---

#### Confirm Delete (Shared)

**Triggered from:** any destructive action in detail drawers
**Purpose:** Require explicit confirmation before a soft delete.

**Fields:**
- Confirmation text configurable by caller (e.g., "Delete seat 'Head of Marketing' and reassign children to its parent?")

**Actions:**
| Action | Behavior |
|---|---|
| Confirm | Proceeds with the caller's deletion logic |
| Cancel | Close, no changes |

---

### Screen Map (Navigation Flow)

```
/login
  └── [success] → /dashboard
  └── [unauthorized email] → /login (with error banner)

/dashboard
  ├── [sidebar: V/TO]             → /vto
  ├── [sidebar: Accountability]   → /accountability-chart
  ├── [sidebar: Scorecard]        → /scorecard
  ├── [sidebar: Rocks]            → /rocks
  ├── [sidebar: Issues]
  │     ├── [short-term]          → /issues/short-term
  │     └── [long-term]           → /issues/long-term
  ├── [sidebar: To-Dos]           → /todos
  ├── [sidebar: People Analyzer]  → /people-analyzer (admin-only)
  ├── [sidebar: Meetings]         → /meetings
  ├── [sidebar Admin: Team]       → /team/settings (admin-only)
  ├── [sidebar Admin: Audit Log]  → /admin/audit-log (admin-only)
  ├── [top-bar user menu: Profile] → /profile
  ├── [card: Start L10] (admin)   → /meetings/start → /meetings/active
  └── [meeting in progress banner] → /meetings/active

/meetings/start
  ├── [Start L10]                 → /meetings/active (participant view)
  └── [Cancel]                    → /dashboard

/meetings/active  (and ?presenter=true for big-screen)
  └── [End Meeting] (admin)       → /meetings/{id} (archive)

/meetings
  ├── [click row]                 → /meetings/{id}
  └── [Start L10] (admin)         → /meetings/start

/meetings/{id}
  └── [Download PDF]              → streamed PDF file

/vto
  ├── [Rocks summary link]        → /rocks
  └── [Issues summary link]       → /issues/short-term

(Every feature screen: Dashboard quick-actions + sidebar primary nav. Presenter variants reached via ?presenter=true query on the same route.)
```

---

### Shared Components

| Component | Used On | Description |
|---|---|---|
| **App Shell** | All authenticated screens | Top bar (app name, team name, user menu), left sidebar (nav), main content area. Includes presence-aware "Meeting in progress — Join" banner at top when a meeting is in progress and current user is an attendee. |
| **Empty State** | Every screen | Illustration + one-sentence hint + primary CTA. CTA disabled (with tooltip) when current user lacks permission. |
| **Loading Skeleton** | Every data-bound screen | Grayscale placeholder blocks matching the final layout shape. |
| **Error Banner** | Every screen | Inline dismissible banner above content with optional retry button. Shown for request failures. |
| **Confirmation Modal** | Every destructive action | Shared reusable with configurable title, body, confirm label. |
| **User Avatar / Initials** | Everywhere a user is referenced | Circular badge with initials. Hovercard shows full name + email. |
| **Status Pill** | Rocks, Issues, To-Dos, Meetings | Small colored pill: On Track (green), Off Track (red), Done (gray), Not Done (red), Open (blue), Solved (green), Dropped (gray), Overdue (red), In Progress (yellow), Completed (green). |
| **Segment Timer** | Active Meeting (participant & presenter) | Countdown display with color shift when exceeded. Fed by SignalR `TimerTick`. |
| **Rating Widget** | Active Meeting Conclude, Meeting Detail | 10 buttons labeled 1–10; selected state persists; optional comment box. |
| **Period Switcher** | Scorecard, Meeting History | Tab-style control for selecting trailing 4 / 13 / 26 / 52 weeks. |
| **Quarter Tabs** | Rocks | Previous / Current / Next quarter with labels. |
| **User Picker** | All modals that reference a user | Searchable dropdown of current team members. |
| **Seat Detail Drawer / Rock Detail Drawer / Issue Detail Drawer / To-Do Detail Drawer / Metric Detail Drawer** | Respective list screens | Right-hand side drawer showing detail + audit trail + actions. Closable with Escape or X. |
| **Theme Toggle** | User menu, Profile screen | Dark / Light / System. Persists to user settings (server side) plus fallback in local state for first paint. |
| **Presenter Layout Wrapper** | All screens supporting a presenter variant | When `?presenter=true`, a wrapper strips chrome, upsizes typography, and disables interactive controls. |

---

## Completion Checklist

- [x] Every screen the user can navigate to is listed
- [x] Every modal/overlay is defined with its trigger
- [x] Navigation paths between screens are documented
- [x] Data dependencies (API endpoints) are identified for each screen
- [x] Empty, loading, and error states are considered
- [x] Shared components are identified to avoid duplication
- [x] Screen map reflects the full navigation graph
- [x] Presenter view variants distinguished from participant views
- [x] Admin-only screens and admin-only controls on shared screens are flagged
