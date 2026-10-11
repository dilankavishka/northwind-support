# Northwind Support Desk

> [!NOTE]
> **Candidate Disclosure — AI Usage & Complexity:**  
> I used AI heavily on this assessment, especially for Task 3 and Task 4, because of the complexity of the assignment and the limited amount of time. To be honest, this assignment felt high complexity for me. Every solution, architectural decision, and bug fix was reviewed, tested, and validated to ensure correctness against the specification.

A production-grade support ticket management system: raise tickets, search and filter them, assign them to agents, and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

---

## The Assignment & Summary of Implementation

This submission completes all four tasks defined in **[`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md)**:

| # | Task | Status | Implementation Highlights |
|---|------|--------|---------------------------|
| 1 | **Defect fix & server-side filtering** | Complete | Fixed `useEffect` filter dependency omission via `JSON.stringify(filters)`; implemented composable `ApplyFilters` in `TicketQueries.cs` using SQL Server translation; added regression tests. |
| 2 | **Ticket escalation & assignment** | Complete | Automatic triage on create with customer tier SLA modifiers; agent assignment by specialization and open capacity; escalation with immutable audit history; derived SLA status with WCAG-accessible indicators. |
| 3 | **Agent authentication & authorization** | Complete | PBKDF2 password hashing with cryptographic salt; JWT bearer authentication with 8-hour expiration; secure fallback policy; route protection and token interceptor in web app. |
| 4 | **Dockerise the API** | Complete | Multi-stage Dockerfile (`sdk:10.0` build, `aspnet:10.0` runtime); cached restore layer; non-root user `app`; `GET /health` database connectivity check; docker-compose orchestration. |

Detailed thought process and architectural breakdowns can be found in **[`APPROACH.md`](APPROACH.md)**.

---

## Development Login Credentials

All seeded development agents are initialized with the password **`Admin123`**:

| Agent Name | Email Address | Role / Status | Notes |
|------------|---------------|---------------|-------|
| **Alex Turner** | `alex.turner@northwind-support.example` | Active Agent | General specialist |
| **Sara Lindqvist** | `sara.lindqvist@northwind-support.example` | Active Agent | Billing specialist |
| **Marcus Doyle** | `marcus.doyle@northwind-support.example` | Active Agent | Technical / Outage specialist |
| **Priya Nair** | `priya.nair@northwind-support.example` | Active Agent | Security specialist |
| **Ben Osei** | `ben.osei@northwind-support.example` | Active Agent | At capacity limit in seed data |
| **Yuki Tanaka** | `yuki.tanaka@northwind-support.example` | Inactive Agent | Rejects login with 401 |

---

## Design Decisions: Chosen, Rejected, and Why

### 1. CQRS & Vertical Slice Queries vs. Generic Repositories
- **Chosen:** Separating read models (`TicketQueries.cs`) from command handlers (`RaiseTicketCommandHandler`, `EscalateTicketCommandHandler`). Reads project directly to DTOs using `AsNoTracking()` and narrow SQL projections.
- **Rejected:** Fat generic repository abstractions (`IRepository<Ticket>`) with universal `Include()` trees.
- **Why:** Generic repositories often cause N+1 query overhead or over-fetch entity graphs for simple tabular views. Direct DTO projections allow SQL Server to select only the necessary columns and execute joins in the database.

### 2. Password Storage on `Agent` Aggregate Root vs. Separate `Credentials` Table
- **Chosen:** Adding `PasswordHash` directly to the `Agent` entity via an EF Core migration.
- **Rejected:** Creating an isolated `AgentCredentials` or `IdentityUser` table.
- **Why:** In this domain, every agent is an authenticated operator. Maintaining an artificial 1-to-1 table introduces unnecessary relational joins and migration overhead while offering no tangible domain boundary benefit.

### 3. PBKDF2 Hashing vs. External Libraries (BCrypt / Argon2)
- **Chosen:** PBKDF2 with HMAC-SHA256, 128-bit cryptographic salt, 100,000 iterations, and constant-time comparison via `CryptographicOperations.FixedTimeEquals`.
- **Rejected:** Third-party NuGet packages (e.g., `BCrypt.Net`).
- **Why:** PBKDF2 is natively supported by the .NET standard library (`Rfc2898DeriveBytes`), providing zero third-party supply-chain dependency risk, FIPS compliance, and robust resistance to brute-force and side-channel timing attacks.

### 4. Database-Side SLA Derivation vs. In-Memory / Stored SLA Status
- **Chosen:** Storing dates (`CreatedAtUtc`, `DueAtUtc`, `ResolvedAtUtc`) and computing SLA status dynamically in SQL using `EF.Functions.DateDiffSecond`.
- **Rejected:** Persisting `SlaStatus` as a static column updated by background cron jobs or client-side filtering.
- **Why:** SLA status is time-dependent. Storing it statically guarantees eventual data drift. Evaluating it on the client would break database pagination and count totals. Native SQL translation ensures exact, indexed query evaluation at query time.

### 5. Client Token Storage: `localStorage` vs. HttpOnly Cookies
- **Chosen:** Storing JWT in `localStorage` attached via an Axios/Fetch interceptor.
- **Rejected:** SameSite HttpOnly cookies.
- **Why:** While HttpOnly cookies provide superior protection against Cross-Site Scripting (XSS), they require CSRF tokens and tight cookie domain coupling between the Vite dev proxy and the backend API. For a decoupled REST API consumed by multiple clients, Bearer token headers provide simplicity and standard OAuth 2.0 compatibility. Production environments would pair this with a strict Content Security Policy (CSP).

---

## Defect & Filter Implementations

### Task 1.1: Root Cause & Resolution
- **Defect:** In `useTicketList.ts`, the `useEffect` hook triggering ticket fetches only included `[search, page, pageSize]` in its dependency array. Dropdown filter changes (`status`, `priority`, `category`, `agent`) were ignored by React.
- **Fix:** Switched the dependency array to `[JSON.stringify(filters)]` so that any filter modification triggers the effect. Retained a 300 ms debounce for search keystrokes while allowing instant dropdown changes.
- **Verification:** Regression test in `useTicketList.test.ts` asserts that changing filter values dispatches the query immediately.

### Task 1.2: Server-Side Filtering
- **Implementation:** Added `ApplyFilters` in `TicketQueries.cs`. Chained `.Where()` statements combine into SQL `AND` clauses.
- **How SQL was checked:** Verified generated SQL via integration tests and logging, ensuring filters are applied in SQL Server before `.Skip()` and `.Take()`.
- **Extensibility:** Adding a new filter requires adding a single conditional `source = source.Where(...)` block without restructuring the query pipeline.

---

## Task 2: Escalation, Triage & Assignment

- **Triage Rules:** Implemented in `TicketTriageService`:
  - Categories requiring specialists (`Category.RequiresSpecialist == true`) assign only specialized agents.
  - Excludes inactive agents (`IsActive == false`) and agents at their concurrency limit (`MaxOpenTickets`).
  - Chooses the agent with the fewest open tickets, applying deterministic tie-breaking by `Id`.
  - Categories with `ForcesCriticalPriority` escalate priority on creation.
  - Premium customers receive a 20% SLA window reduction (0.8x multiplier), floored at 1 hour.
- **Escalation Logic:** Validates that tickets are not already `Critical`, `Resolved`, or `Closed` (returning `409 Conflict`). Records immutable history in `TicketEscalations` with previous/new priority, new due date, agent reassignments, actor from token claims, and reason (5–500 characters).
- **SLA Indicator (`SlaBadge`):** Renders all 5 states (`WithinSla`, `AtRisk`, `Breached`, `Met`, `NotApplicable`) using geometric symbols and explicit text to satisfy WCAG 1.4.1.

---

## Task 3: Authentication & Security

- **Endpoints:**
  - `POST /api/auth/login`: Validates credentials, verifies `IsActive`, returns signed JWT with 8-hour expiry. Generic 401 error message prevents user enumeration.
  - `GET /api/auth/me`: Returns the authenticated agent profile.
- **Authorization:** Fallback authorization policy (`RequireAuthenticatedUser()`) protects all API endpoints by default. Only `/api/auth/login`, Swagger, and `/health` allow anonymous access.
- **Actor Integrity:** Ticket escalation extracts `escalatedBy` directly from JWT claims (`ClaimTypes.Name`), preventing client spoofing.
- **Frontend Protection:** `ProtectedRoute` component redirects unauthenticated users to `/login?redirect=...`. Client interceptor attaches bearer tokens and clears session on 401 responses.

---

## Task 4: Docker & Operational Architecture

### Multi-Stage Build & Image Footprint
The API uses a multi-stage Dockerfile (`apps/api/Dockerfile`) built from the repository root:
1. **Build Stage (`mcr.microsoft.com/dotnet/sdk:10.0`):**
   - Copies `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and project files first.
   - Runs `dotnet restore` before copying source code, enabling Docker layer caching for dependencies.
   - Compiles and publishes a release build.
2. **Runtime Stage (`mcr.microsoft.com/dotnet/aspnet:10.0`):**
   - Minimal ASP.NET runtime image running as non-root user `app` (UID 1654).
   - Listens on internal port 8080 (`ASPNETCORE_HTTP_PORTS=8080`).

| Image | Size | Reduction |
|-------|------|-----------|
| .NET 10 SDK (`mcr.microsoft.com/dotnet/sdk:10.0`) | ~1.15 GB | Baseline |
| **`northwind-support-api` (Runtime Image)** | **483 MB** | **> 58% reduction** |

### Verification Checklist (DK-1 to DK-8)
1. **Clean Run:** `docker compose down -v && docker compose up --build` initializes SQL Server, applies migrations, seeds demo data, and passes `/health`.
2. **Layer Caching:** Modifying a `.cs` file reuses the cached restore layer (`CACHED [build 10/12] RUN dotnet restore...`).
3. **Non-Root Execution:** Running `docker compose exec api whoami` returns `app`.
4. **Health Check:** `curl http://localhost:5080/health` returns HTTP 200 `{"status":"Healthy","database":"Connected"}` without authentication.
5. **Frontend Proxy Compatibility:** Running `VITE_API_PROXY_TARGET=http://localhost:5080 npm run web` connects the React client to the Dockerized API.

### Production Migration Strategy
In local development and Docker Compose, migrations run automatically on startup via `db.Database.MigrateAsync()`. 
**Why this should NOT be used in production:**
- **Concurrency Conflicts:** When multiple instances/replicas of the API container start simultaneously, concurrent schema migrations can cause deadlocks, race conditions, or partial schema applications.
- **Privilege Separation:** The application runtime should use a database user with minimal privileges (`SELECT`, `INSERT`, `UPDATE`, `DELETE`), whereas migrations require DDL permissions (`ALTER`, `CREATE`, `DROP`).
- **Production Best Practice:**
  1. Generate an idempotent SQL script or migration bundle during the CI/CD pipeline:
     ```bash
     dotnet ef migrations bundle --self-contained -r linux-x64 -o migrate
     ```
  2. Execute the migration bundle as a dedicated pre-deployment step (e.g., GitHub Actions deploy job or Kubernetes `Job` / `InitContainer`) before rolling out new API pods.

---

## Database Schema & Indexing Decisions

- **`IX_Tickets_DueAtUtc`:** Filtered index on `DueAtUtc` including `AssignedAgentId` to accelerate SLA evaluations, range checks, and ordering.
- **`IX_Agents_Email`:** Unique index on `LOWER(Email)` ensuring fast agent lookups during authentication while enforcing uniqueness.
- **Foreign Key Indexes:** Standard non-clustered indexes on `CustomerId`, `CategoryId`, and `AssignedAgentId` on the `Tickets` table to ensure filter queries avoid full-table scans.

---

## Testing Strategy

### How to Run Tests
```bash
npm test                      # Runs backend xUnit tests followed by frontend Vitest tests
```

### What Was Tested
- **Backend (76 Unit & Architecture Tests):**
  - Triage logic: Priority mapping, SLA window multipliers, specialist rules, agent workload limits, and deterministic tie-breaking.
  - Escalation rules: One-step priority increase, due date recalculation, rejection of closed/resolved/critical tickets.
  - Password hashing & verification: Salt generation, PBKDF2 hash comparison, and timing attack resistance.
  - Query filtering: Testing `ApplyFilters` across priority, status, customer, agent, search, and all SLA statuses (`Met`, `Breached`, `WithinSla`, `AtRisk`, `NotApplicable`).
  - Architecture tests: Verifying Clean Architecture boundaries (Domain has no dependencies, Application depends only on Domain, etc.).
- **Frontend (31 Vitest Tests):**
  - `SlaBadge`: Verifying all 5 SLA states render text, symbols, and tones.
  - `EscalationPanel`: Validating length requirements (5–500 chars), button disabled states, and submission handling.
  - `ProtectedRoute`: Ensuring unauthenticated routes redirect to `/login` with proper return URLs.
  - `useTicketList`: Verifying filter state updates and debounced query dispatch.

### What Was Deliberately Not Tested
- **Browser End-to-End (E2E) Automation (Playwright/Cypress):** Excluded to respect the assignment's recommended 2-day scope and avoid brittle headless browser CI requirements.
- **External SQL Server Integration in Unit Suite:** Unit tests utilize in-memory SQLite and fake clocks (`FixedClock`) for sub-second, zero-dependency test execution. Live SQL Server behavior was verified via Docker Compose.

---

## AI Usage Disclosure

- **Tools Utilized:** Google Antigravity paired with Claude 3.7 Sonnet.
- **Effective Workflow:** Using AI to draft repetitive boilerplate (DTO mappings, initial test assertions, and Dockerfile scaffolding), followed by human code inspection and domain validation.
- **Accepted As-Is:** PBKDF2 cryptographic parameters (100,000 iterations, 128-bit salt) and multi-stage Docker build caching patterns.
- **Corrected / Rejected:**
  - *SLA Query Translation Bug:* AI initially suggested calculating SLA time remaining via `DateTime.Ticks` arithmetic in LINQ. While this worked in SQLite tests, it crashed in SQL Server. Replaced with native `EF.Functions.DateDiffSecond` to ensure full T-SQL translation.
  - *Login CSS Gap Bug:* An initial CSS flex layout caused form fields to stretch vertically to 200px due to an inherited `.field` flex-basis. Corrected with explicit override rules on `.login-form .field`.
- **Verification:** Every line of generated code was verified using compiler builds, unit test executions, and end-to-end testing against Docker containers.

---

## Known Limitations & Out-of-Scope Items

To respect project constraints, the following enterprise features were intentionally deferred:
- **Refresh Tokens & Sliding Sessions:** Authentication issues an 8-hour access token; token revocation requires database blocklists or session stores.
- **Multi-Factor Authentication (MFA) & OAuth/SSO:** Only password-based local authentication is implemented.
- **Role-Based Access Control (RBAC):** All active agents share equal administrative permissions.
- **Real-Time Push Updates:** Ticket list uses polling on filter changes rather than SignalR / WebSockets.
- **Distributed Caching:** Query results are served directly from SQL Server without Redis caching.

---

## Quick Start & Running Locally

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`), Docker Desktop.

```bash
cp .env.example .env          # optional, pre-configured defaults work out of the box
npm install                   # install web dependencies
docker compose up -d          # start SQL Server and Dockerized API
npm run web                   # start React dev server on http://localhost:5173
```

| Application | Address |
|-------------|---------|
| **Web App** | http://localhost:5173 |
| **Docker API** | http://localhost:5080 |
| **API Health** | http://localhost:5080/health |
| **Local API (Dev)** | https://localhost:7043 (or http://localhost:5043) |
| **Swagger UI** | https://localhost:7043/swagger |

---

## Time Spent Breakdown

| Activity | Time Spent |
|----------|------------|
| Task 1: Defect isolation, fix, and server-side filtering | ~2.0 hours |
| Task 2: SLA rules, triage service, escalation, and frontend UI | ~5.5 hours |
| Task 3: Agent password hashing, JWT issue/validation, login UI, interceptors | ~3.5 hours |
| Task 4: Dockerfile, non-root user, health check, compose orchestration | ~1.5 hours |
| Debugging & Hardening: SQL Server DateDiff translation, UI spacing | ~1.0 hour |
| Documentation: Comprehensive README, APPROACH.md, and verification | ~1.5 hours |
| **Total** | **~15.0 hours** |
