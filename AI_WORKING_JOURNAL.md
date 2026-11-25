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

---

## Entry 9: HTTP API and authentication (Phase 7A)

- **Date:** 2026-11-25
- **Task:** Minimal HTTP API and demo authentication, as one working vertical slice
- **AI tool:** Claude Code (Claude Opus 5.5)

### Request and process

The developer specified the endpoints, authentication, startup behaviour and tests, and asked the AI to preserve the domain rules and the existing 77 tests. The AI left the domain entities and migration unchanged and identified one conflict with an earlier decision (see "Developer decisions", item 1).

### Assessment requirements and developer decisions

- **Assessment (p.2):** names the claimant and staff activities and lists REST/HTTP under preferred technology. It does not mention authentication or prescribe any endpoint, token format or visibility rule.
- **Developer decisions:** JWT, roles, endpoints, visibility rules and startup behaviour described below.

### Implemented endpoints

| Endpoint | Role | Behaviour |
|---|---|---|
| `POST /api/auth/login` | Anyone | Same error for an unknown email and a wrong password |
| `POST /api/claims` | Claimant | Submit a claim |
| `GET /api/claims` | Claimant | Own claims only |
| `GET /api/claims/{id}` | Claimant | Own claim only; otherwise 404 |
| `GET /api/claims/{id}/history` | Claimant | Own claim only; assessment events left out |
| `GET /api/officer/claims` | ClaimsOfficer | Submitted claims and claims assigned to this officer |
| `GET /api/officer/claims/{id}` | ClaimsOfficer | Any claim |
| `GET /api/officer/claims/{id}/history` | ClaimsOfficer | Any claim |
| `POST /api/officer/claims/{id}/pickup` | ClaimsOfficer | Requires the expected version; 409 if stale or already taken |

### Authentication and startup

- Passwords are hashed with ASP.NET Core's `PasswordHasher`.
- One-hour JWT containing user ID (`sub`) and `role`. Issuer, audience, signature, algorithm and expiry are validated.
- Every endpoint requires a token by default, except login and the Development-only OpenAPI document.
- The signing key and demo passwords exist only in `appsettings.Development.json`, labelled development-only.
- In Development only: migrations are applied, then two demo users are seeded, skipping any that exist.
- Claim changes and history are saved with one `SaveChanges` call. Domain errors and stale updates map to 400, 401, 403, 404 and 409.

### Packages added

| Package | Version | Project |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.15 | ClaimFlow.Api |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.15 | ClaimFlow.Tests |

### Results

- Build: 0 warnings and 0 errors.
- **101 tests passed, 0 failed** (77 existing and 24 new API tests).
- Vulnerability check found no known vulnerable packages.
- **Real HTTP check:** the API ran against a temporary file database outside the project folder. Login, submission and pickup worked, a stale pickup returned 409 and a request without a token returned 401. A second startup with existing data also worked. The temporary database was then deleted.

**Security tests**
- **Forged role:** a token with only its role changed is rejected. The AI's first version of this test was too weak, because the forged token had no expiry and would have been rejected anyway. The AI found and corrected this.
- **Hidden assessment data:** Claimant responses contain no assessment data and no officer names or IDs. The officer's role may appear in history. The automated visibility test currently covers a claim in review. The code also applies the no-assessment-visibility rule after settlement, but that specific scenario has not yet been independently tested.
- **Unauthenticated access:** every claim endpoint returns 401 without a token and 403 with the wrong role.
- **Production startup:** does not migrate or seed, and fails without its own signing key.

### Developer decisions

1. **Assessment visibility:** claimants never see internal assessments or `AssessedAmount`, including after settlement. They see `SettlementAmount` when settled. This replaces the Entry 3 rule, which hid assessments only until settlement. The AI raised the conflict and implemented the stricter rule; the developer approved it.
2. **Database location:** keep the default location usable for reviewers, and use the environment override for local development outside OneDrive.
3. **Officer access:** officers can view any claim, but can only act on claims assigned to them, except picking up an unassigned claim.

**Developer decision: Phase 7A review approved**

- **Accepted:** The implementation, tests, packages and the three decisions above.
- **Challenged:** None.
- **Overridden:** None.

### Remaining limitations

- T3–T8 endpoints are not implemented.
- No dashboard or exposure API.
- No login rate limiting or token revocation.
- CORS is not configured yet.
- Truly parallel pickup is not tested.

---

## Entry 10: Complete claim lifecycle HTTP API (Phase 7B)

- **Date:** 2026-11-25
- **Task:** Add the remaining lifecycle endpoints (T3–T8) and tests
- **AI tool:** Claude Code (Claude Opus 5.5)

### Request and process

The developer asked for the remaining endpoints using the existing domain methods, without duplicating lifecycle rules or changing the domain model or migration, and to "Prioritize correctness and security over adding extra features." The domain model and migration were not changed and no packages were added.

### Assessment requirements and developer decisions

- **Assessment (p.2):** staff review and assess claims and progress them to settlement or rejection; claimants provide additional information when asked and receive decisions.
- **Developer decisions:** the endpoint paths, version checks, visibility rules and status codes. The assessment does not prescribe these. The AI chose the paths to match the existing `pickup` endpoint.

### Endpoints added

| Endpoint | Role | Transition |
|---|---|---|
| `POST /api/officer/claims/{id}/assessment` | Assigned officer | T3 |
| `POST /api/officer/claims/{id}/information-requests` | Assigned officer | T4 |
| `POST /api/claims/{id}/information` | Claim owner | T5 |
| `POST /api/officer/claims/{id}/settle` | Assigned officer | T6 |
| `POST /api/officer/claims/{id}/reject` | Assigned officer | T7, T8 |

### Implementation

- Every existing-claim action, including pickup, uses one shared `ClaimService` method: load the claim, call the domain method, save once.
- Information requests are always loaded with the claim, so they are available for T5 and T8.
- Every existing-claim action requires `expectedVersion` and returns the updated version. Stale versions return 409.
- Question, response, reason and amount are checked by the domain, not by request validation, so that the approved error order is kept.

### Tests and results

- Build: 0 warnings and 0 errors.
- **119 tests passed, 0 failed** (101 existing and 18 new).
- **Lifecycle:** success cases, assignment and ownership, invalid input, rejection from InReview and AwaitingInformation, T8 closure and history, stale versions, and error order for requests wrong in several ways.
- **Settled-claim visibility:** the claimant sees `SettlementAmount` but no `AssessedAmount` or assessment history. The test uses an earlier assessment (54,321) that differs from the settlement amount (80,000), so that equal amounts cannot hide a leak. This closes the gap recorded in Entry 9.
- **Persistence:** two stale information requests for the same claim were tested one after the other. The second fails as a concurrency conflict and saves nothing.

### Developer decisions

1. **Information requests in claimant responses:** these include questions, answers and status, but not officer names or IDs. The AI added this because a claimant cannot answer a question they cannot see; the developer approved it.
2. **Structural validation:** A missing or malformed `expectedVersion` returns 400 before assignment and ownership checks. Authentication (401) and role authorisation (403) still occur before the request body is processed. Permission-first ordering applies after structural validation. The AI reported this exception to the Entry 7 order; the developer approved it.

**Developer decision: Phase 7B approved**

- **Accepted:** The implementation, tests and the two decisions above.
- **Challenged:** None.
- **Overridden:** None.

### Remaining limitations

- No dashboard or exposure API.
- Truly parallel requests are untested.
- No new file-database verification in this phase.
- CORS, login rate limiting and token revocation remain unimplemented.
- The README has not been created yet.

---

## Entry 11: Claims dashboard and liability exposure API (Phase 8)

- **Date:** 2026-11-25
- **Task:** Team dashboard and liability exposure endpoints
- **AI tool:** Claude Code (Claude Opus 5.5)

### Request and process

The developer specified the two endpoints, figures, exposure rules and tests, and asked the AI to prefer database aggregation, add indexes only if query inspection showed a need, and not add a Manager role. No domain entity, migration, index or package was changed.

### Assessment requirements and developer decisions

- **Assessment:** staff see their team's workload and performance (p.2). Outstanding liability exposure is named as a problem (p.2) and a question to consider (p.3). The brief defines no metric, formula or dashboard layout.
- **Developer decisions and interpretations:** every figure, definition and endpoint below.

### Endpoints

| Endpoint | Role | Returns |
|---|---|---|
| `GET /api/officer/dashboard` | ClaimsOfficer | Team figures, counts by status, one row per officer |
| `GET /api/officer/dashboard/exposure` | ClaimsOfficer | One row per currency |

Requests without a token return 401; claimants receive 403.

### Calculation definitions

| Figure | Definition |
|---|---|
| Total | All claims |
| Unassigned | Claims with no officer |
| Open | Submitted, InReview or AwaitingInformation |
| Decided | Settled or Rejected |
| Officer: open assigned | InReview or AwaitingInformation |
| Officer: awaiting information | Part of open assigned, shown separately |
| Officer: decided | Settled or Rejected |
| Officer: average time to decision | Mean elapsed time from submission to decision, in seconds, including time waiting for the claimant; null with no decisions |
| Exposure | Open claims only: assessed amount if present, otherwise claimed amount |

- Exposure is grouped by currency, with a claim count and **no total across currencies**.
- Amounts stay in minor units. Each row includes the currency's decimal places (JPY has none).
- Figures cover all time. Officers with no claims appear with zeros.

### Queries

Inspected in the generated SQL:
- Counts and exposure use `GROUP BY`, `COUNT` and `SUM` in the database.
- The average reads three columns of decided claims and is calculated in C#, because EF Core does not translate date subtraction for SQLite.

### Tests and results

- Build: 0 warnings and 0 errors.
- **129 tests passed, 0 failed** (119 existing and 10 new).
- Coverage: access, empty dashboard, status and officer counts, average time, claimant waiting time, assessed and claimed amounts, separate currencies, exclusion of decided claims, and no emails, password hashes or claimant names.
- **Initial failure:** one new test failed because the AI's test helper used an email as an officer's display name. The dashboard was correct; the AI fixed the helper and made the check stricter.
- **Real HTTP check:** the API ran against a temporary file database. Submit, pickup, assessment, settlement, dashboard and exposure all worked. The temporary database was then deleted.

### Developer decision

**Developer decision: Phase 8 implementation approved, based on the reported results**

- **Accepted:** The endpoints, calculations, queries and tests.
- **Challenged:** None.
- **Overridden:** None.

### Limitations

- Dashboard counts and averages use separate queries, so a decision made between them could differ for one request.
- No date filter or market breakdown.
- No large-volume performance testing.
- Angular and README are not implemented.

---

## Entry 12: Angular foundation and authentication (Phase 10A)

- **Date:** 2026-11-25
- **Task:** Create the Angular application, login, role-based navigation and basic layouts
- **AI tool:** Claude Code (Claude Opus 5.5)

### Purpose and process

The developer asked for a frontend foundation that uses the actual backend contract ("Do not guess URLs, response property names or roles"), with login for both roles, guards, an interceptor and placeholder layouts. Claim screens were excluded and backend files were not to be modified. The AI inspected the launch settings, login DTOs, JWT claims and roles before writing code.

### Assessment requirements and developer decisions

- **Assessment (p.2):** Angular is the preferred frontend, and both user types access the same Angular application. Frontend architecture is left to the developer.
- **Developer decisions:** location, standalone components, strict TypeScript, no UI or chart libraries, development proxy instead of CORS, and the navy, white and teal design.

### Implementation

- Angular 22.3.0, the current stable release, whose supported Node versions include the installed 26.7.0. Created with `npx`, so there is no global install.
- One application with lazily loaded `/claimant` and `/officer` areas that share a page shell.
- API requests use relative `/api` paths. `proxy.conf.json` forwards them to `http://localhost:5032`, taken from the backend launch settings.
- Responsive login page with validation, show/hide password, loading state and friendly errors. No registration or forgot-password function.
- The generated sample page, icon, README and `.vscode` folder were removed.

### Authentication behaviour

| Part | Behaviour |
|---|---|
| Session storage | Login response kept in `sessionStorage`: survives a refresh and ends with the tab. Expired, damaged and unknown-role sessions are ignored. Passwords are not stored |
| Interceptor | Adds the token only to ClaimFlow API requests, not to login or other sites |
| 401 | Ends the session and opens login with a message. A 401 from login itself means wrong credentials |
| 403 | Keeps the session and shows the API's reason |
| Guards | No session leads to login; the wrong role leads to the user's own area. They guide navigation; the API enforces access |
| Logout | Clears the session and opens login |

### Tests and verification

- Angular build: no errors. `npm audit`: 0 vulnerabilities.
- **20 frontend unit tests passed.**
- **Backend unchanged: 129 tests passed.**
- **Browser check:** 20 of 20 checks passed in headless Chrome with the Angular development server, proxy, real API and a temporary database, with no browser errors. It covered login for both roles, a wrong password, blocked areas, every menu link, logout, real 200 and 403 responses through the proxy, and phone width. It was a temporary script and is not in the repository.
- With the API stopped, the proxy returned 502. The login page's server-error message was unit-tested with status 500.
- The AI reviewed screenshots of the desktop login error, phone login and officer pages.

### Decisions

- **Accepted:** The developer accepted Phase 10A for finalisation and commit, subject to a clean final review.
- **Challenged:** None.
- **Overridden:** None.
- **AI choice, different from the earlier plan:** the officer area is `/officer` rather than the `/staff` path in the architecture proposal. Reason: the Phase 10A request calls it the officer area and it matches the API paths.
- **Trade-off:** `sessionStorage` is readable by a script injected into the page. This was stated in the architecture proposal that the developer approved.

### Known limitations

- The 401 and 403 messages are unit-tested but not tested through the browser interface.
- Session expiry is noticed on the next navigation or request, not by a timer.
- The proxy works only in development; deployment is not configured.
- All pages except login and Account are placeholders.
- The README still says there is no user interface and that Node.js is not needed. It must be updated.

---

## Entry 13: Project README (Phase 9, retrospective)

- **Date of work:** 2026-11-25
- **Task:** Root README and local setup documentation
- **AI tool:** Claude Code (Claude Opus 5.5)
- **Order:** Phase 9 was completed and committed before Phase 10A (Entry 12). It had no journal entry at the time. The AI pointed out the gap after Phase 10A, and this entry was then added at the developer's request.

### Request and process

The developer asked for a concise README for the assessment panel, with every command and credential verified against the actual code, no invented features, and no changes to application code or configuration.

### Assessment requirements and developer decisions

- **Assessment (p.3):** a working application that can be started locally, and any supporting documentation the developer feels is appropriate. A README is not specifically required.
- **Developer decisions:** the README's content, structure and assumptions wording.

### Completed work

- Created the root `README.md` (239 lines), committed as `db555d5 docs: add project README`.
- It documented features, architecture, project structure, backend setup, demo accounts, database behaviour, tests, API endpoints, design decisions, assumptions and limitations.
- At that point the frontend had not been implemented. The README stated that there was no user interface, no registration and no Kafka implementation.

### Verification

Tested from a clean temporary clone, which was then deleted:
- `dotnet run` started the API and created the default database, which Git ignores.
- The "Try it" script ran as written.
- All 16 documented endpoints matched the live OpenAPI document.
- 129 tests passed.
- The migration tool commands and the database path override worked.

**Limits:** the override was tested with a different folder from the documented `C:\Temp`, and NuGet packages were already cached. After these tests the AI redrew the lifecycle diagram, because one arrow pointed to nothing, and clarified that assessment is required before settlement. No commands changed.

### Credential search: timing

- The credential search was part of the staged review, but **it failed to run before the commit** because of an error in the AI's search pattern. The AI's command did not stop on that failure, and the commit went ahead without the check.
- The AI ran the search immediately afterwards against the committed README. It found only the two intended demo logins. No signing key value, personal names, local user paths or tokens were included.
- The whitespace check and the check that only `README.md` was staged did pass before the commit.
- The AI reported the failure to the developer. For Phase 10A, the secret search was run and reviewed before committing.

### Decisions

**Developer decision: README approved**

- **Accepted:** The README content and the reported verification.
- **Challenged:** The AI noted that the market disclaimer mentioned the insurer's name and that the brief bars its material and branding (p.3). The developer directed neutral wording: "not confirmed operating markets of any particular insurer."
- **Overridden:** None.

### Status

The working tree was clean after the commit. The README has not yet been updated for the Angular application.

---

## Entry 14: Claimant claim screens (Phase 10B)

- **Date:** 2026-11-25
- **Task:** My Claims, claim submission and claim details for claimants
- **AI tool:** Claude Code (Claude Opus 5.5)

### Purpose and scope

The developer asked for a claimant to view their own claims, submit Motor and Property claims, and see a claim's details, status and history, using the actual API contract and without changing the backend. The AI reported the endpoints, DTOs, required fields and backend limitations before coding. Officer screens were excluded.

**This phase does not complete the claimant journey. A claimant cannot yet answer information requests through the UI.** Questions are shown, but there is no reply form. This is a known incomplete requirement, to be addressed in Phase 10C.

### Assessment requirements and developer decisions

- **Assessment (p.2):** claimants report an incident, track their claim, provide additional information when asked and receive decisions.
- **Covered in the UI:** reporting, tracking and receiving decisions. **Not yet covered in the UI:** providing additional information.
- **Developer decisions and AI implementation choices:** page design, validation messages, reference format and formatting rules.

### Implementation

| Part | Content |
|---|---|
| Pages | `my-claims`, `submit-claim`, `claim-details` |
| Service | `ClaimantClaimsService`: all claim API calls |
| Models | `claim.models.ts`: claimant request and response models matching the backend DTOs |
| Helpers | `claim-format.ts` (reference and money), `markets.ts`, `api-error.ts` |
| Shared | `status-badge.ts`; small phone layout fix to the page shell |
| Endpoints used | `GET /api/claims`, `POST /api/claims`, `GET /api/claims/{id}`, `GET /api/claims/{id}/history` |

- **Motor and Property:** only the fields for the chosen type are shown, validated and sent. Limits match the backend, which checks every rule again. File uploads are not offered.
- **Submission:** the form accepts one request at a time. API errors are shown without losing the form; server error details are not shown.
- **Money:** the currency follows the market. Text is converted to minor units without floating-point arithmetic. JPY accepts no decimals.
- **Incident date:** checked against today's date in the market's time zone, as the API does.
- **History and visibility:** history is shown oldest first. The models contain no assessment or officer identity fields, and the page shows only known claimant events.
- **Accessibility:** labelled fields, linked error messages, focus moved to the first invalid field, and statuses shown as text as well as colour.

### Tests and verification

- Angular production build: no errors.
- **58 frontend unit tests passed** (38 new). **129 backend tests passed**; the backend is unchanged. No package was added.
- **Browser check: 24 of 24 passed** in headless Chrome with the development server, proxy, real API and a temporary database, with no uncaught errors. It was rerun after the phone layout fix with the same result. It is a temporary script and is not in the repository.
- It covered Motor and Property submission, three rapid submits creating one claim, questions, settlement and rejection reasons, no assessment or officer name before or after settlement, HTML shown as plain text, phone widths, and a rejected token returning the user to login. The last point closes the browser-level 401 gap in Entry 12.

### Decisions

- **Accepted:** The developer accepted Phase 10B for finalisation and commit, subject to a clean review.
- **Directed by the developer:** tidy the phone header, where the log out button wrapped to a second line.
- **Challenged:** None.
- **Overridden:** None.

### Limitations and trade-offs

- No UI for answering information requests (above).
- If the connection fails after the server saves a claim, resubmitting could create a duplicate. The API cannot recognise a repeat.
- Market settings are copied from the backend, which has no market endpoint. A later backend change would not be detected.
- No automatic refresh or paging.
- Timestamps are shown in the viewer's time zone.
- The 403 message has not been tested through the browser interface.

---

## Entry 15: Claimant information responses (Phase 10C)

- **Date:** 2026-11-25
- **Task:** Let a claimant answer an officer's information request in the UI
- **AI tool:** Claude Code (Claude Opus 5.5)

### Purpose and process

This phase addresses the incomplete requirement recorded in Entry 14. The developer required the actual backend contract to be reported before coding, no automatic retry after an uncertain failure, and no backend changes.

### Assessment requirements and developer decisions

- **Assessment (p.2):** claimants provide additional information when asked. With Entry 14, the UI now covers all four claimant activities named in the brief.
- **Developer decisions and AI implementation choices:** the form design and the handling of conflicts and uncertain results.

### API contract

- `POST /api/claims/{id}/information` with `{ expectedVersion, response }`; returns 200 with the updated claim.
- No request ID is sent. The API answers the claim's single open request. `expectedVersion` is the `version` last loaded, and ensures it is still the question the claimant saw.
- Several rounds are possible, one open at a time. The answer must not be blank and is limited to 2,000 characters.
- Success changes the request to Answered and the claim to InReview. It is not a decision.

### Implementation

- Requests are shown oldest first, labelled in text as waiting, answered or closed. Only the open one has a form.
- The form has a label, explanation, character count, validation and focus on error, and accepts one submission at a time.
- After success the page shows a message and the saved answer, then reloads the claim and history.
- An answer is never carried over to a different question.

| Problem | Behaviour |
|---|---|
| 400, 403 | Message shown; answer kept |
| 401 | Login page with a message |
| 404 | Claim not found |
| 409 | Claim reloaded; explains whether the request is open, answered or closed |
| No reply or 5xx | Treated as uncertain, because the API saves before it builds its reply. The claim is checked and the answer is never resent automatically |
| The check also fails | Sending stays locked, the answer is kept and "Check again" is offered |

### Tests and verification

- Angular production build: no errors.
- **81 frontend unit tests passed** (23 new). **129 backend tests passed**; the backend is unchanged. No package was added.
- **Browser check: 21 of 21 passed** in headless Chrome with the real API and a temporary database, with no uncaught errors. It is a temporary script and is not in the repository.
- It covered the full flow, a second round, phone width, three rapid submits saving one answer, a closed request (409), the connection cut before the server received an answer and after it had saved one (saved exactly once), the API stopped, no assessment or officer name on the page, and a real 403. The last point closes the browser-level 403 gap in Entries 12 and 14.
- The Phase 10B browser script was not rerun.

### Decisions

- **Accepted:** The developer accepted Phase 10C for finalisation and commit, subject to a clean review.
- **Challenged:** None.
- **Overridden:** None.

### Known limitations

- Unsent text is lost if the claimant leaves the page or the session ends.
- No automatic refresh; a new question appears after reloading.
- Text only, and an answer cannot be edited after sending.
- If a first attempt is still travelling when the check reports "not saved", both could be sent. The version check lets only one be saved.
- A 403 on page load shows both a banner and a page error.

---

## Entry 16: Claims officer screens (Phase 10D)

- **Date:** 2026-11-25
- **Task:** Officer dashboard, claim lists, claim details and claim actions
- **AI tool:** Claude Code (Claude Opus 5.5)

### Purpose and process

The developer asked for the officer experience using the actual backend contract, prioritising "functional, demonstrable workflows over visual polish" because remaining time was limited. The AI reported the endpoints, DTOs and expectedVersion rules before coding. No backend file or package was changed.

### Assessment requirements and developer decisions

- **Assessment (p.2):** claims staff pick up incoming claims, review and assess them, progress them to settlement or rejection, and see their team's workload and performance. Each of these activities now has a screen.
- **Developer decisions:** metric definitions (Entry 11), lifecycle rules (Entry 3), confirmation before settling and rejecting, and no chart library. Page structure and error handling are AI implementation choices within the request.

### API contract used

| Endpoint (under `/api/officer`) | Body besides `expectedVersion` | Response |
|---|---|---|
| `GET /dashboard` | – | `DashboardResponse` |
| `GET /dashboard/exposure` | – | `CurrencyExposure[]` |
| `GET /claims`, `GET /claims/{id}` | – | `OfficerClaimResponse` (list or one) |
| `GET /claims/{id}/history` | – | `OfficerHistoryResponse[]` |
| `POST /claims/{id}/pickup` | none | `OfficerClaimResponse` |
| `POST /claims/{id}/assessment` | `amountMinor` (no notes field) | `OfficerClaimResponse` |
| `POST /claims/{id}/information-requests` | `question` | `OfficerClaimResponse` |
| `POST /claims/{id}/settle` | `note` (optional) | `OfficerClaimResponse` |
| `POST /claims/{id}/reject` | `reason` | `OfficerClaimResponse` |

### Implementation

- **Dashboard:** total, unassigned, open, awaiting-information and decided claims, the officer's own open claims, and workload and average time to decision per officer.
- **Exposure:** one row per currency, with no total across currencies.
- **Lists:** Incoming Claims (submitted, with pickup) and My Assigned Claims (the officer's own, including decided claims).
- **Details:** claim information, assignment, assessed and settlement amounts, requests and answers, and full history.
- **Actions:** pickup, assessment and reassessment, information request, settlement, and rejection from InReview or AwaitingInformation. Only actions allowed for the claim's status and assignment are offered. Amounts are entered in the claim's currency.
- **Officer identification:** the login response has no user ID, so the app reads the JWT `sub` claim to recognise the officer's own claims. This is for display only; the API enforces access.
- **Versions:** every action sends the version last loaded and then refreshes the claim and history. On 409 the claim is reloaded and an open confirmation is cancelled, so settlement must be confirmed again at the current amount.
- **Confirmation:** settling and rejecting need confirmation. The settlement confirmation shows the amount.
- **Uncertain results (no reply or 5xx):** details-page actions reload the claim and are never repeated automatically. If reloading fails, actions are locked. Pickup from the list shows the error and refreshes the list.

### Tests and verification

- Angular production build: no errors.
- **118 frontend unit tests passed** (37 new). **129 backend tests passed**; the backend is unchanged.
- **Officer browser check: 33 of 33 passed** with the real API and a temporary database. It covered settlement, rejection from both statuses, repeated clicks saving one pickup and one assessment, a stale settlement refused and reconfirmed at the new amount, dashboard figures, phone widths, and the claimant seeing only the settlement.
- **Claimant browser checks rerun:** 24 of 24 (Phase 10B) and 21 of 21 (Phase 10C) passed, because two stylesheets were moved to a shared folder.
- No uncaught browser errors. The scripts are temporary and are not in the repository.
- **Initial failure:** 39 unit tests failed at first. Officer redirect tests now reach a dashboard that calls the API, so their "no unexpected requests" check failed and broke later tests. The AI updated two tests to expect those requests; no application code changed.

### Decisions

- **Accepted:** The developer accepted Phase 10D for finalisation and commit, subject to a clean review.
- **Challenged:** None.
- **Overridden:** None.

### Known limitations

- No automatic refresh, paging, sorting or filtering.
- One demo officer, so "assigned to another officer" was checked by unit and API tests, not in the browser.
- Officers can open any claim by address, but no screen lists other officers' claims.
- Unsent text is lost when leaving the page.
- The confirmation moves focus to its button but does not trap focus.
- Kafka is not implemented. The README has not been updated for the Angular application.

---

## Entry 17: README for the complete application

- **Date:** 2026-11-25
- **Task:** Update the README for the Angular frontend and prepare for submission
- **AI tool:** Claude Code (Claude Opus 5.5)

### Request and process

The developer asked for the README to let a reviewer understand, install, run and demonstrate the complete application, using actual configuration and verified commands, without changing application code, configuration or packages.

### README update

- Project overview with both roles, features available on screen, and current architecture and versions.
- Project structure covering backend and frontend.
- Prerequisites, two-window startup, development proxy explanation and demonstration steps.
- Frontend test and build commands, frontend design decisions, Kafka decision and current limitations.
- Existing lifecycle, API reference, database, assumptions and design decisions were kept.

**Corrected outdated statements:** "There is no user interface yet" and that Node.js is not needed. The supported Node.js versions come from the installed Angular CLI's published requirements. The resolved Angular user-interface limitation was removed.

### Verification

Tested from clean temporary clones, with no local configuration, which were then deleted:
- The API and frontend started with the documented commands, using the default database.
- Login worked for both demo roles through the proxy. All seven demonstration steps worked in headless Chrome, with no uncaught errors.
- The API script ran as written, and all 16 documented endpoints matched the live OpenAPI document.
- 129 backend tests, 118 frontend tests and the Angular production build passed. The migration tool commands worked.

The AI's demonstration script first failed because it declared a variable name twice. The AI fixed the script and reran that part from a second clean clone.

### Kafka decision

- **Background:** Entry 2 made Kafka optional, after a working and tested core, and only with renewed developer approval. The AI reported that the condition was met and recommended updating the README first.
- **Developer decision:** Kafka is not implemented. Synchronous REST was selected to prioritise a reliable, runnable MVP within the assessment time limit. The README says so and describes an outbox-based extension as a future possibility only.
- **Assessment (p.2):** lists REST/HTTP and Kafka under preferred technology and asks the developer to decide what uses which and to explain it. Leaving Kafka out is a known gap against that preference.

### Decisions

- **Accepted:** The README update, subject to a clean review.
- **Challenged:** None.
- **Overridden:** None.

### Remaining documentation limitations

- Commands were verified on one Windows computer with .NET 10.0.404 and Node.js 26.7.0. Other listed Node.js versions were not tested.
- NuGet packages were already cached, so a first download was not tested.
- The documented `C:\Temp` database override was verified earlier with a different folder (Entry 13).
- No screenshots are included.
