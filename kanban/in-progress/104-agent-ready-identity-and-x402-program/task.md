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
Wave 5  Optional polish           104-023 … 104-025
```

Order within a wave: follow the checklist below (it encodes the 2026-08-03
prioritization review), not raw child numbers. Settle→tier (013) needs both
packages. Open decision: if the metered endpoint (011) lives on api-server,
104-030 (agent bearer on api-server) must land before 011/014.

## Locked product decisions (from design sessions)

1. **Passkey / key first, profile later.** Account = accepted public key, not a
   registration form. Progressive profile is optional and later (024).
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
- [ ] 104-014 Agent E2E path

### Wave 4 — Template + agents
- [x] 104-017 Discovery files (may parallelize earlier — story is stable)
- [x] 104-018 Markdown negotiation — home twin `/index.md` + Accept rewrite on `/`; SPA untouched
- [x] 104-019 MCP / skills / A2A stubs
- [x] 104-020 x402 discoverable
- [ ] 104-021 Flags / slices / Entra non-default + auth-slice consolidation addendum
- [x] 104-022 E2E sunny paths (program exit criterion) — Program104Sunny suite 3/3

### Wave 5 — Optional (post-exit)
- [ ] 104-023 Cloudflare operator notes
- [ ] 104-024 Progressive profile (hold until demanded)
- [ ] 104-025 humanUx link (hold until demanded)

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
- 2026-08-04 progress (local, unpushed): **007 ✅ 008 ✅ 010 ✅ 011 ✅ 017 ✅
  012 ✅ 009 ✅** (tip jar); **030 ✅**; **018 ✅**; **019 ✅** (MCP/skills/A2A
  cards); remaining Wave 2: 016; Wave 4: 020; then Wave 3
