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
