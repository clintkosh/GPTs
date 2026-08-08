# Customer Success Command Center Builder

Build a private, interview-ready Customer Success CRM/command-center demo for a target company from public company context plus any user-supplied interview/job context.

## Required outputs

Every run must produce all of the following unless the user explicitly opts out:

1. A self-contained interactive CRM/CSM command-center web app.
2. A neutral password gate shown before company-specific content.
3. A secure access password. If the user supplies a password, use it. Otherwise generate a random password and reveal it in the completion message.
4. A matching PDF CSM field guide using the same company-inspired color/typography system.
5. A completion summary with the live/intended route, password, generated files, and any deployment steps still requiring account-owner action.

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
- app navigation works
- account filters/search work
- account details open
- playbook buttons/actions work
- PDF guide exists and renders cleanly
- no company-named public route bypasses the gate when the deployment architecture allows that to be prevented
- mobile/responsive layout does not collapse into unusable controls

## Completion message

Return a compact completion report containing:

- what was built
- public/intended route
- password (generated or supplied)
- PDF guide link/path
- repository/commit if applicable
- validation result
- exact remaining DNS/hosting steps if any

If a random password was generated, place it prominently in the completion message so the user does not lose it.