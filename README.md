# Northwind Support Desk

A support ticket management system: raise tickets, search and filter them, assign them to agents,
and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

---

## The assignment

You are joining a team that already has this app working. Complete the tasks **in order**.
Full requirements are in **[`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md)**.

| # | Task | Done looks like | Effort |
| --- | --- | --- | --- |
| 1 | **Fix a defect, add server-side filtering** | Root cause fixed with a regression test; every list filter applied in the database with a correct count | ~2.5 h |
| 2 | **Ticket escalation & assignment** | Automatic triage on create, escalation with history, SLA status, UI for all of it | ~7 h |
| 3 | **Login and authentication** | Agents sign in; API rejects unauthenticated calls; login UI | ~4 h |
| 4 | **Dockerise the API** *(optional)* | `docker compose up` gives a migrated, seeded API with no baked-in secret | ~1.5 h |

### Guidance

- **Time: 2 days.** Honestly marking a task "not finished" beats rushing all of them.
- **Fork** this repo to your own GitHub account and work there. **Do not push to the original.**
- **AI tools are allowed.** You must understand and be able to defend every line you submit.
- **Commit as you go** and keep the real history. Commit the Task 1 defect fix and the filtering
  separately from each other and from the feature work. A single squashed commit is not accepted.
- **Where the spec is ambiguous**, make a decision, write it down in the README, and move on.
- **Out of scope:** registration, password reset, roles, OAuth/SSO, refresh tokens, MFA,
  notifications, CI, caching, visual redesign. Mention them under *Known limitations* instead.

### Submission

1. Update this **README** with what you built, your design decisions (chosen, rejected and why),
   the defect's root cause, dev login credentials, how to run and test it, AI usage, and known
   limitations. A reviewer must be able to run it without asking you anything.
2. Add an **APPROACH.md** explaining your approach and thought process for each task.
3. Commit both, then send us the **link to your fork**, the **dev login credentials**, and a
   **rough note of time spent** on what. (If the fork is private, add the reviewers as collaborators.)

---
## Root cause explanations

### Task 1.1

The `useEffect` hook in `useTicketList.ts` that triggers the API call only had `search`, `page`, and `pageSize` in its 
dependency array. It was ignoring all other filter changes, so React never fired the effect when a dropdown was changed.

#### How I fixed this?
I changed the dependency array to use `JSON.stringify(filters)` so that any change to the filters object automatically 
triggers the effect. Added a test case for this issue as well.

### Task 1.2

Filtering is applied in `TicketQueries.cs` via a new `ApplyFilters` helper method.

#### How I checked the SQL
The `IQueryable` delays execution until enumeration. By using EF Core's translation, multiple `.Where()` clauses safely 
combine into `AND` operators in the generated SQL. Verified with the newly added integration tests.

#### How I added the next filter
I just add one more `if (query.SlaStatus.HasValue)` block inside the `ApplyFilters` method.

### Task 2.1 & 2.2 (Backend)

- Implemented `TicketTriageService` to calculate SLAs based on category priorities and customer tiers.
- Automated ticket assignment factoring in Agent capacity and category specializations.
- Configured SLA business rules using `IOptions<SlaOptions>`.
- Built domain logic and API endpoint (`POST /api/tickets/{id}/escalate`) for ticket escalation with immutable 
- history tracking.
- Updated `SlaEvaluator` to dynamically calculate `AtRisk` SLA statuses based on a 25% remaining window rule.

### Task 2.3 & 2.4 (Frontend)

- Reusable component rendering all 5 SLA states (`WithinSla`, `AtRisk`, `Breached`, `Met`, `NotApplicable`) with 
explicit text and distinct geometric symbols.
- Added SLA status dropdown in `TicketFilters.tsx` wired to the server-side `slaStatus` parameter.
- Client-side validation enforcing 5–500 character reason length before dispatching network requests. Disabled with 
explanatory guidance when ticket is already `Critical`, `Resolved`, or `Closed`. Displays server errors.
- Immutable audit trail showing previous/new priority, agent reassignment, recalculated due date, reason, actor, 
and timestamp. Refreshes ticket and history in-place without a full page reload.
- Notice banner displaying automated triage decisions: applied priority, SLA window & due date, assigned agent, 
and the business rule explanation.

---

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`; 20.19+ works), Docker.

```bash
cp .env.example .env          # optional, every value has a working default
npm install                   # installs the web app
npm run db:up                 # SQL Server 2022 in Docker on localhost:1433
npm run api                   # terminal 1: migrates, seeds, then serves the API
npm run web                   # terminal 2: React dev server
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger |

In Development the API applies migrations and seeds demo data on startup. **Never commit real
credentials.**

**Visual Studio 2026:** run `npm run db:up`, open `SupportDesk.sln`, set
**SupportDesk.Presentation** as the startup project, and press F5 on the **https** profile.

## Tests

```bash
npm test                      # dotnet test, then the web tests (Vitest)
```

Both pass on a clean checkout and neither needs a database.

## Commands

| Command | What it does |
| --- | --- |
| `npm run db:up` / `db:down` / `db:reset` | Start / stop / wipe SQL Server |
| `npm run api` / `npm run web` | Run the API / the React app |
| `npm run build` | Build both |
| `npm run lint` / `typecheck` / `format` | Web lint and types / `dotnet format` |
| `npm run ef -- migrations add <Name>` | Add an EF Core migration |

## API at a glance

| Endpoint | Description |
| --- | --- |
| `GET /api/tickets` | Paged list with `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `sortBy`, `sortDirection`. |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket |
| `PATCH /api/tickets/{id}/status` · `/assignment` | Change status / assignee |
| `POST /api/tickets/{id}/escalate` | Escalate a ticket |
| `GET /api/tickets/{id}/escalations` | History of escalations |
| `GET /api/customers` · `/api/agents` · `/api/categories` | Reference data |

Errors are `ProblemDetails`: `400` validation, `404` not found, `409` conflict or business rule.

**Seed data:** 5 categories, 6 customers (3 Premium), 6 agents (one inactive, one at their limit)
and 40 tickets across every status, priority and SLA state.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API cannot reach the database | `npm run db:up`, wait ~20 s, check `docker compose ps` |
| Port 1433 already in use | Change the port in `docker-compose.yml` and `.env` |
| Browser warns about the certificate | `dotnet dev-certs https --trust`, or use the web app, which proxies |
| Web app shows "Unable to load tickets" | The API is not running, or `VITE_API_PROXY_TARGET` points at the wrong port |
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |
