# Architectural & Engineering Approach

This document outlines the thought process, architectural decisions, and problem-solving methodology applied throughout the Northwind Support Desk assessment.

---

## Task 1: Defect Fix & Server-Side Filtering

### 1.1 Defect Analysis & Resolution
- **Thought Process:** 
  The ticket list was failing to re-query the backend when filter dropdowns (Status, Priority, Category, Agent) were updated. Looking at `useTicketList.ts`, the `useEffect` hook triggering data fetches listed only `search`, `page`, and `pageSize` in its dependency array. The hook was unaware of filter changes because `filters` was not included.
- **Decision:**
  Instead of passing individual filter properties into the dependency array (which would require updating every time a new filter is introduced), `JSON.stringify(filters)` was utilized as a stable value comparator for the filter object. A debounce mechanism (`DEBOUNCE_MS = 300`) was preserved so typing in the search box does not overwhelm the server, while dropdown changes trigger an immediate request.
- **Verification:**
  Created a regression test in `useTicketList.test.ts` verifying that changing filter criteria triggers the expected API fetch call.

### 1.2 Server-Side Filtering Architecture
- **Thought Process:**
  List filtering must happen inside SQL Server before pagination and count calculation to avoid N+1 queries, memory bloat, and incorrect paging counts.
- **Decision:**
  Implemented `ApplyFilters` in `TicketQueries.cs`. By chaining LINQ `.Where()` calls on `IQueryable<TicketWithLabels>`, Entity Framework Core dynamically combines all active filters into a single SQL query using `AND` operators.
- **Extensibility:**
  Adding subsequent filters (such as Task 2's `slaStatus`) requires only a single additional conditional check within `ApplyFilters`, keeping query composition modular and adhering to the Open/Closed Principle (OCP).

---

## Task 2: Ticket Escalation, Triage & Assignment

### 2.1 Automated Triage & SLA Calculation
- **Domain Modeling:**
  SLA windows depend on ticket priority and customer tier:
  - Base SLA windows: Critical (4h), High (8h), Medium (24h), Low (72h).
  - Premium customer multiplier: 0.8x applied to base window, rounded down, floored at 1 hour minimum.
  - Forced Critical priority for specific categories (e.g. Outage, Security) via domain flags (`Category.ForcesCriticalPriority`).
- **Separation of Concerns:**
  Created `TicketTriageService` in `SupportDesk.Application` injected with `IOptions<SlaOptions>`. This keeps business configuration out of domain aggregates while encapsulating assignment logic:
  - Selects only active agents (`IsActive == true`).
  - Prioritizes specialists if the category requires it (`Category.RequiresSpecialist == true`).
  - Checks workload limits (`MaxOpenTickets`).
  - Dispatches to the agent with the fewest open tickets, applying deterministic tie-breaking (by Agent `Id`).
  - Handles the edge case where no eligible agents exist by leaving the ticket unassigned with an explicit explanation.

### 2.2 Escalation & SLA Status Derivation
- **Escalation Rules:**
  - One-step priority increase (`Low -> Medium -> High -> Critical`).
  - Tickets already `Critical`, `Resolved`, or `Closed` reject escalation with domain conflict exceptions (`409 Conflict`).
  - Recalculates due date from the escalation timestamp using the new priority's SLA window.
  - Immutable audit history recorded in `TicketEscalation` entities capturing previous/new priority, reassignments, recalculated due dates, actor, and reasons (5–500 characters).
- **Dynamic SLA Status Derivation (BR-8):**
  SLA status is **derived at runtime, never persisted**, ensuring consistency regardless of clock drift:
  - `NotApplicable`: No due date.
  - `Met`: Resolved on or before due date.
  - `Breached`: Due date passed while unresolved, or resolved late.
  - `AtRisk`: Unresolved with $\le 25\%$ of the total window remaining.
  - `WithinSla`: Unresolved with $> 25\%$ window remaining.
- **SQL Server Query Translation:**
  Because derived SLA statuses must be filtered on the database side (`GET /api/tickets?slaStatus=...`), `ApplyFilters` maps SLA criteria to SQL Server-native functions. When running on SQL Server, it employs `EF.Functions.DateDiffSecond` to avoid runtime LINQ translation failures caused by unsupported `DateTime.Ticks` arithmetic.

### 2.3 Frontend User Experience
- **SLA Indicator (`SlaBadge`):**
  Built with distinct geometric symbols (●, ▲, ✕, ✓, —) and colors to comply with WCAG 1.4.1 (not relying on color alone).
- **Escalation Modal & History:**
  Provides inline validation, character count (5–500 chars), disabled states with contextual tooltips for un-escalatable tickets, and instant optimistic/in-place table updates without full page reloads.

---

## Task 3: Agent Authentication & Route Protection

### 3.1 Security & Credential Storage
- **Aggregate Boundary:**
  Added `PasswordHash` directly to the `Agent` entity. Every agent is an authenticated operator within this domain; avoiding a separate 1-to-1 `Credentials` table eliminates needless relational joins and keeps agent lifecycle management cohesive.
- **Cryptographic Hashing:**
  Implemented `PasswordHasher` utilizing **PBKDF2** with HMAC-SHA256, 128-bit cryptographically secure random salt, and 100,000 iterations. Verification employs `CryptographicOperations.FixedTimeEquals` to prevent side-channel timing attacks.
- **Anti-Enumeration:**
  `POST /api/auth/login` returns an identical `401 Unauthorized` (`"Invalid email or password."`) whether the email does not exist, belongs to an inactive agent, or has an invalid password.

### 3.2 Token Architecture & Centralized Authorization
- **Token Design:**
  Issued HMAC-SHA256 signed JWTs with an 8-hour expiration containing standard claims (`sub`, `name`, `email`).
- **Centralized Security ("Secure by Default"):**
  Configured ASP.NET Core fallback authorization policy:
  ```csharp
  options.FallbackPolicy = new AuthorizationPolicyBuilder()
      .RequireAuthenticatedUser()
      .Build();
  ```
  Every existing and future endpoint requires authentication by default unless explicitly decorated with `[AllowAnonymous]` (such as `/api/auth/login`, Swagger, and `/health`).
- **Actor Identity Verification:**
  Replaced client-supplied `escalatedBy` parameters in commands with the authenticated agent's name extracted directly from JWT `ClaimTypes.Name` claims.

### 3.3 Frontend Authentication & Interceptors
- **Token Storage:**
  Stored in `localStorage` under `support_desk_auth_token` for straightforward cross-tab persistence in an SPA.
- **HTTP Interceptor:**
  `client.ts` attaches `Authorization: Bearer <token>` to all outbound requests. On HTTP 401, the client automatically clears credentials and notifies `AuthContext` to redirect the user to `/login?redirect=...`.
- **Protected Routing:**
  Wrapped application routes in `ProtectedRoute` to safeguard internal features while preserving target return paths post-login.

---

## Task 4: Dockerisation & Operational Hardening

### 4.1 Multi-Stage Container Architecture
- **SDK Build vs. Runtime Separation:**
  - Build stage: `mcr.microsoft.com/dotnet/sdk:10.0`
  - Runtime stage: `mcr.microsoft.com/dotnet/aspnet:10.0`
- **Build Cache Optimization (DK-1):**
  Copied `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and `.csproj` files prior to source copying to cache the `dotnet restore` layer. Subsequent code edits rebuild and publish in seconds without re-fetching NuGet packages.
- **Image Size Reduction:**
  - Full SDK image: **~1.15 GB**
  - Published runtime container: **483 MB** (over **58% reduction** in image footprint).

### 4.2 Security Hardening (DK-3 & DK-4)
- **Least Privilege:** The container runs as the non-root `app` user (UID 1654).
- **Stateless & Secret-Free:** No secrets or connection strings baked into the image. All configuration is supplied via runtime environment variables (`ConnectionStrings__Default`, `Jwt__SecretKey`).
- **Port:** Standard non-privileged port `8080` internally, exposed as `5080:8080`.

### 4.3 Production Readiness & Health Checks
- **Health Endpoint (DK-7):**
  Added `GET /health` with `[AllowAnonymous]` executing `db.Database.CanConnectAsync()`. Returns 200 OK `{ "status": "Healthy", "database": "Connected" }` or 503 Service Unavailable.
- **Docker Compose Orchestration (DK-5):**
  Configured `api` service with `depends_on: sqlserver` and `condition: service_healthy`, ensuring the database engine is ready before the API attempts migrations and startup.
