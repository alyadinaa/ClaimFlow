# AI Working Journal

A running log of what was asked of the AI, and what the developer accepted, challenged and overrode.

---

## Entry 1: Assessment requirement analysis

- **Date:** 2026-11-25
- **Task:** Assessment requirement analysis
- **AI tool:** Claude Code (Claude Opus 5.5)

### User prompt

```text
I am working on a technical take-home assessment
for the Chubb APAC Young Graduate Program
(Full-stack Developer).

I have placed the original assessment PDF in
the project root directory as assessment.pdf.

TASK: REQUIREMENT ANALYSIS ONLY

First, read the entire assessment.pdf carefully.

Analyse the assessment and provide the following:

1. PROJECT OVERVIEW
   - What problem are we solving?
   - What is the expected application?
   - Who are the target users?

2. FUNCTIONAL REQUIREMENTS
   - List all requirements for Claimants.
   - List all requirements for Claims Staff.
   - Identify dashboard and management requirements.

3. TECHNICAL REQUIREMENTS
   - Required and preferred technologies.
   - Communication requirements.
   - Backend and frontend expectations.

4. BUSINESS WORKFLOW
   - Explain the expected claims process.
   - Identify important claim status transitions.
   - Explain how claimants and claims staff interact.

5. DELIVERABLES
   - List everything I must submit.
   - Explain the AI Working Journal requirement.
   - Identify local application requirements.

6. ASSESSMENT PRIORITIES
   - What should be prioritised?
   - What can reasonably be left out?
   - What does the assessment explicitly say about
     backend versus frontend development?

7. AMBIGUITIES
   - Identify requirements that are not clearly
     specified in the assessment.
   - Do not invent answers for missing requirements.

IMPORTANT:
- Base your analysis strictly on assessment.pdf.
- Include the relevant PDF page number for each
  major requirement.
- Separate explicit requirements from your own
  interpretations.
- Do not introduce unnecessary features.
- Do not write application code.
- Do not create backend or frontend projects.
- Do not install dependencies.
- Do not make final architecture decisions yet.

AI WORKING JOURNAL:
Create or update AI_WORKING_JOURNAL.md.

Record this actual interaction:
- Task: Assessment requirement analysis.
- AI tool: Claude Code.
- User prompt: This requirement analysis prompt.
- AI findings: Summarise your actual findings.
- Developer decision: Pending review.

Do not claim that I accepted your recommendations
before I have reviewed them.

At the end, wait for my review and approval.
Do not proceed to the next development phase.
```

### AI findings

Pages refer to the Word document's saved page breaks. **[E]** = explicit in the brief, **[I]** = AI interpretation.

- **Source document clarification:**
  - The project folder contained `Young Grad Programme THA (Nov 2026).docx`.
  - The initial user prompt mistakenly referred to `assessment.pdf`.
  - Claude correctly identified and used the DOCX for the requirement analysis.
  - The developer confirmed that the DOCX and original PDF contain identical assessment content.
- **Overview [E, p.2]:** a platform to replace fragmented motor and property claims handling across six markets. Users are claimants and claims staff, sharing one Angular application. Managers appear only in the problem statement, so a separate manager role is not explicitly required [I].
- **Claimants [E, p.2]:** report an incident, track a claim, provide additional information when asked, receive decisions.
- **Claims staff [E, p.2]:** pick up incoming claims, review and assess, progress to settlement or rejection, see team workload and performance. Requesting information is implied [I].
- **Dashboard and management:** team workload and performance are explicit [E, p.2]. Officer workload needs and liability exposure are posed as questions to consider [E, p.3]. A real-time manager view is stated as a problem [E, p.2]; treating it as a feature is [I].
- **Technology [E, p.2]:** under "Preferred": Angular; C#/.NET or Java/Spring Boot; REST/HTTP and Kafka, with the split to be decided and explained. Everything else is the developer's decision.
- **Workflow:** report, pick up, review and assess, request and receive information if needed, settle or reject, decision received. The brief defines no status names or state machine; any status model is [I].
- **Deliverables [E, p.3]:** Git repository with meaningful history; application that starts locally; this journal, committed alongside the code; any appropriate supporting documentation. No public hosting and no Chubb proprietary information, material or branding.
- **Journal requirement [E, p.3]:** log what was asked, accepted, challenged and overrode, with brief reasoning. The developer signs off every line.
- **Priorities [E]:** start with the backend, where most time should go; a well-built backend with a partial frontend outscores a rushed attempt at both (p.2). Target 2–3 hours, hard cap 5 (p.1, p.3). Build what matters most well (p.3).
- **Suggested order [I]:** claim lifecycle backend, REST/Kafka split, workload/performance/exposure data, thin Angular slice; local start, commits and journal throughout.
- **Candidates to leave out [I]:** full frontend coverage, polished UI, production-grade authentication, real email/SMS, real payments, market-specific rules.
- **Ambiguities (left unanswered):** manager role and authentication; claim fields, attachments and policy validation; market differences; statuses and meaning of "settlement"; pickup and reassignment; information request format; decision authority and notification; team and performance metrics; liability exposure calculation; "real-time" latency; whether Kafka is mandatory; minimum frontend; testing and non-functional needs; submission method and deadline; whether analysis counts towards the time cap.
- **Caution [I]:** the source document's footer says "Confidential"; the AI recommended not committing it.

### Developer decision

**Developer decision: Approved**

- **Accepted:** The requirement analysis, identified functional requirements, technical expectations, business workflow, deliverables, assessment priorities and identified ambiguities.
- **Challenged:** None.
- **Overridden:** None.
- **Reasoning:** The analysis accurately reflects the assessment brief and provides a clear understanding of the project requirements and development priorities.

Note: The developer initially approved the creation of this journal file. The requirement analysis was subsequently reviewed and formally approved in this entry.

---

## Repository preparation record

- **Date:** 2026-11-25
- Claude Code created `.gitignore` for ASP.NET Core and Angular at the developer's request.
- The confidential assessment document, `Young Grad Programme THA (Nov 2026).docx`, is excluded.
- The developer made the initial commit, `0c373f9 chore: initialize ClaimFlow project`, containing `.gitignore` and this journal.
- The developer reported a clean working tree after the commit; Claude Code confirmed this with a read-only check before starting the architecture proposal.

---

## Entry 2: System architecture (Phase 2, Task 1)

- **Date:** 2026-11-25
- **Task:** System architecture proposal, two revisions and approval
- **AI tool:** Claude Code (Claude Opus 5.5)
- **Output:** Design discussion only. No application code, project files, dependencies or commits.

### Prompt 1: Initial architecture request (condensed)

The developer asked for an architecture proposal before any code, covering: modular monolith vs microservices, frontend and backend architecture, REST vs Kafka, database, security and access, and scalability and limitations.

Stated environment: VS Code, C# / ASP.NET Core Web API, Angular, Node.js, Git, Windows; a beginner in Angular and ASP.NET Core.

Key constraints: "Prioritise backend quality", "Do not overengineer", "Clearly distinguish assessment requirements from your recommendations", "Explain technical trade-offs", and do not assume features not stated in the assessment.

### AI response 1: Initial proposal

The AI checked the local tools first: .NET SDK 10.0.404, Node 26.7.0, Docker installed but not running, Angular CLI not installed.

- Modular monolith: one ASP.NET Core API, one Angular app, SQLite with EF Core and no repository layer.
- REST for user actions; Kafka for later reactions (notifications), within the main plan.
- One `ClaimEvents` table serving as both claim history and outbox.
- JWT with seeded users and two roles.
- Money: the AI noted that SQLite has no native decimal type, but gave no concrete representation.
- 280-minute time budget, exceeding the 2–3 hour target.

### Prompt 2: First revision (verbatim adjustments)

```text
1. **Kafka implementation**
   - Prioritise the core REST API and claim workflow.
   - Treat Kafka integration as a secondary implementation task.
   - Explain the minimum viable Kafka implementation that can demonstrate asynchronous communication.
   - Do not make Kafka a blocker for running the main application locally.

2. **Claim history and Outbox**
   - Separate ClaimHistory and OutboxMessages conceptually.
   - Explain their different responsibilities.
   - Only introduce the Outbox implementation if Kafka integration proceeds.

3. **Financial data**
   - Recommend a safe representation for monetary values with SQLite.
   - Store currency separately.
   - Ensure liability exposure calculations do not combine different currencies.

4. **Security**
   - Keep the JWT and two-role approach.
   - Prioritise backend authorisation and claim ownership checks.
   - Avoid unnecessary authentication features.

5. **Time management**
   - Revise the implementation plan to prioritise the essential assessment requirements.
   - Provide a core implementation plan that can be completed within approximately 2–3 hours.
   - Identify optional features that can be added only if time remains.
   - Do not assume that the entire 5-hour allowance should be used.

6. **Scope**
   - Keep the modular monolith.
   - Keep one Angular application and one ASP.NET Core API.
   - Avoid adding unnecessary projects, services or dependencies.
   - Preserve motor and property claim support across multiple markets.
```

### AI response 2: Revised proposal

- Kafka became the first optional stage, off by default, with one topic, three events and one consumer.
- `ClaimHistory` (permanent business timeline, core) separated from `OutboxMessages` (delivery tracking, only with Kafka).
- Money stored as integer minor units with a separate currency code; exposure grouped by currency with no grand total.
- Layered access checks: authentication, role, ownership, assignment, business rule, concurrent pickup.
- Core plan of 165 minutes plus a 15-minute buffer. The dashboard was API-only, with its screen optional.

### Prompt 3: Second revision (verbatim adjustments)

```text
1. **Dashboard**
   - Include a basic Claims Officer dashboard screen in the core Angular implementation.
   - Keep it simple: summary cards and a table showing team workload and performance.
   - Do not postpone the entire dashboard UI to the optional stage.

2. **Six markets**
   - The assessment requires six markets but does not specify their names.
   - Use these illustrative APAC demo markets:
     Malaysia (MYR), Singapore (SGD), Australia (AUD), Hong Kong (HKD), Japan (JPY), and New Zealand (NZD).
   - Clearly document that these are developer assumptions for demonstration, not confirmed Chubb operating markets.
   - Ensure liability exposure remains grouped by currency.

3. **Performance metrics**
   - Use claims decided per officer and average submission-to-decision time as the proposed MVP metrics.
   - Clearly document these as interpretations of the assessment requirement.

4. **Time plan**
   - Update the core implementation plan to include the basic dashboard UI.
   - Keep Kafka optional and only implement it after the core application works.
   - Keep the total core target around 2–3 hours, but clearly state that the estimates are targets rather than guarantees.
```

### AI response 3: Final proposal

- Dashboard moved into core, sharing the officer landing page with the claim queue.
- Six demo markets adopted as assumptions. The AI noted that p.2 mentions six markets as background and does not explicitly require six to be implemented.
- JPY has no decimal places, so conversion must not assume division by 100.
- Average time to decision includes time spent waiting for the claimant, so it is not a pure measure of officer speed.
- Core targets total 180 minutes with no buffer. The AI warned that a modest overrun is likely.

### Prompt 4: Approval (verbatim decisions)

```text
1. Approve the modular monolith architecture.
2. Approve .NET 10, ASP.NET Core Web API, Angular, SQLite and EF Core.
3. Approve the proposed claim workflow, ClaimHistory and backend authorisation.
4. Approve JWT authentication with two roles: Claimant and Claims Officer.
5. Approve the six illustrative demo markets and their currencies.
6. Approve liability exposure grouped by currency.
7. Approve the basic Claims Officer dashboard in the core implementation.
8. Approve the two proposed performance metrics as developer interpretations.
9. Approve the remaining recommendations in the final architecture proposal.

**Kafka condition:**
I agree that Kafka should be attempted after the core application is completed and committed. However, do not automatically proceed with Kafka. Ask for my approval again after the core implementation works and its essential tests pass.

The five-hour hard cap must not be exceeded. The time estimates are targets, not guarantees.

For time tracking, keep implementation time separate from preparation and planning time. Do not invent or estimate time already spent.
```

### Developer decision

**Developer decision: Approved, with a Kafka condition**

**Accepted** (the reasoning column is the AI's rationale, as proposed and approved)

| Decision | Reasoning |
|---|---|
| Modular monolith | Claim state, history and assignment change in one transaction; simple to run; nothing in the brief requires separate services |
| .NET 10, ASP.NET Core, Angular, SQLite, EF Core | Matches the preferred stack; SQLite needs no server |
| No repository layer | `DbContext` already provides data access |
| Claim workflow, `ClaimHistory`, backend authorisation | Covers the core requirements; the API, not Angular, enforces security |
| JWT with two roles | Realistic access control without extra authentication features |
| Integer minor units; currency derived from market | Exact sums in SQLite; avoids mismatched currencies |
| Exposure = open claims, assessed amount otherwise claimed | Uses the best available figure; the brief defines no calculation |
| No manager role; one team; self-assigned pickup; no reassignment | Not required by the brief; keeps scope small |
| Officers view all claims but change only their own | Shared queue with clear responsibility |
| One `Claim` with small motor/property details | Supports both types without duplication |
| Backend-owned OpenAPI contract | Single source for the frontend/backend agreement |

Items without a source in the brief are assumptions or interpretations.

**Challenged and revised at the developer's direction**

| AI proposal | Developer direction | Result |
|---|---|---|
| Kafka in the main plan | Make it secondary and non-blocking | Optional, after core |
| Combined history and outbox table | Separate them | `ClaimHistory` core; outbox only with Kafka |
| No concrete money representation | Recommend a safe one, with currency separate | Integer minor units plus currency |
| 280-minute plan | About 2–3 hours for core | 180-minute core target |
| Markets left for the developer to choose | Use six named demo markets | Adopted as assumptions |

**Developer reasoning for the challenged decisions**

- **Kafka:** I prioritised the core REST API and claim workflow, so I made Kafka secondary. It must not block running the main application locally.
- **History and outbox:** I asked for these to be separated because they have different responsibilities. The outbox is only needed if Kafka proceeds.
- **Money:** I asked for a safe representation with SQLite and for currency to be stored separately, so that exposure never combines different currencies.
- **Time plan:** I wanted the essential assessment requirements completed first, in approximately 2–3 hours, without assuming the entire 5-hour allowance should be used.
- **Markets:** The assessment does not name the markets, so I chose six illustrative ones and asked for them to be documented as my assumptions.

**Overridden**

| AI proposal | Developer direction | Result |
|---|---|---|
| Dashboard screen in the optional stage | Include a basic screen in core | Moved into core |

- **Developer reasoning:** I explicitly requested the basic dashboard UI in the core implementation because team workload and performance are important assessment requirements. I asked for it to stay simple: summary cards and a table.

**Conditions**
- Approval of the general claim workflow does not constitute approval of the exact claim status names and transitions. These will be reviewed in a later design task.
- The AI must not proceed to Kafka automatically. It requires renewed developer approval after the core is working, its essential tests pass and it is committed.
- The five-hour hard cap must not be exceeded.
- Estimates are targets, not guarantees.

### Assumptions to carry into the README

- Clarification to Prompt 3, which is preserved verbatim: the assessment mentions six markets as background context (p.2) but does not explicitly require implementing six markets.
- The markets Malaysia (MYR), Singapore (SGD), Australia (AUD), Hong Kong (HKD), Japan (JPY) and New Zealand (NZD) are developer assumptions for demonstration, not confirmed Chubb operating markets.
- Claims decided per officer and average submission-to-decision time are interpretations of "workload and performance" (p.2).

### Time tracking

Implementation time is recorded separately from preparation and planning. No time already spent has been invented or estimated.

| Category | Time recorded |
|---|---|
| Preparation and planning | Not recorded |
| Implementation | Not started |

---

## Entry 3: Claim lifecycle design (Phase 3)

- **Date:** 2026-11-25
- **Task:** Claim status and transition design
- **AI tool:** Claude Code (Claude Opus 5.5)
- **Output:** Design discussion only. No code was written for this task.

The exact statuses and transitions below are **our design decisions, not status names prescribed by the assessment**. This entry covers the detail left open in Entry 2.

### Prompt 1: Developer's original proposal (verbatim excerpt)

```text
1. Submitted
   - Claimant submits a new claim.
   - Claim is waiting for an officer.

2. InReview
   - An officer picks up the claim.
   - The officer assesses the claim.

3. AwaitingInformation
   - Officer requests additional information.
   - Claimant provides the requested information.
   - Claim returns to InReview.

4. Settled
   - Claim is approved and settled.
   - Terminal status.

5. Rejected
   - Claim is rejected with a reason.
   - Terminal status.
```

The developer asked the AI to evaluate this, covering assignment, roles, information requests, terminal statuses, history and invalid transitions, without introducing unnecessary statuses.

### AI evaluation

- The five statuses are sufficient.
- Gap found: a claimant who never replies would leave the claim stuck. The AI added T8 and named an alternative: let the officer cancel the request, at the cost of another action and endpoint.
- Assignment should be separate from status, and both change together at pickup.
- Assessment should be an action, not a status, and is required before settlement.

### Prompt 2: Developer adjustments (verbatim excerpt)

```text
1. **T8 – Reject while awaiting information**
   - Approve this transition.
   - The assigned officer must provide a non-empty rejection reason.

2. **Settlement amount**
   - Record SettlementAmount separately from AssessedAmount.
   - Initially, SettlementAmount equals AssessedAmount.
   - Ensure the final amount and currency are recorded in claim history.

3. **Incident date**
   - Reject future incident dates based on the claim market's local date.
   - Do not use the proposed one-day-ahead-of-UTC allowance.

4. **Documentation**
   - Clearly distinguish explicit assessment requirements from developer assumptions.
   - Record that the exact statuses and transitions are our design decisions, not prescribed status names from the assessment.
```

### Approved design

**Transitions**

| # | Current | Action | Actor | Result | Basis |
|---|---|---|---|---|---|
| T1 | (none) | Submit | Claimant | Submitted | Action explicit (p.2) |
| T2 | Submitted | Pick up | Any claims officer | InReview | Action explicit (p.2) |
| T3 | InReview | Record assessment | Assigned officer | InReview | Action explicit (p.2) |
| T4 | InReview | Request information | Assigned officer | AwaitingInformation | Implied by "when asked" (p.2) |
| T5 | AwaitingInformation | Provide information | Claim owner | InReview | Action explicit (p.2); return to InReview is a design decision |
| T6 | InReview | Settle | Assigned officer | Settled | Action explicit (p.2) |
| T7 | InReview | Reject | Assigned officer | Rejected | Action explicit (p.2) |
| T8 | AwaitingInformation | Reject | Assigned officer | Rejected | Design decision |

"Action explicit" means the brief names the activity for that user type. Status names, the assigned-officer rule and the transition structure are design decisions.

**Rules** (design decisions)
- A Submitted claim has no officer; every other status has one, retained after the decision.
- One open information request at a time; multiple rounds allowed; text only.
- Settled and Rejected are terminal.
- `SettlementAmount` is recorded separately from `AssessedAmount` and initially equals it.
- Rejection (T7 and T8) requires a non-empty reason.
- Incident dates must not be in the future by the claim market's local date.
- Other submit checks: supported type and market, description, claimed amount above zero, required motor or property detail.
- Assessed amount must be above zero, in the claim's currency; information questions and responses must not be blank; text limit 2,000 characters.
- Identity and time come from the server. Claim changes and history are saved together, with a version check on every transition.
- No endpoint sets a status directly; each performs one named action.

**ClaimHistory:** claim ID, action, from status, to status, actor ID and role, UTC timestamp, action-specific note. Rows are never edited. Settlement history records the final amount and currency. Failed attempts are not recorded.

**Claimant visibility:** assessment entries and assessed amount are hidden until settlement. Rejection reasons are visible.

**Error codes**

| Situation | Code |
|---|---|
| Invalid input | 400 |
| Not logged in (from Entry 2) | 401 |
| Wrong role, or officer acting on another officer's claim | 403 |
| Claimant accessing another person's claim | 404 |
| Transition not allowed, settling without an assessment, or concurrent change | 409 |

### Assumptions

Not defined in the assessment; approved by the developer as assumptions:

| # | Assumption |
|---|---|
| A1 | Settlement means approval at a recorded amount; no payment is made |
| A2 | An officer may reject an unresponsive claimant's claim with a reason; no automatic deadline |
| A3 | Additional information is text only |
| A4 | The assessed amount may exceed the claimed amount |
| A5 | Re-assessment is allowed in InReview; the latest counts and history keeps earlier ones |
| A6 | A partial settlement is Settled with a lower assessed amount |
| A7 | No claimant withdrawal or editing |
| A8 | No release or reassignment; an absent officer's claims cannot move |
| A9 | No appeal or reopening |

### Developer decision

**Developer decision: Approved with adjustments**

- **Accepted:** The five statuses (the developer's own proposal), T1–T8, separate assignment, assessment as an action, terminal statuses, information request rules, ClaimHistory and visibility rules, validation rules and error codes as adjusted, and A1–A9.
- **Confirmed (T8):** Approved with a mandatory non-empty reason. The AI's proposal already required a reason for rejection; the developer stated it explicitly for T8.
- **Challenged (settlement amount):** The AI proposed that the assessed amount becomes the final amount. The developer required a separate `SettlementAmount`, initially equal to it, with the final amount and currency in history.
- **Overridden (incident date):** The AI proposed allowing up to one day ahead of UTC. The developer required the claim market's local date instead.
- **Reasoning:** The developer did not state separate reasons for the adjustments, so none are recorded here. (Reasoning was confirmed later; see "Follow-up decisions".)

### Open point

Using the market's local date requires a time zone for each demo market. Some countries, such as Australia, have several. The choice has not been approved. (Resolved later; see "Follow-up decisions".)

### Follow-up decisions

The AI recommended representative time zones and asked what "initially" meant for `SettlementAmount`. The developer then approved the following. None of it is prescribed by the assessment.

**Market time zones (developer assumptions for the MVP)**

| Market | Time zone |
|---|---|
| Malaysia | `Asia/Kuala_Lumpur` |
| Singapore | `Asia/Singapore` |
| Australia | `Australia/Sydney` |
| Hong Kong | `Asia/Hong_Kong` |
| Japan | `Asia/Tokyo` |
| New Zealand | `Pacific/Auckland` |

These are representative zones, not confirmed operating locations or complete national time-zone coverage. Australia, for example, has multiple time zones. A production system should determine the time zone from the incident's actual location, because the incident date should be validated against the local date where the incident occurred.

The developer first approved wording based on the claimant's actual location. The AI raised the distinction between claimant location and incident location, and the developer approved using the incident's actual location.

**SettlementAmount (developer decision)**

- `SettlementAmount` is null before settlement.
- When T6 (settle claim) occurs, the latest `AssessedAmount` is copied into `SettlementAmount`.
- The final settlement amount and currency are recorded in `ClaimHistory`.

This clarifies "initially equals" in the approved rules above.

**Developer reasoning (confirmed, verbatim)**

- **Settlement amount:** "I want to keep the assessed amount separate from the settlement amount so that the system distinguishes the officer's assessment from the final amount approved for settlement."
- **Incident date:** "I want to validate the incident date using the claim market's local date so that claims are not incorrectly rejected because the server uses UTC."

---

## Entry 4: Backend project setup (Phase 3, Task 1)

- **Date:** 2026-11-25
- **Task:** Create the ASP.NET Core solution, API project and test project
- **AI tool:** Claude Code (Claude Opus 5.5)
- **Order:** This work was completed before the lifecycle design in Entry 3 and is recorded here afterwards.

### Request and process

The developer asked the AI to inspect the project, check the .NET templates and explain its commands before creating anything. The AI proposed the commands and structure. The developer approved them with clarifications, including HTTP for initial local development, the `.slnx` format, and no EF Core, SQLite, JWT or Kafka packages yet. The AI then ran the commands.

### Completed work

1. Created the .NET 10 solution, `ClaimFlow.slnx`.
2. Created the ASP.NET Core Web API project, `ClaimFlow.Api`.
3. Created the xUnit test project, `ClaimFlow.Tests`, with a project reference to the API.
4. Removed unused template files and the unnecessary `coverlet.collector` dependency.
5. Verified that the solution built successfully with 0 warnings and 0 errors.
6. Started the API locally and confirmed that `/openapi/v1.json` returned HTTP 200.
7. Stopped the API after verification.

No tests exist yet, so none were run.

### Environment note

- During the initial .NET SDK inspection, the SDK automatically installed an ASP.NET Core HTTPS development certificate in the Windows user's certificate store.
- This was an environment-level side effect, not a project dependency or repository file.
- The AI disclosed this to the developer, and the developer chose to leave the certificate in place.

### Requirements and developer decisions

- **Assessment (p.2):** C#/.NET is one of the preferred backend options. The brief also says to start with the backend and leaves testing libraries to the developer.
- **Developer decisions:** .NET 10, xUnit, the solution structure, the `.slnx` format and HTTP for initial local development. The assessment does not prescribe these.

### Current implementation boundary

- No database or EF Core integration yet.
- No claim entities, endpoints or business logic yet.
- No authentication or authorisation implementation yet. The template's `UseAuthorization()` call is present but protects nothing.
- No Kafka implementation or dependencies yet.

### Developer decision

**Developer decision: Setup plan approved before execution**

- **Accepted:** The proposed commands and project structure, with the developer's clarifications.
- **Challenged:** None.
- **Overridden:** None.

### Time tracking

Project scaffolding and environment preparation have started. Feature implementation has not started. No time-spent figures are recorded.

---

## Entry 5: EF Core and SQLite setup

- **Date:** 2026-11-25
- **Task:** Phase 4, Stage A: EF Core and SQLite setup
- **AI tool:** Claude Code (Claude Opus 5.5)

### Request and process

The developer asked for a setup proposal before any changes. The AI proposed two stages: A (packages, empty DbContext, configuration) and B (entities and first migration). The developer approved Stage A with clarifications, asked for the exact files and commands, and then confirmed execution.

### Completed work

1. Installed `Microsoft.EntityFrameworkCore.Sqlite` 10.0.15.
2. Installed `Microsoft.EntityFrameworkCore.Design` 10.0.15 with `PrivateAssets="all"` (stored as a nested element in the project file).
3. Created a local tool manifest at `backend/dotnet-tools.json`.
4. Installed local `dotnet-ef` version 10.0.15.
5. Created the empty `ClaimFlowDbContext`.
6. Registered the DbContext in `Program.cs`.
7. Configured the SQLite connection string in `appsettings.json`.
8. Resolved the default database path using ASP.NET Core `ContentRootPath`.
9. Added support for the `ConnectionStrings__ClaimFlow` environment-variable override, using ASP.NET Core's standard configuration.

### Verification results

- Build succeeded with 0 warnings and 0 errors.
- `dotnet ef --version` returned 10.0.15.
- EF Core successfully discovered `ClaimFlow.Api.Data.ClaimFlowDbContext`.
- Default SQLite data source resolved inside the API project folder.
- Environment-variable override successfully changed the data source to `C:\Temp\override-test.db`.
- API started successfully and `/openapi/v1.json` returned HTTP 200.
- No database file was created.
- No migrations were created.
- Vulnerability check found no known vulnerable packages.
- `git diff --check` passed (it covers changes to tracked files, not new untracked files).
- `dotnet ef dbcontext info` warned that no entity configurations were found, as expected with an empty model.
- Tests were not run because no tests exist yet.

### AI review and developer decisions

- The initial database path recommendation was clarified to use `ContentRootPath` rather than relying on the current working directory.
- The AI then corrected its own earlier claim that the same file would always be used: `ContentRootPath` is set from the working directory by default, so some ways of starting the API can resolve a different path.
- The developer approved documenting `dotnet run` as the supported local startup method for this configuration.
- The default database path and environment override were verified.
- The implementation followed the approved Stage A scope.

### Requirements and developer decisions

- **Assessment (p.2):** The database is left to the developer.
- **Developer decisions:** SQLite, EF Core 10.0.15, the folder structure, no repository layer and the path approach. The assessment does not prescribe these.

### Current implementation boundary

- No claim entities or entity configurations.
- No database migrations or database file.
- No authentication implementation.
- No Kafka implementation.
- No repository layer.
- No feature implementation yet.

---

## Entry 6: Claim domain and database design (Phase 5)

- **Date:** 2026-11-25
- **Task:** Entity, relationship and constraint design
- **AI tool:** Claude Code (Claude Opus 5.5)
- **Output:** Design discussion only. No entities, migrations or other code.

### Assessment requirements and developer assumptions

- **Assessment:** p.3 asks what the core entities are and how they relate. It names no entities, fields, tables or constraints. p.2 names motor and property claims and the claimant and staff activities.
- **Developer decisions and assumptions:** everything under "Approved design" below.

### Request (condensed)

The developer asked for a design of User, Claim, ClaimHistory and InformationRequest covering fields, relationships, keys, indexes, constraints, enums, dates, money, versioning and the one-open-request rule. Constraints: no extra entities without clear reason, no repository layer, and "Identify any conflicts or missing decisions instead of guessing."

### AI proposal

- Four entities, with no additions.
- Foreign keys restrict deletion; indexes, check constraints and a filtered unique index.
- Ten open questions (Q1–Q10), including a gap: a version check at save time does not catch a user acting on an old page. Example: an officer reassesses a claim in one tab and settles from another that still shows the old amount.
- The AI named Q3, Q5 and Q8 as the decisions most worth challenging.

### Approved design

**Entities** (`?` = optional)

| Entity | Fields |
|---|---|
| User | Id, Email, DisplayName, PasswordHash, Role |
| Claim | Id, ClaimantId, AssignedOfficerId?, Type, Market, CurrencyCode, Status, IncidentDate, Description, VehicleRegistration?, PropertyAddress?, ClaimedAmountMinor, AssessedAmountMinor?, SettlementAmountMinor?, RejectionReason?, SubmittedAtUtc, DecidedAtUtc?, Version |
| ClaimHistory | Id, ClaimId, Action, FromStatus?, ToStatus, ActorId, ActorRole, OccurredAtUtc, Note?, AmountMinor?, CurrencyCode?, InformationRequestId? |
| InformationRequest | Id, ClaimId, RequestedById, Question, RequestedAtUtc, Status, Response?, RespondedAtUtc? |

**Relationships**
- A claim has one claimant and zero or one assigned officer.
- A claim has one or more history rows and zero or more information requests.
- A history row may reference one information request. **One information request can be referenced by multiple history rows**, such as T4 and T5.
- Each history row has one actor; each information request has one requesting officer.
- Deleting a referenced row is refused.

**Indexes:** unique User email; Claim by claimant, by status, and by assigned officer with status; ClaimHistory by claim and Id; InformationRequest by claim, plus a unique index on claim where status is Open.

**Check constraints**
- A claim has no officer if and only if it is Submitted.
- Claimed amount above zero; other amounts above zero when present.
- Settlement amount present if and only if Settled.
- Rejection reason present if and only if Rejected.
- Decision time present if and only if Settled or Rejected.
- Vehicle registration for Motor only; property address for Property only.
- Response and response time present if and only if the request is Answered.

**Representation:** enums as text; market as an enum with a fixed lookup for currency, decimal places and time zone; no money value object; UTC `DateTime` for timestamps; `DateOnly` for incident date; `long` minor units; `int` version increased by every transition.

**One open information request:** lifecycle rule, version check and filtered unique index. Request status is Open, Answered or Closed; T8 closes the open request.

**Limits of the database:** application code must check that the assigned user is an officer, that transitions are legal, and that a history row's request belongs to the same claim. SQLite does not enforce text lengths.

### Developer decisions on Q1–Q10

| # | Topic | Decision |
|---|---|---|
| Q1 | History detail | Approved: AmountMinor, CurrencyCode and InformationRequestId added to ClaimHistory where applicable |
| Q2 | Rejection reason | Approved: on Claim for current state and in ClaimHistory for the event |
| Q3 | Keys | Approved: int. Access control must never rely on IDs being difficult to guess |
| Q4 | Type detail | Approved: VehicleRegistration for Motor, PropertyAddress for Property |
| Q5 | Policy number and incident location | **Adjusted** (below) |
| Q6 | Maximum amount | Approved: 1,000,000,000,000 minor units per claim, as a technical safeguard. A developer assumption, not a business rule from the assessment |
| Q7 | Append-only history | Approved: enforced through application design, without SQLite triggers. Limitation: direct database edits remain possible |
| Q8 | Stale versions | **Strongly approved:** every action on an existing claim must include the version observed by the client; stale versions get HTTP 409 |
| Q9 | Text limits | Approved: email 254, name 100, registration 20, address 300 |
| Q10 | Past incident dates | Approved: no arbitrary restriction |

### Developer decision

**Developer decision: Approved with adjustments**

- **Accepted:** Decisions 2–6 (relationships, indexes and constraints, enums, representation, open-request rule); Q1–Q4 and Q6–Q10.
- **Adjusted (entities and Q5):** The AI proposed leaving out both policy number and incident location. The developer left PolicyNumber out of the MVP and kept the approved representative market time zones, but retained incident location as a production requirement, because production incident-date validation should use the actual incident location. **This production capability is not implemented in the MVP.**
- **Clarified (history and requests):** The developer required that one information request can be referenced by multiple history events. The AI's table linked both T4 and T5, but its diagram did not make this clear.
- **Conditions added (Q3, Q6, Q7):** Access control must not rely on hard-to-guess IDs; the amount limit is labelled an assumption; the direct-edit limitation is documented.
- **Overridden:** None.

### Follow-up clarifications

The AI asked two questions after the decisions above, and recommended that T8 also reference the request it closes. The developer approved both clarifications.

**Incident location**
- `IncidentLocation` is not included in the MVP Claim entity.
- It is retained as a documented production requirement.
- MVP incident-date validation uses the approved representative market time zones.

**T8 history reference**
- When T8 rejects a claim from AwaitingInformation, its ClaimHistory entry must reference the request being closed through `InformationRequestId`.
- The request status changes to Closed in the same transaction as the rejection.
- The many-to-one relationship is preserved: multiple ClaimHistory records may reference the same InformationRequest.
- `InformationRequestId` in ClaimHistory must not be unique.

T4, T5 and T8 therefore reference a request; other history entries do not.

### Current implementation boundary

No entities, entity configurations, migrations or database file exist yet.

---

## Entry 7: Domain entities and claim lifecycle (Phase 6A and 6B)

- **Date:** 2026-11-25
- **Task:** Implement the approved entities, EF Core configurations and T1–T8 lifecycle
- **AI tool:** Claude Code (Claude Opus 5.5)

### Request and process

The developer asked for an implementation plan first. The AI proposed three steps: 6A (entities and configurations), 6B (lifecycle, market lookup, unit tests) and 6C (first migration and database constraint tests). The developer approved the plan, including the seven `ClaimAction` values and `Version` starting at 1, and then approved 6A and 6B separately. For 6B the developer stated that approximately one hour remained and prioritised "a correct, testable claim lifecycle over additional features."

### Phase 6A: completed work

- Created four entities, six enums and four EF Core configuration classes.
- Added four `DbSet`s, enum-to-text conversion and a UTC marker on reading timestamps.
- Configured `Version` as a concurrency token and Restrict delete behaviour for all relationships.
- Gave the two `InformationRequest.ClaimId` indexes distinct names, because unnamed indexes on the same column overwrite each other in EF Core.

**Verification:** build with 0 warnings and 0 errors. The generated SQL contained four tables, ten check constraints, seven Restrict foreign keys and the approved indexes, including the filtered unique index for one open request. The API started and `/openapi/v1.json` returned HTTP 200. No database or migration was created.

**Failed command:** the AI's first attempt to create the files in one shell command failed with a quoting error. It confirmed that nothing had been created, then wrote the files individually.

### Phase 6B: completed work

- Implemented T1–T8 as methods on `Claim`, with role, ownership, assigned-officer, version, status and input checks.
- Each action uses a server UTC time and adds one history row. T2–T8 also increment `Version`.
- T4, T5 and T8 history rows reference their information request; T8 closes it.
- Implemented the six-market lookup for currency, decimal places and time zone (developer assumptions).
- Added `ClaimRuleException`, with error types intended to map to 400, 403, 404 and 409.

**Verification:** build with 0 warnings and 0 errors; **48 tests passed, 0 failed**. The EF model remained valid. No packages were added.

### Error precedence decision

The approved design did not say which error applies when several do. The AI recommended an order, and the developer approved it.

- **Developer decision, approved after AI recommendation:** Permission → Version → Status → Input validation.
- **Reason:** avoid disclosing claim state to users who do not have permission.
- **Example:** an officer acting on another officer's settled claim gets 403, not 409.

The assessment does not prescribe this.

### Developer decision

**Developer decision: Phase 6A plan approved before execution; Phase 6B review approved**

- **Accepted:** The 6A/6B/6C split, files and namespaces, enum values, navigations, constraints, and the error precedence order.
- **Challenged:** None.
- **Overridden:** None.

### Not yet verified or implemented

- Database persistence, transaction behaviour and simultaneous database updates remain unverified. The 48 tests exercise the domain rules and market lookup in memory only.
- The database constraints are defined but have not been tested against a real database.
- No service or HTTP endpoints exist yet.
- No authentication, Kafka or frontend exists yet.
- User fields are not validated beyond storing email in lowercase.

---

## Entry 8: SQLite migration and persistence verification (Phase 6C)

- **Date:** 2026-11-25
- **Task:** First EF Core migration and SQLite integration tests
- **AI tool:** Claude Code (Claude Opus 5.5)

### Request and process

The developer limited this phase to the migration and persistence tests, required EF Core migrations rather than `EnsureCreated`, and told the AI not to change the approved domain design to make tests pass or claim behaviour the tests do not demonstrate. No domain code changed.

### Migration

- **Name:** `20261125051429_InitialCreate`, created with the local `dotnet-ef` tool.
- Creates four tables, seven restrictive foreign keys and ten check constraints.
- Includes the unique email index, the ordinary `InformationRequest.ClaimId` index and the filtered unique index for one open request per claim.
- EF Core reported no pending model changes.
- `.gitignore` was updated with `*.db-journal`; the `.db`, `.db-wal` and `.db-shm` patterns already existed.

### Integration tests

29 tests use in-memory SQLite, with a connection kept open for each test. Data is reloaded through a new DbContext.

| Coverage | Tests |
|---|--:|
| Migration applies and matches the model | 1 |
| Claim and history saved and loaded; stored value formats | 2 |
| Settlement amounts, decision time and history | 1 |
| Request and response history share one request | 1 |
| T8 rejection, closure and history saved together | 1 |
| T8 rollback, forcing a failure at each of three write points | 3 |
| Stale version detected using two copies of the same claim | 1 |
| Claim check constraints | 9 |
| Information request answer constraint | 3 |
| One open request per claim | 1 |
| Unique email | 1 |
| Foreign keys enforced | 5 |

### Results

- Build: 0 warnings and 0 errors.
- **77 tests passed, 0 failed** (48 existing and 29 new).
- No persistent database file was created.

### Remaining unverified

- Truly simultaneous writes and SQLite locking. The stale-version test is sequential on one connection.
- File-based database and OneDrive behaviour.
- Rollback for actions other than T8.
- Reverse migration.
- Startup migration and demo-user seeding, which are not implemented yet.
- Each Restrict foreign key in isolation. The tests show that deletes are refused, not which of several keys refused them.

### Developer decision

**Developer decision: Phase 6C review approved, based on the reported results**

- **Accepted:** The migration, integration tests and `.gitignore` change.
- **Challenged:** None.
- **Overridden:** None.
