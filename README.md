# ClaimFlow

ClaimFlow is a motor and property claims management platform. One web application serves two user roles:

- **Claimants** report incidents, track their claims, answer questions and receive decisions.
- **Claims officers** pick up, assess and decide claims, and see their team's workload, performance and outstanding liability exposure.

It has an Angular frontend and an ASP.NET Core API, covering the full claim lifecycle from submission to settlement or rejection, with 247 automated tests. **Kafka is not implemented**; see [Kafka decision](#kafka-decision) and [Limitations](#limitations-and-future-improvements).

## Features

| Area | Implemented in the web application and API |
|---|---|
| Login | Email and password with two demo users. There is no registration |
| Claimant: claims | Submit Motor and Property claims, list own claims, view status, details and history |
| Claimant: information | Read an officer's question and answer it |
| Claimant: decisions | See the settlement amount or rejection reason. Assessments are never shown |
| Officer: lists | Incoming claims and own assigned claims |
| Officer: actions | Pick up, assess and reassess, request information, settle, reject. Settling and rejecting need confirmation |
| Officer: dashboard | Claim counts, workload and average decision time per officer |
| Officer: exposure | Outstanding liability per currency, with no total across currencies |

### Claim lifecycle

```text
Submitted            ── officer picks up ────────────► InReview
InReview             ── officer asks a question ─────► AwaitingInformation
AwaitingInformation  ── claimant responds ───────────► InReview
InReview             ── officer settles ─────────────► Settled
InReview             ── officer rejects ─────────────► Rejected
AwaitingInformation  ── officer rejects ─────────────► Rejected
```

- Recording an assessment keeps the claim in `InReview`, and is required before settlement.
- `Settled` and `Rejected` are final.
- Status names and transitions are design decisions; the brief does not prescribe them.

## Architecture

One Angular application and a modular monolith: one ASP.NET Core API with one SQLite database.

```text
Angular application ──REST/JSON + JWT──► ASP.NET Core API ──EF Core──► SQLite
/login /claimant /officer                Auth │ Claims │ Dashboard
```

| Part | Technology |
|---|---|
| Frontend | Angular 22.3.0, TypeScript 6 (strict), RxJS 7.8. No UI or chart library |
| API | C#, .NET 10, ASP.NET Core Web API |
| Data | Entity Framework Core 10.0.15, SQLite |
| Authentication | JWT bearer tokens, ASP.NET Core `PasswordHasher` |
| API description | OpenAPI |
| Backend tests | xUnit, ASP.NET Core test host, in-memory SQLite |
| Frontend tests | Vitest |

- **Backend:** controllers are thin. `ClaimService` loads a claim, calls a method on the `Claim` entity and saves once. All lifecycle rules are in `Claim`.
- **Frontend:** services call the API, an interceptor adds the token, and route guards keep each role in its own area. Guards only guide navigation; **the API enforces roles, ownership and assignment**.
- **History:** every action adds a `ClaimHistory` row in the same save as the claim change.
- **Concurrency:** every action on an existing claim sends `expectedVersion`. A stale version gets 409, and the screens reload the claim instead of repeating the action.

## Project structure

```text
ClaimFlow/
├── README.md
├── AI_WORKING_JOURNAL.md          AI prompts, decisions and challenges
├── backend/
│   ├── ClaimFlow.slnx
│   ├── dotnet-tools.json          Pins the dotnet-ef tool
│   ├── ClaimFlow.Api/
│   │   ├── Modules/
│   │   │   ├── Auth/              Login, JWT, users
│   │   │   ├── Claims/            Lifecycle, markets, endpoints
│   │   │   └── Dashboard/         Team figures and exposure
│   │   ├── Data/                  DbContext, configurations, migrations, demo users
│   │   ├── Infrastructure/        Error responses, OpenAPI security
│   │   └── Program.cs
│   └── ClaimFlow.Tests/           Domain, persistence and API tests
└── frontend/
    └── claimflow-web/
        ├── proxy.conf.json        Forwards /api to the backend
        └── src/app/
            ├── core/              API access, authentication, models
            ├── features/          auth, claimant and officer screens
            └── shared/            Page shell, status badge, shared styles
```

## Prerequisites

- Windows with PowerShell
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (developed with 10.0.404)
- [Node.js](https://nodejs.org/) 22.22.3 or later in version 22, 24.15.0 or later in version 24, or 26 and later (developed with 26.7.0)
- npm 8 or later, included with Node.js (developed with 11.19.0)
- Git, to clone the repository
- Internet access for the first run, to download packages

No database server, Docker, Kafka or global Angular CLI is needed.

## Run locally

Use two PowerShell windows, both starting in the repository root.

**1. Start the API**

```powershell
dotnet run --project backend/ClaimFlow.Api
```

Wait for `Now listening on: http://localhost:5032`. The API description is at http://localhost:5032/openapi/v1.json. The root address `/` is not an endpoint.

**2. Start the frontend**

```powershell
cd frontend/claimflow-web
npm.cmd ci
npm.cmd start
```

`npm.cmd` works even if PowerShell blocks script files. The first command is needed only once.

**3. Open http://localhost:4200** and log in with a demo account below. To use both roles together, open the second in a different browser or a private window.

Press Ctrl+C in each window to stop.

**How they connect:** the browser calls `/api` on the frontend's own address. The Angular development server forwards those calls to `http://localhost:5032`, as set in `proxy.conf.json`, so no CORS setting is needed. The proxy is for development only.

### Demo credentials

| Role | Email | Password |
|---|---|---|
| Claimant | `claimant@demo.example` | `Demo-Claimant-1` |
| Claims officer | `officer@demo.example` | `Demo-Officer-1` |

These credentials and the JWT signing key are in `backend/ClaimFlow.Api/appsettings.Development.json`. **They are for local assessment use only and must never be used in production.** Outside Development, the API refuses to start unless `Jwt:Key` is configured.

### Demonstration

1. As the **claimant**, choose Submit Claim and submit a Motor or Property claim.
2. As the **officer**, open Incoming Claims and pick it up, then open it.
3. Choose Request information and ask a question.
4. As the **claimant**, open the claim and send an answer.
5. As the **officer**, reload the claim, record an assessment, then settle it. Alternatively, reject it with a reason.
6. As the **claimant**, reload the claim to see the decision.
7. As the **officer**, open the Dashboard.

Screens do not refresh automatically, so reload to see the other user's changes.

### Try the API without the frontend

With the API running, paste this into another PowerShell window. It submits, picks up, assesses and settles a claim, then shows the dashboard.

```powershell
$api = "http://localhost:5032"

function Login($email, $password) {
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    $login = Invoke-RestMethod "$api/api/auth/login" -Method Post -Body $body -ContentType "application/json"
    @{ Authorization = "Bearer $($login.accessToken)" }
}

function Send($path, $headers, $body) {
    Invoke-RestMethod "$api$path" -Method Post -Headers $headers -Body ($body | ConvertTo-Json) -ContentType "application/json"
}

$claimant = Login "claimant@demo.example" "Demo-Claimant-1"
$officer = Login "officer@demo.example" "Demo-Officer-1"

$claim = Send "/api/claims" $claimant @{
    type = "Motor"; market = "Singapore"; incidentDate = "2026-01-15"
    description = "Rear bumper damage"; vehicleRegistration = "SGX1234A"; claimedAmountMinor = 125050
}

$path = "/api/officer/claims/$($claim.id)"
$claim = Send "$path/pickup" $officer @{ expectedVersion = $claim.version }
$claim = Send "$path/assessment" $officer @{ expectedVersion = $claim.version; amountMinor = 100000 }
$claim = Send "$path/settle" $officer @{ expectedVersion = $claim.version; note = "Approved in part" }

Invoke-RestMethod "$api/api/claims/$($claim.id)" -Headers $claimant
Invoke-RestMethod "$api/api/officer/dashboard" -Headers $officer | ConvertTo-Json -Depth 5
Invoke-RestMethod "$api/api/officer/dashboard/exposure" -Headers $officer
```

Exposure is empty if every claim has been decided.

## Database

- In Development, startup creates the database, applies migrations and adds the two demo users. Existing users are left unchanged. Nothing is migrated or seeded in other environments.
- The default file is `backend/ClaimFlow.Api/claimflow.db`, which Git ignores.
- To start again, stop the API and delete `claimflow.db` and any `claimflow.db-*` files beside it.
- If the repository is in a synchronised folder such as OneDrive, use a file elsewhere:

```powershell
New-Item -ItemType Directory -Force C:\Temp | Out-Null
$env:ConnectionStrings__ClaimFlow = "Data Source=C:\Temp\claimflow.db;Foreign Keys=True"
dotnet run --project backend/ClaimFlow.Api
```

Running the API does not need the migration tool. To work with migrations, run these from `backend`:

```powershell
dotnet tool restore
dotnet ef migrations list --project ClaimFlow.Api
```

## Run the tests

Backend, from the repository root (129 tests, using in-memory SQLite, so the development database is not touched):

```powershell
dotnet test backend/ClaimFlow.slnx
```

Frontend, from `frontend/claimflow-web` (118 tests, with the API replaced by test data), and the production build:

```powershell
npm.cmd test -- --watch=false
npm.cmd run build
```

## API reference

All endpoints except login need an `Authorization: Bearer <token>` header. Tokens last one hour.

| Method and path | Role | Purpose |
|---|---|---|
| `POST /api/auth/login` | Anyone | Get a token |
| `POST /api/claims` | Claimant | Submit a claim |
| `GET /api/claims` | Claimant | List own claims |
| `GET /api/claims/{id}` | Claimant | View own claim |
| `GET /api/claims/{id}/history` | Claimant | View own claim's history |
| `POST /api/claims/{id}/information` | Claimant | Answer an information request |
| `GET /api/officer/claims` | Officer | List incoming and own assigned claims |
| `GET /api/officer/claims/{id}` | Officer | View any claim |
| `GET /api/officer/claims/{id}/history` | Officer | View any claim's history |
| `POST /api/officer/claims/{id}/pickup` | Officer | Pick up a submitted claim |
| `POST /api/officer/claims/{id}/assessment` | Assigned officer | Record an assessed amount |
| `POST /api/officer/claims/{id}/information-requests` | Assigned officer | Ask the claimant a question |
| `POST /api/officer/claims/{id}/settle` | Assigned officer | Settle at the latest assessed amount |
| `POST /api/officer/claims/{id}/reject` | Assigned officer | Reject with a reason |
| `GET /api/officer/dashboard` | Officer | Workload and performance |
| `GET /api/officer/dashboard/exposure` | Officer | Outstanding exposure per currency |

Every action on an existing claim must include `expectedVersion`, the claim version last seen.

| Status | Meaning |
|---|---|
| 400 | Invalid input |
| 401 | Not logged in |
| 403 | Wrong role, or the claim is assigned to another officer |
| 404 | Not found, or another claimant's claim |
| 409 | Action not allowed in this status, or the claim has changed |

## Design decisions

| Decision | Reason | Trade-off |
|---|---|---|
| Modular monolith | Claim, history and requests save in one transaction; simple to run | Module boundaries rely on discipline |
| One Angular application with lazily loaded areas | Both roles share login, styling and components | Both roles are released together |
| Development proxy | Frontend and API share one address, so no CORS | Deployment needs its own setup |
| Token in `sessionStorage` | Survives a refresh and ends with the tab | Readable by a script injected into the page |
| REST | Users need an immediate result for each action | No push updates |
| SQLite and EF Core | No server to install; real constraints and migrations | One writer at a time |
| No repository layer | `DbContext` already provides data access | Services depend on EF Core |
| JWT with two roles | Enforced access control with little code | Tokens cannot be revoked early |
| Rules in the `Claim` entity | One place to read and test the lifecycle | – |
| Version on every action | Prevents acting on an out-of-date screen | Clients must send it |
| Money as whole minor units | Exact totals; supports currencies without decimals, such as JPY | Values need converting for display |
| Exposure per currency | Adding currencies would need exchange rates | No single regional total |

Errors follow the order permission, version, status, then input, so that claim state is not revealed to people without access. A missing or malformed version is refused before assignment and ownership checks.

### Kafka decision

**Kafka is not implemented.** Every claim operation uses synchronous REST, because the user needs to know at once whether an action succeeded. Within the assessment time limit, a reliable, tested application that runs locally without extra infrastructure was prioritised over event delivery.

A possible future extension, none of which exists today:

- Save an outbox message in the same transaction as the claim change, so an event cannot be lost.
- Publish it to a Kafka topic, keyed by claim so that each claim's events stay in order.
- Let consumers react later, for example by notifying the claimant, ignoring events they have already handled.

REST would remain the way to change a claim, so the application would keep working if Kafka were unavailable. `ClaimHistory` records the same business events but is not an event queue.

### Assumptions

The brief does not specify these points. They are developer assumptions or interpretations:

- **Markets:** Malaysia (MYR), Singapore (SGD), Australia (AUD), Hong Kong (HKD), Japan (JPY) and New Zealand (NZD) are illustrative. The six markets are illustrative developer assumptions and are not confirmed operating markets of any particular insurer.
- **Time zones:** each market uses one representative zone to check that an incident date is not in the future. Countries such as Australia have several.
- **Performance:** claims decided, and average time from submission to decision, per officer. The average includes time waiting for the claimant, so it is not a pure measure of officer speed.
- **Exposure:** open claims only, using the assessed amount or otherwise the claimed amount.
- **Settlement:** approval at a recorded amount. No payment is made.
- **Visibility:** claimants see the settlement amount, but never assessments.
- **Roles:** there is no separate manager role. All officers form one team and see the dashboard.
- **Maximum amount:** 1,000,000,000,000 minor units per claim, as a technical safeguard.

## Limitations and future improvements

| Not implemented or verified | Future improvement |
|---|---|
| Kafka and notifications | See [Kafka decision](#kafka-decision) |
| Automatic screen refresh | Polling or push updates |
| Paging, sorting and filtering of lists | Add to the API and screens |
| Registration, login rate limiting, token revocation | Company identity provider |
| Deployment; HTTPS in the local profile | Hosting setup; HTTPS is required in production |
| Recognising a repeated claim submission after a lost connection | A request key checked by the API |
| Market settings are copied in the frontend | A market endpoint |
| Saving unsent form text | Drafts, or a warning before leaving |
| Browser tests in the repository | Add the temporary browser checks as a test suite |
| Reassignment, withdrawal, appeal, file attachments | Add with agreed business rules |
| Incident location | Use it for the production time zone |
| Dashboard date filters and market breakdown | Add as query parameters |
| Truly simultaneous requests and large data volumes are untested | Load tests; PostgreSQL or SQL Server |
| History is append-only in the application only | Database permissions or triggers |

Some smaller points: times are shown in the viewer's time zone; there is one demo officer; dashboard counts and averages come from separate queries, and the default database path depends on starting the API with `dotnet run` as shown.
