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
