# Customer Success Command Center Builder

Build a private, interview-ready Customer Success CRM/command-center demo for a target company from public company context plus any user-supplied interview/job context.

## Required outputs

Every run must produce all of the following unless the user explicitly opts out:

1. A self-contained interactive CRM/CSM command-center web app.
2. A neutral password gate shown before company-specific content.
3. A secure access password. If the user supplies a password, use it. Otherwise generate a random password and reveal it in the completion message.
4. A matching PDF CSM field guide using the same company-inspired color/typography system.
5. Shared analytics instrumentation for gate views, successful unlocks, failed unlocks, guide opens, account opens, playbook actions, filter usage, theme changes, and meaningful CRM engagement.
6. A persistent light/dark mode toggle inside the unlocked CRM.
7. A completion summary with the live/intended route, password, analytics `demo_id`, generated files, and any deployment steps still requiring account-owner action.

## Privacy gate rules

Before unlock, do not visibly reveal the target company name, logo, product names, customer names, or other identifying copy. Use neutral text such as `Private Customer Success Demo`.

Prefer a neutral hostname such as `<codename>-crmdemo.example.com`. Do not place the target company name in the public URL unless the user explicitly asks for it.

Add `noindex,nofollow,noarchive,nosnippet` to the sign-in page.

If the hosting environment supports server-side authentication, Cloudflare Access, a Worker, or another edge/server gate, prefer that. If only a static host is available, implement a client-side presentation gate and clearly classify it as a lightweight demo gate rather than server-side security.

## Password generation

If no password is supplied, generate one with a cryptographically secure RNG. Default length: 20 characters. Include uppercase, lowercase, digits, and symbols while avoiding characters that commonly break HTML/JavaScript embedding (`'`, `"`, `\\`, backtick).

Reference implementation:

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

When the host already has a first-party analytics property, use that property instead of creating a new property for every demo. Give each CRM a unique neutral `demo_id`, normally derived from its neutral codename, for example `summertime_2026`.

Do not send the target company name, recipient name, recipient email, password, synthetic customer names, or other identifying/private values as analytics parameters. Keep analytics identifiers neutral.

Standard event schema:

- `crm_gate_view` — neutral password page loaded
- `crm_unlock_success` — correct password successfully unlocked the CRM
- `crm_unlock_failed` — incorrect password attempt
- `crm_guide_open` — CSM PDF guide opened
- `crm_account_open` — synthetic account drill-down opened
- `crm_playbook_run` — playbook/action launched
- `crm_filter_use` — portfolio/account filter changed
- `crm_theme_toggle` — user changes light/dark appearance; parameter `theme` is `light` or `dark`
- `crm_session_engaged` — first meaningful unlocked CRM interaction

Every event should include at least `demo_id` and `host`. Add only non-identifying operational parameters such as `playbook_type`, `guide_name`, `filter_type`, `theme`, or synthetic `account_segment` when useful.

For email attribution, use neutral UTM/campaign names or a neutral per-link reference token. Do not put the target company or named recipient into a visible URL unless the user explicitly asks for that.

Where Google Analytics 4 / gtag is used, preserve the site's existing measurement ID and fire custom events with `gtag('event', ...)`. Validate events in Realtime or DebugView when access permits. Recommend registering `demo_id` as an event-scoped custom dimension and marking `crm_unlock_success` as a Key event.

The completion report must state the exact `demo_id` and event names so the user can find them in analytics.

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

Use the actual public font family when it can be verified from the live site or published assets. If the exact font cannot be verified or licensed for redistribution, use the closest system/web-safe fallback and state that the build uses a visual-match fallback.

Do not redistribute proprietary font files. A generated PDF may use a metrically similar system font if the exact web font is unavailable in the generation environment.

The goal is to feel native to the target company's current design language without copying proprietary logos, source code, or protected artwork.

## Light and dark mode

Every generated CRM must include a compact light/dark toggle inside the unlocked application. The pre-login gate remains neutral and is not required to expose the toggle.

The theme system must:

- preserve the target company's visual identity in both modes rather than applying a generic inversion
- default to the mode that most closely matches the source company's current website
- persist the user's selection in `localStorage`
- restore the saved mode on the next visit
- update cards, tables, forms, modal surfaces, navigation, status colors, borders, text, and charts/readability together
- retain sufficient contrast in both modes
- expose accessible button labeling such as `Switch to light mode` / `Switch to dark mode`
- avoid flashing company-specific content before unlock
- fire `crm_theme_toggle` when analytics are enabled, with only the neutral `theme` value

If the company site is predominantly dark, use dark as the initial default; if predominantly light, use light. Do not force operating-system preference over the company-matched default unless the user explicitly requests system-mode behavior.

## CRM operating model

Map the CRM to the company's real product and customer journey rather than making a generic sales database. Include, as relevant:

- portfolio command center
- managed ARR / account segmentation
- account health
- onboarding and time-to-value
- product/module adoption
- telemetry or usage coverage
- outcome realization
- stakeholder/champion map
- executive sponsor status
- support burden and escalations
- risk signals
- renewal timing/confidence
- expansion signals
- QBR/EBR evidence
- playbooks
- next-best action
- signal logging

Use synthetic companies, numbers, ARR, usage, and outcomes unless real customer data was explicitly supplied and authorized for use.

## Health scoring

Health must be explainable. Prefer a weighted score combining adoption, outcome realization, stakeholder strength, support burden, technical coverage, and commercial timing. Display the reason for the score and recommended CSM action.

## Playbooks

Generate playbooks from the company's product/customer model. Common patterns include:

- executive sponsor reset
- adoption recovery
- technical coverage recovery
- critical support escalation
- renewal recovery
- value-evidence/QBR preparation
- expansion discovery
- program maturity review

## Generated tool quality standard

Any interactive helper added to a CRM — including signal recommenders, classifiers, calculators, health-score tools, playbook generators, prioritizers, search/filter logic, next-best-action engines, or similar utilities — must be implemented as a real input-sensitive tool rather than a decorative demo control.

For every such tool:

- map the realistic input dimensions before implementation; do not reduce the tool to one or two obvious keywords when the domain contains multiple meaningful variables
- support multiple distinct categories and materially different outputs
- handle compound inputs containing more than one signal and combine or prioritize recommendations where appropriate
- use surrounding context when available, such as the selected account, active playbook, renewal timing, health state, percentages, urgency language, stakeholder state, product/module, support severity, or onboarding stage
- extract useful structured facts from free text when practical, such as dates, day counts, percentages, severity indicators, stages, or named workflow types
- provide a structured result rather than a generic sentence when the decision is multidimensional; useful fields include classification, urgency, recommended action, owner, evidence to collect, timing, and next checkpoint
- avoid a single canned fallback that is returned for unrelated inputs; unknown inputs must receive a context-aware generic response that reflects the actual text supplied and asks for or identifies the missing decision variables
- never pretend that simple deterministic rules are an LLM or AI model; label the mechanism accurately
- keep synthetic/demo status explicit when the tool operates on synthetic data
- add privacy-conscious analytics for meaningful tool use when analytics are enabled, using neutral classifications rather than raw sensitive free text
- preserve keyboard accessibility, responsive behavior, and light/dark readability

### Required regression tests for generated tools

Before declaring a generated tool complete, test it with a deliberately varied matrix rather than one happy-path input. At minimum include:

1. several single-category inputs that should produce different outputs
2. at least two compound/multi-signal inputs
3. an urgency or time-sensitive case
4. a numeric case when the tool accepts numbers, percentages, dates, or thresholds
5. a vague/unknown case
6. an empty or malformed input case
7. a context-specific invocation, such as launching from a named playbook or account state, when context exists
8. repeated inputs to confirm deterministic tools are stable and state does not leak between runs

A tool fails validation if materially different realistic inputs collapse to the same recommendation without a defensible reason. Fix the logic before completion rather than documenting the limitation as acceptable.

## PDF CSM field guide

Automatically create a PDF guide for the CSM using the same visual theme as the CRM. The guide should explain:

1. How to use the command center.
2. Health-score logic.
3. Company-specific customer outcomes.
4. Core playbooks.
5. Day 0-90 onboarding/TTV milestones.
6. QBR/EBR storyline.
7. Signal-to-action rules.
8. A 60-second interview demonstration path.

The PDF must be visually QA'd after generation. Render it to page images and inspect every page for clipping, overlaps, broken tables, or missing glyphs before declaring completion.

## Validation before completion

At minimum verify:

- sign-in page contains no visible target-company identification
- wrong password fails
- correct password unlocks
- `crm_gate_view` fires on load
- `crm_unlock_failed` fires on an incorrect password
- `crm_unlock_success` fires on successful unlock
- guide/account/playbook/filter/theme events fire where implemented
- light/dark toggle visibly changes the CRM
- theme selection persists across reloads
- both themes preserve readable contrast and usable controls
- app navigation works
- account filters/search work
- account details open
- playbook buttons/actions work
- every generated helper/tool passes the varied-input regression standard above
- PDF guide exists and renders cleanly
- no company-named public route bypasses the gate when the deployment architecture allows that to be prevented
- mobile/responsive layout does not collapse into unusable controls

## Completion message

Return a compact completion report containing:

- what was built
- public/intended route
- password (generated or supplied)
- default theme and light/dark toggle status
- neutral analytics `demo_id`
- tracked event names
- generated tool regression result
- PDF guide link/path
- repository/commit if applicable
- validation result
- exact remaining DNS/hosting/analytics-admin steps if any

If a random password was generated, place it prominently in the completion message so the user does not lose it.