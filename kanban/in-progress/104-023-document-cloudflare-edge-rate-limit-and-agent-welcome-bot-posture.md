# Document Cloudflare edge rate-limit and agent-welcome bot posture

## Parent

104

## Description

Operator notes only: WAF/rate limits, AI crawl classes, agent-welcome not block-all-bots. Optional Worker front door pointer to tip hosting. Not a substitute for app Identity/402.

## Requirements

- Written doc in documentation/ or template ops notes
- Align with 015 app limits

## Checklist

- [ ] `ganda kanban move 104-023 in-progress`
- [ ] Operator how-to under `documentation/developer/how-to-guides/` (kebab filename)
- [ ] Link from `documentation/developer/how-to-guides/overview.md` (root README is a stub — do not "fix" that TODO page)
- [ ] Align numbers/paths with **104-015** app limiter
- [ ] Results including ### How to validate; `ganda kanban done 104-023`; PR; STOP

## Notes

Optional Wave 5. Edge ≠ identity. **104-015 is already done** (nice-to-have first).

### Depends on

104-015 nice-to-have first

## Session

- Created: 2026-07-16
- Cockpit: Grok launch (2026-08-26) — docs-only Wave 5; dispatch `ganda task work`

### Implementer brief (2026-08-26)

Stay in this claim worktree. **Docs only.** Do not add Cloudflare Workers, WAF Terraform, or app rate-limiter code. Do not close parent **104**. Do not start **024** / **025**.

**What this is:** operator notes so someone putting a generated app behind Cloudflare does not (a) block AI agents by default, or (b) treat the CDN as Identity/402.

**Write** `documentation/developer/how-to-guides/how-to-configure-cloudflare-edge-for-agent-welcome.md` (or equally kebab name). Cover:

1. **Two rings** — Cloudflare = volumetric outer ring (DDoS/WAF/crude IP limits). App Identity + TimeWarp.402 = who they are and whether they paid. Cite locked decision 11 on epic **104** and the EDGE VS APP Design region in `source/container-apps/web/platform/abuse/abuse-rate-limit-options-application.cs`.
2. **Agent-welcome** — do **not** recommend default “block all bots / AI crawlers”. Welcome verified/agent traffic; still rate-limit abuse. This template is meant to score on https://isitagentready.com/ via real surfaces (017–020 already shipped).
3. **Align with 015** (shipped, do not change):
   - `principal-registration` — `api/identity/passkey/register[/options]`, `api/identity/agent/register[/options]` — default 10 / 60s sliding
   - `payment-challenge` — `api/tip`, `api/demo/metered-capability` — default 30 / 60s sliding
   - structured `application/problem+json` 429; `AbuseRateLimitOptions:Enabled`
   Edge limits should be **at least as coarse** as these (edge catches multi-IP floods; app still protects origin).
4. **Optional Worker front door** — one short pointer to the timewarp.software tip Worker pattern (archived 102-002). Not a required sample rewrite. Isolation: free discovery routes stay cheap; never return **402** on free/discovery (503 if payment disabled).
5. **What never lives only at the edge** — principal ledger, credits, sessions/tokens.

**Link:** add a bullet on `documentation/developer/how-to-guides/overview.md` (new “Ops / edge” section is fine). Optionally one line in `AGENTS.md` only if that file already points at how-tos.

**Done bar:** Results + How to validate (open the doc, expect two-rings + 015 paths + “do not block all AI bots”). `ganda kanban done 104-023`, `tw-pr` / `gh pr create` `--head` `--base master`. STOP. Do not merge. Do not `ganda kanban done 104`.
