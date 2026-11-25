# ClaimFlow

ClaimFlow is a motor and property claims management platform. Claimants report incidents, track their claims and answer questions. Claims officers pick up, assess and decide claims, and see their team's workload, performance and outstanding liability exposure.

**Current state:** the backend API is complete for the claim lifecycle and dashboard, with 129 automated tests. There is no user interface yet, and Kafka is not implemented. See [Limitations](#limitations-and-future-improvements).

## Features

| Area | Implemented |
|---|---|
| Login | Email and password with two demo users. There is no registration |
| Claimant | Submit a claim, list and track own claims, view history, answer information requests, see decisions |
| Claims officer | View incoming and own claims, pick up, record assessment, request information, settle, reject |
| Dashboard | Claims by status, workload and performance per officer |
| Exposure | Outstanding liability per currency |

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

A modular monolith: one ASP.NET Core API with one SQLite database.

```text
HTTP client ──REST/JSON + JWT──► ASP.NET Core API ──EF Core──► SQLite
                                 Auth │ Claims │ Dashboard
```

| Part | Technology |
|---|---|
| API | C#, .NET 10, ASP.NET Core Web API |
| Data | Entity Framework Core 10.0.15, SQLite |
| Authentication | JWT bearer tokens, ASP.NET Core `PasswordHasher` |
| API description | OpenAPI |
| Tests | xUnit, ASP.NET Core test host, in-memory SQLite |

Controllers are thin. `ClaimService` loads a claim, calls a method on the `Claim` entity and saves once. All lifecycle rules are in `Claim`.

## Project structure

```text
ClaimFlow/
├── README.md
├── AI_WORKING_JOURNAL.md          AI prompts, decisions and challenges
└── backend/
    ├── ClaimFlow.slnx
    ├── dotnet-tools.json          Pins the dotnet-ef tool
    ├── ClaimFlow.Api/
    │   ├── Modules/
    │   │   ├── Auth/              Login, JWT, users
    │   │   ├── Claims/            Lifecycle, markets, endpoints
    │   │   └── Dashboard/         Team figures and exposure
    │   ├── Data/                  DbContext, configurations, migrations, demo users
    │   ├── Infrastructure/        Error responses, OpenAPI security
    │   └── Program.cs
    └── ClaimFlow.Tests/           Domain, persistence and API tests
```

## Prerequisites

- Windows with PowerShell
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (developed with 10.0.404)
- Internet access for the first build, to download NuGet packages

No database server, Docker or Node.js is needed.

## Run locally

From the repository root:

```powershell
dotnet run --project backend/ClaimFlow.Api
```

The API listens on **http://localhost:5032**. The API description is at http://localhost:5032/openapi/v1.json. The root address `/` is not an endpoint, so use the API paths below. Press Ctrl+C to stop.

### Demo credentials

| Role | Email | Password |
|---|---|---|
| Claimant | `claimant@demo.example` | `Demo-Claimant-1` |
| Claims officer | `officer@demo.example` | `Demo-Officer-1` |

These credentials and the JWT signing key are in `backend/ClaimFlow.Api/appsettings.Development.json`. **They are for local assessment use only and must never be used in production.** Outside Development, the API refuses to start unless `Jwt:Key` is configured.

### Try it

With the API running, paste this into a second PowerShell window. It submits, picks up, assesses and settles a claim, then shows the dashboard.

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

```powershell
dotnet test backend/ClaimFlow.slnx
```

The 129 tests use in-memory SQLite and do not touch the development database.

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
| REST | Users need an immediate result for each action | No push updates |
| SQLite and EF Core | No server to install; real constraints and migrations | One writer at a time |
| No repository layer | `DbContext` already provides data access | Services depend on EF Core |
| JWT with two roles | Enforced access control with little code | Tokens cannot be revoked early |
| Rules in the `Claim` entity | One place to read and test the lifecycle | – |
| Version on every action | Prevents acting on an out-of-date screen | Clients must send it |
| Money as whole minor units | Exact totals; supports currencies without decimals, such as JPY | Values need converting for display |
| Exposure per currency | Adding currencies would need exchange rates | No single regional total |

Errors follow the order permission, version, status, then input, so that claim state is not revealed to people without access. A missing or malformed version is refused before assignment and ownership checks.

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
| Angular user interface | Claimant and officer screens, dashboard; configure CORS or a development proxy |
| Kafka | Publish claim events through an outbox for notifications |
| Registration, login rate limiting, token revocation | Company identity provider |
| HTTPS in the local profile | HTTPS is required in production |
| Reassignment, withdrawal, appeal, file attachments | Add with agreed business rules |
| Incident location | Use it for the production time zone |
| Dashboard date filters and market breakdown | Add as query parameters |
| Truly simultaneous requests and large data volumes are untested | Load tests; PostgreSQL or SQL Server |
| History is append-only in the application only | Database permissions or triggers |

Two smaller points: dashboard counts and averages come from separate queries, and the default database path depends on starting the API with `dotnet run` as shown.
