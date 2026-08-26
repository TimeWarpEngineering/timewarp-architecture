# Agent-ready Identity and x402 program

## Description

Build first-class principals for **humans and agents**, internet-native **x402
payment** as both product feature and abuse control, and an **agent-ready**
template surface. Work happens in kanban → source (with Purpose/Design regions)
→ tests. Skills and human ADRs are **out of scope until the software works**.

Supersedes archived epic tree 097–103 (ADR-first framing was wrong).

## Build order (do not reorder casually)

```
Wave 1  Identity package          104-001 … 104-006 + 026–029, 031, 032  ✅ done
Wave 2  TimeWarp.402 package      104-007 … 104-012 (+016 pulled forward, +030)
Wave 3  Compose Identity + 402    104-013, 104-015, 104-014
Wave 4  Template + agent surface  104-017 … 104-022
Wave 5  Optional polish           104-023 (Cloudflare docs)
```

Order within a wave: follow the checklist below (it encodes the 2026-08-03
prioritization review), not raw child numbers. Settle→tier (013) needs both
packages. Open decision: if the metered endpoint (011) lives on api-server,
104-030 (agent bearer on api-server) must land before 011/014.

## Locked product decisions (from design sessions)

1. **Passkey / key first, profile later.** Account = accepted public key, not a
   registration form. Progressive profile is optional and later (task **205**,
   pulled off this epic).
2. **Humans and agents are both principals.** Kind: Human | Agent | Service.
3. **No human required if the agent pays.** Wallet/x402 is enough to buy service.
4. **Payment is in the template story**, not a bolt-on. Package name:
   **TimeWarp.402**. Identity package name: **TimeWarp.Identity**.
5. **Sessions for browsers; short-lived scoped tokens for agents.**
6. **Hybrid identity:** server-issued PrincipalId (Guid) for FKs + attached
   public keys. SSI/DID later if useful — not a v1 gate.
7. **Trust tiers:** cheap identity, expensive power.
   - Keyed = has credential, tiny/no expensive quota
   - Funded = paid / has credit
   - Established / Quarantined as behavior accumulates
8. **Free/discovery routes never return HTTP 402.** Disabled/misconfigured
   payment → **503**, never 402. (Hard lesson from timewarp.software tip jar.)
9. **Any human authenticator** (Proton Pass, platform, hardware). First-party
   WebAuthn; Passwordless.dev is not the long-term center (legacy in repo).
10. **Entra/MSAL is not the priority path.** Keep non-default or dormant (021).
11. **Agent-welcome edge posture** — do not default-block all AI bots. Cloudflare
    is outer ring (DDoS/WAF/rate limits); Identity+402 are app law (023).
12. **Score well on https://isitagentready.com/** via real surfaces (017–020),
    not docs-only.

## Mental model

| Layer | Job |
|-------|-----|
| Cloudflare (optional later) | Volumetric abuse, crude rate limits |
| TimeWarp.Identity | Who is this principal? (passkey / agent key / session-token) |
| TimeWarp.402 | Did they pay? Credits, tip, metered capability |
| App / template | What can they do? Demos, slices, agent discovery |

**Sybil defense:** infinite free principals OK if useless; power costs payment
or earned trust. Rate-limit register + 402 challenge endpoints (015).

## Existing code / material to reuse

- SPA `PasswordlessService` + web-server Passwordless SDK + `GetSignInToken` —
  reference only; replace center of gravity with first-party WebAuthn in Identity.
- timewarp-software: `worker/tip.js`, `documentation/x402-tip-spike.md`,
  tip-buyer, tests — port pattern into 009 (free routes never 402; CDP/mainnet
  vs Sepolia testnet separation).
- MSAL/Entra wiring in web-server — deprioritize (021).

## Non-goals (v1)

- Full SSI/DID/VC stack
- Entra External ID as primary identity
- Requiring a human sponsor for paid agents
- Skills or ADRs before working software
- Blocking training crawlers by default (product may choose later)

## Checklist

### Wave 1 — Identity (complete)
- [x] 104-001 Scaffold TimeWarp.Identity
- [x] 104-002 Principal / Credential / TrustTier
- [x] 104-003 Passkey register + authenticate
- [x] 104-004 Agent keys + scoped tokens
- [x] 104-005 Multi-credential
- [x] 104-006 Identity tests
- [x] 104-026 Apply 104-002 RFC ballot resolutions (archived — folded into 002)
- [x] 104-027 TypedId source generator + identity id migration
- [x] 104-028 Optimistic concurrency token on identity entities + store port
- [x] 104-029 Agent identity demo CLI (keygen/register/token ceremony)
- [x] 104-031 WebAuthn RP ID from request host against allowlist
- [x] 104-032 EF Core identity persistence behind postgres flag

### Wave 2 — 402
- [x] 104-007 Scaffold TimeWarp.402
- [x] 104-008 Challenge / verify / settle / 503 policy
- [x] 104-016 Passkey human demo (pulled forward from Wave 4 — deps 003/006 done;
      removes shipped Passwordless CDN script + tenant key from template, 131 F-010)
- [x] 104-009 Tip-jar port — web-server GET|POST api/tip, PaymentGate, TIP_* env,
      7/7 host tests + library PaymentGate coverage
- [x] 104-010 Credit ledger
- [x] 104-030 Agent bearer validation on api-server + string-enum wire verification
      (api-server capability sample GET api/agent/bearer/me; ceremonies stay on web)
- [x] 104-011 Metered demo
- [x] 104-012 Payment tests (Wave 2 exit gate) — library 42/42 mocked facilitator

### Wave 3 — Compose
- [x] 104-013 Settle → Funded + credits
- [x] 104-015 Rate limits (before advertising discovery paths publicly)
- [x] 104-014 Agent E2E path

### Wave 4 — Template + agents
- [x] 104-017 Discovery files (may parallelize earlier — story is stable)
- [x] 104-018 Markdown negotiation — home twin `/index.md` + Accept rewrite on `/`; SPA untouched
- [x] 104-019 MCP / skills / A2A stubs
- [x] 104-020 x402 discoverable
- [x] 104-021 Flags / slices / Entra non-default + auth-slice consolidation addendum
- [x] 104-022 E2E sunny paths (program exit criterion) — Program104Sunny suite 3/3

### Wave 5 — Optional (post-exit)
- [x] 104-023 Cloudflare operator notes

Progressive profile and agent–human / humanUx handoff were **not** 104 kernel.
Moved to independent to-do **205** (need more domain before placement).
Former Wave 5 children **104-024** / **104-025** are superseded stubs in `kanban/done/` (product on **205**). **104-026** is done process residue (fold-in on **104-002**).

- [x] Epic Results + ### How to validate (program already shipped; cite 022 Program104Sunny)
- [x] `ganda kanban done 104`; kanban-only PR; STOP (do not merge)

## Notes

### When code exists
Every new source file gets `#region Purpose` (required) and `#region Design`
where decisions live. Reconcile Design when behavior changes. That is the
durable design store — not this kanban after ships.

### After it works
Extract skills for consumers; optional human ADRs last. Do not invent either now.

## Overnight run (2026-08-04) — operator authorizations

Human is offline; agent continues on **this session** (not a Rhai workflow of
full `tw-orchestrate-task` — that skill needs sequential judgment, design-issue
gates, and commits that a fan-out script does not replace).

| Policy | Choice |
|--------|--------|
| Runner | Continuous session; `tw-orchestrate-task` per child id |
| Ambiguity | Decide from **Locked product decisions** above + existing code patterns; record rationale in child Notes; only park on true external product blocks |
| Git | **Local commits only** — no push, no PR until human wakes |
| Scope | Waves **2–4** critical path + safe parallels; **hold Wave 5** (023–025) |
| Parallels when deps allow | 016 (passkey demo), 017–019 (discovery), 030 (api-server bearer) — avoid same-file thrash with 402 path |
| Critical path | 007 → 008 → (009 ∥ 010) → 011 → 012 (Wave 2 exit) → 013 → 015 → 014 → 022 |

Wake-up: `git log --oneline origin/dev..HEAD`, `ganda kanban board` (or column
listings), child `## Results` / `review/disposition.md` trails.

## Session

- Created: 2026-07-16
- Context: passkey/agent/x402 brainstorm + reject ADR/skill-first sequencing
- Archived prior tree: 097–103
- 2026-08-03: prioritization review — Wave 1 closed (incl. follow-ons 026–032);
  checklist reconciled; 016 pulled into Wave 2 (deps done + template ships
  Passwordless tenant key until it lands); 030 slotted pending the
  metered-endpoint host decision (api-server vs web-server)
- 2026-08-04 overnight: continuous session; Waves 2–4; local commits; start 007
- 2026-08-04 overnight **complete (Waves 2–4)**: all Wave 2–4 children **done**
  including program exit **022**; Wave 5 (023–025) held per authorization.
  `./bin/dev build` **0/0**. **~35 commits local only** on `dev` (no push/PR).
  Wake-up: `git log --oneline origin/dev..HEAD`, child Results under `kanban/done/104-*`
- 2026-08-26: **104-023** done (Cloudflare operator notes). **104-024** / **104-025**
  pulled off this epic into to-do **205** (higher-level; wait for more domain).
- 2026-08-26: Cockpit close — human asked to mark epic **done**. Remaining is
  board hygiene only.

### Board close (2026-08-26)

Program is shipped (Waves 1–4 + 023). **205** is independent to-do — do **not**
implement it. Do **not** change product code.

Write `## Results` including `### How to validate`:

- What shipped: Identity + TimeWarp.402 + compose + template/agent surface + 023 docs
- Deferred: **205** (profile + humanUx)
- Smoke: `ganda kanban path 104` under `kanban/done/`; no 104 in in-progress
- Automated: Program104Sunny from **104-022**:

```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class Program104Sunny
# expect: 3/3 passed
```

Then `ganda kanban done 104` (folder kitchen), commit, `tw-pr` / `gh pr create`
`--head` `--base master`. STOP. Do not merge. Do not close **205**.
- Implementer launch: host=headless profile=implementer-grok provider=profile-default worktree=/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/task-104-agent-ready-identity-and-x402-program (2026-08-26 UTC)
- Implementer: grok headless board close — Results + `ganda kanban done 104` + kanban-only PR (2026-08-26)

## Results

Program 104 shipped. Waves 1–4 plus Wave 5 **104-023** are on origin/master via child PRs. Progressive profile and agent–human / humanUx handoff stay on independent to-do **205** — not closed here, not implemented here.

This close is **board hygiene only**. No Identity, TimeWarp.402, template, or other product files change in this PR. `ganda kanban done 104` moves the folder kitchen from `kanban/in-progress/` to `kanban/done/` so origin-home matches the shipped program.

### What shipped (already on origin/master)

| Wave | Scope | Children |
|------|--------|----------|
| 1 | TimeWarp.Identity — principals (Human/Agent/Service), passkeys, agent keys + scoped tokens, multi-credential, TypedId, concurrency, WebAuthn RP ID allowlist, EF persistence | 104-001 … 104-006, 026–029, 031, 032 |
| 2 | TimeWarp.402 — challenge/verify/settle, 503-not-402, tip jar, credit ledger, metered demo, api-server agent bearer, payment tests | 104-007 … 104-012, 016, 030 |
| 3 | Compose — settle → Funded + credits, rate limits, agent E2E | 104-013, 015, 014 |
| 4 | Template + agent surface — discovery, markdown negotiation, MCP/skills/A2A stubs, x402 discoverable, flags/Entra non-default, Program104Sunny | 104-017 … 104-022 |
| 5 | Cloudflare operator notes | 104-023 |

**Deferred (not this epic):** **205** — progressive profile + agent–human / humanUx handoff. Former children **104-024** / **104-025** are superseded stubs in `kanban/done/` (product stays on **205**). **104-026** is done process residue (RFC fold-in landed on **104-002**). They cannot stay archived: parent-done treats archived as open (ganda 187).

**Files changed (this PR):** 104 kitchen move + Results; 104-024/025/026 archived→done with Results; 205 pointer that those stubs are done. No product code.

**Decisions (locked, already in child Design regions):** passkey/key first; humans and agents are both principals; no human required if the agent pays; free/discovery routes never HTTP 402 (disabled payment is 503); sessions for browsers, short-lived scoped tokens for agents; hybrid PrincipalId + attached keys; trust tiers (Keyed / Funded / Established / Quarantined); Entra/MSAL non-default.

**Test outcomes:** program exit criterion **104-022** Program104Sunny **3/3** (human passkey onboard, agent register+pay+call, voluntary tip). Child payment library tests 42/42 mocked facilitator (**104-012**).

### How to validate

**Smoke** (kitchen move)

```bash
test ! -d kanban/in-progress/104-agent-ready-identity-and-x402-program && echo no-in-progress-104
# Expect: no-in-progress-104

test -f kanban/done/104-agent-ready-identity-and-x402-program/task.md && echo ok-104
# Expect: ok-104

ganda kanban path 104
# Expect: …/kanban/done/104-agent-ready-identity-and-x402-program/task.md

git diff origin/master...HEAD --stat
# Expect: only kanban/ paths (104 column move + Results)
```

**Expect** (kitchen)

- `ganda kanban path 104` is under `kanban/done/`. No 104 kitchen in `kanban/in-progress/`.
- Task 104 stays id **104** with Results and this How to validate.
- **205** remains in `kanban/to-do/` (`205-progressive-profile-and-agent-human-handoff-after-more-domain-exists.md`). Do not close it.
- **104-024**, **104-025**, **104-026** are under `kanban/done/` (not archived).
- This PR is kanban-only; no product code in the diff. STOP; do not merge from this worktree.

**Automated** (program exit — **104-022**, already on origin/master)

```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class Program104Sunny
# Expect: 3/3 passed
#   1) Human passkey onboard → principal + session
#   2) Agent register + pay (mock) + metered 200 + Funded
#   3) Voluntary tip mock settle
```

**Depends on:** in-proc web host; mock facilitator; no live chain; software authenticator (not Playwright).

**Not in scope:** live facilitator settle (funded wallet); browser WebAuthn hardware / Playwright virtual authenticator; task **205** profile/humanUx; Cloudflare live edge config (023 is operator notes only).
