# Customer Success Command Center Builder

Build a private, interview-ready Customer Success CRM / command-center demo for a target company from public company context plus any user-supplied interview/job context.

## Non-negotiable product boundary: metrics, records, and workflows only

By default, generated CRMs are **measurement and record systems**, not recommendation engines.

Do not add or regenerate any of the following unless the user explicitly overrides this rule in the current request:

- signal recommenders or free-text signal classifiers
- keyword-based pattern guessing
- sentiment analysis
- next-best-action engines
- inferred playbook selection
- inferred renewal/growth/expansion suggestions
- AI-generated customer strategy
- automatic assignment of an action based on prose input
- controls that claim to detect meaning or intent from notes, meetings, emails, or other free text

Free-text notes and meeting records are storage/display/search inputs only. Do not analyze them to produce advice.

Allowed derived behavior is limited to transparent calculations from explicit structured fields, including:

- sums, averages, ratios, percentages, deltas, durations, dates, and counts
- disclosed weighted health scores
- disclosed threshold/status bands
- deterministic SLA or lifecycle comparisons
- completion percentages
- renewal-day calculations
- factual rollups of stored structured values
- meeting briefs that assemble stored metrics, goals, stakeholders, meetings, actions, and notes without inventing recommendations

If a status such as `Healthy`, `Watch`, or `At risk` is displayed, publish the exact formula/threshold that produces it. A calculated flag may state the value and breached threshold, but it must not invent the action to take.

Static playbooks are allowed as reference checklists. They must not be automatically selected, ranked, or recommended from customer text or inferred patterns.

## Required outputs

Every run must produce all of the following unless the user explicitly opts out:

1. A self-contained interactive CRM/CSM command-center web app.
2. A neutral password gate shown before company-specific content.
3. A secure access password. If the user supplies a password, use it. Otherwise generate a secure random password and reveal it in the completion message.
4. A matching PDF CSM field guide using the same company-inspired color/typography system.
5. Shared privacy-conscious analytics for access and meaningful CRM use.
6. A persistent light/dark mode toggle inside the unlocked CRM.
7. Rich account workspaces with expected CSM records.
8. A completion summary with route, password, analytics `demo_id`, generated files, formulas/thresholds, validation result, and remaining owner-only deployment steps.

## Required account workspace

Every generated account workspace should include the fields relevant to the target SaaS model and, when applicable:

- account name, industry, segment, ARR/value, lifecycle stage
- contract start and end dates
- renewal date or days remaining
- structured health inputs and calculated health score
- adoption / usage / telemetry / technical coverage metrics
- outcome-evidence metrics
- stakeholder/champion coverage
- support/escalation records
- customer success-plan objectives, owners, proof definitions, and progress
- product/module entitlements and adoption state
- integrations and technical-owner context
- CSM, Technical Success, Sales/AE, Services, Support, or other internal ownership as relevant
- meeting history
- account notes
- customer employee / stakeholder directory

### Required record-entry controls

Account workspaces must support these record actions by default:

1. **Add account note**
   - timestamp
   - factual note text
   - display in the account's note history
   - edit/delete where practical

2. **Add meeting log**
   - meeting date
   - meeting type/title
   - attendees
   - factual notes / decisions / measurements / commitments
   - display in chronological meeting history
   - edit/delete where practical

3. **Add employee / stakeholder**
   - name
   - role/title
   - relationship/status
   - responsibility / stakeholder lane
   - display in the account stakeholder map
   - edit/delete where practical

These records may persist in `localStorage` for an interview/demo prototype. Clearly state that browser-local persistence is a demo mechanism, not an enterprise system-of-record design. In a production architecture, use the authorized governed backend/system of record.

Do not analyze newly entered records to infer sentiment, risk, next action, expansion opportunity, or customer intent.

## Privacy gate rules

Before unlock, do not visibly reveal the target company name, logo, product names, customer names, or other identifying copy. Use neutral text such as `Private Customer Success Demo`.

Prefer a neutral hostname such as `<codename>-crmdemo.example.com`. Do not place the target company name in the public URL unless the user explicitly asks for it.

Add `noindex,nofollow,noarchive,nosnippet` to the sign-in page.

If the hosting environment supports server-side authentication, Cloudflare Access, a Worker, or another edge/server gate, prefer that. If only a static host is available, implement a client-side presentation gate and clearly classify it as a lightweight demo gate rather than server-side security.

## Password generation

If no password is supplied, generate one with a cryptographically secure RNG. Default length: 20 characters. Include uppercase, lowercase, digits, and symbols while avoiding characters that commonly break HTML/JavaScript embedding (`'`, `"`, `\\`, backtick).

```python
import secrets, string
alphabet = string.ascii_letters + string.digits + "!@#$%^&*_-+="
while True:
    password = ''.join(secrets.choice(alphabet) for _ in range(20))
    if (any(c.islower() for c in password)
        and any(c.isupper() for c in password)
        and any(c.isdigit() for c in password)
        and any(c in "!@#$%^&*_-+=" for c in password)):
        break
```

Never substitute a memorable default like `Password123!`. Reveal the generated password in the user-facing completion message. Do not expose it on the pre-login page.

## Analytics and engagement tracking

Analytics are mandatory unless the user explicitly opts out.

Reuse the host site's existing first-party analytics property when available. Assign every CRM a unique neutral `demo_id`.

Standard event schema should include, as implemented:

- `crm_gate_view`
- `crm_unlock_success`
- `crm_unlock_failed`
- `crm_guide_open`
- `crm_account_open` / `crm_account_workspace_open`
- `crm_filter_use`
- `crm_theme_toggle`
- `crm_session_engaged`
- `crm_account_note_add`
- `crm_meeting_log_add`
- `crm_stakeholder_add`
- `crm_meeting_brief_generate`

Do not send target-company names, recipient names/emails, passwords, free-text notes, meeting text, stakeholder names, or other identifying/private values as analytics parameters. Use only neutral operational parameters such as `demo_id`, `host`, synthetic segment, filter type, or theme.

## Company research and theming

Research the target company's current public website before designing. Extract or infer:

- primary and secondary brand colors
- background/light-dark balance
- button/accent treatment
- card radius, density, and borders
- typography family and fallback stack
- headline weight and letter spacing
- navigation style
- visual hierarchy

Use an exact public font only when it can be verified and safely reused. Do not redistribute proprietary font files. Otherwise use a close system/web-safe fallback and disclose it.

The goal is to feel native to the target company's design language without copying proprietary logos, source code, or protected artwork.

## Light and dark mode

Every generated CRM must include a compact light/dark toggle after unlock.

The theme system must:

- preserve the target company's visual identity in both modes
- default to the mode that best matches the current company website
- persist the user's selection in `localStorage`
- restore it on revisit
- update all major surfaces together
- preserve readable accessible contrast
- expose clear accessible labeling
- avoid flashing company-specific content before unlock
- fire `crm_theme_toggle` with only the neutral `theme` value when analytics are enabled

## CRM operating model

Map the CRM to the company's real post-sales/customer journey rather than a generic sales database. Include relevant structured measurements for:

- portfolio command center
- managed ARR / account segmentation
- account health
- onboarding and time-to-value
- product/module adoption
- telemetry or usage coverage
- outcome realization
- stakeholder/champion coverage
- executive sponsor status
- support burden and escalations
- renewal timing
- QBR/EBR evidence readiness
- success plans
- static playbook/checklist library

Use synthetic customer/account data unless real data was explicitly supplied and authorized.

## Health scoring

Health must be reproducible and explainable.

Prefer a weighted score combining explicit structured fields such as adoption, outcome realization, stakeholder strength, support confidence/burden, technical coverage, and commercial timing. Display:

- formula
- input values
- weights
- final calculated score
- status thresholds
- which thresholds were breached

Do **not** turn the score into an automatically generated CSM recommendation.

## Static playbook library

Generate playbooks from the company's product/customer model as static reference checklists. Common patterns include:

- executive sponsor reset checklist
- adoption review checklist
- technical coverage recovery checklist
- critical support escalation checklist
- renewal review checklist
- value-evidence/QBR preparation checklist
- program maturity review checklist
- privacy/governance checklist
- integration review checklist

No automatic selection or recommendation based on customer text is allowed by default.

## Meeting brief

A generated meeting brief may assemble:

- calculated structured metrics
- threshold flags
- account objective
- stakeholder list
- stored success-plan goals/progress
- stored meeting history
- stored account notes
- open manually recorded actions/commitments

It must not invent a recommended discussion, next-best action, strategy, expansion idea, or inferred customer intent.

## PDF CSM field guide

Automatically create a PDF guide using the same visual theme. The guide should explain:

1. How to use the command center.
2. Exact health-score formula and thresholds.
3. Company-specific structured customer outcome metrics.
4. Account workspace and record-entry workflow.
5. Day 0-90 onboarding/TTV measurement milestones.
6. QBR/EBR evidence and meeting-record flow.
7. Support / Technical Success / Services ownership model where relevant.
8. Renewal measurement model.
9. Static playbook/checklist library.
10. A 60-second interview demonstration path.

The PDF must explicitly state that the CRM does not use free-text pattern guessing or generated recommendations by default.

Render the PDF to page images and visually inspect every page for clipping, overlaps, broken tables, or missing glyphs before completion.

## Validation before completion

At minimum verify:

- sign-in page contains no visible target-company identification
- wrong password fails
- correct password unlocks
- access and engagement analytics fire where implemented
- light/dark mode works and persists
- app navigation works
- account filters/search work
- account workspace opens
- account note can be added and persists
- meeting log can be added and persists
- employee/stakeholder can be added and persists
- deletion/edit behavior works where implemented
- calculated health matches the published formula
- threshold flags match the published rules
- meeting brief contains only stored/calculated facts
- playbooks are static and do not auto-select
- no recommender/classifier/signal engine/next-best-action code remains
- free-text notes/meetings are not analyzed for inferred meaning
- PDF guide exists and renders cleanly
- mobile/responsive layout remains usable

Treat the build as failed if recommendation/classifier code is present without a current explicit user override.

## Completion message

Return a compact completion report containing:

- what was built
- public/intended route
- password
- default theme and light/dark status
- neutral analytics `demo_id`
- tracked event names
- published health formula and thresholds
- account write controls implemented
- PDF guide link/path
- repository/commit if applicable
- validation result
- exact remaining DNS/hosting/analytics-admin steps if any
