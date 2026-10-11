# Bring x402 pay-per-call into TimeWarp Architecture

## Description

Filed 2026-10-12 from Steven's voice call. **to-do, unclaimed, not launched.**

**Firm requirement (Steven):** port the x402 tip-jar code from
[timewarp-software](https://github.com/TimeWarpEngineering/timewarp-software)
into Architecture as **real app code** (server and/or API plus Blazor client
where a human surface is warranted), following `tw-web-api-contracts`,
`tw-feature-placement`, `tw-slice-isolation`, and State actions for every user
interaction — replacing the static HTML/TypeScript Cloudflare Worker approach
as the teaching/dogfood path for pay-per-call.

**First concrete deliverable:** tip-jar parity in Architecture (see Acceptance
criteria). Marketplace metering for task 118 builds on that foundation.

**Already landed (do not re-scaffold blindly):** program **104** shipped
`TimeWarp.402` / `TimeWarp.X402` (`source/libraries/timewarp-402/`), voluntary
tip on web-server (`web/features/tip/`, task **104-009** — USDC exact via
`PaymentGate`), metered capability demo (**104-011**), credit ledger
(**104-010**), settle→Funded (**104-013**), and discoverable tip/meter paths
(**104-020**). Software tip jar has since grown **Lightning** and **Cardano**
rails, settle email, and richer docs that Architecture has not fully mirrored.
This task closes that gap and decides host placement for marketplace pay-per-call
(api-server pipeline per 118), not a greenfield rewrite of 104.

**Supersedes** [timewarp-software backlog 019](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/kanban/backlog/019-extract-x402-testnet-example-into-timewarp-architecture-repo.md)
(extract x402 testnet tip example into Architecture). Work happens here;
software 019 should be archived or closed as superseded when this lands.

## Requirements

1. **Tip jar first.** Bring Architecture tip jar to intentional parity with
   software's voluntary tip jar teaching surface:
   - Unpaid enabled tip → **402** + `PAYMENT-REQUIRED` (x402 v2).
   - Disabled / misconfigured → **503** (never 402).
   - Free / discovery routes never enter the tip gate.
   - Settled → **200** + `PAYMENT-RESPONSE`.
   - Document local Sepolia walkthrough (facilitator mock in unit tests;
     optional live settle against `https://x402.org/facilitator`).
   - Decide per-rail scope vs software (USDC Base already present; Lightning /
     Cardano / settle-notify — port, stub, or defer with rationale in Results).
2. **App-level placement for pay-per-call.** Wire metered x402 as a real
   **api-server** cross-cutting concern (pipeline behavior / paid endpoints),
   matching task **118** host-role mapping (api = agent plane). Replace sample
   pipeline placeholders with real metering when marketplace nouns land —
   this task prepares that seam; 118 owns marketplace nouns.
3. **Follow Architecture skills:** `tw-web-api-contracts`,
   `tw-feature-placement`, `tw-slice-isolation`. State actions only where there
   is a real user interaction (human plane); agent pay path may stay
   header/buyer-script driven.
4. **No merchant private keys** in process; public `payTo` + facilitator only
   (existing TimeWarp.X402 policy).

## Open question — State.Plus module?

Steven asked whether x402 should become a self-contained module (state +
actions + contracts) that could move into **TimeWarp.State.Plus** as an
a-la-carte module. Cross-link:
[State backlog 110](https://github.com/TimeWarpEngineering/timewarp-state/blob/master/kanban/backlog/110-state-plus-a-la-carte-modules-pluggable-action-sets--predefined-state-for-any-site/task.md)
(and its backend contract-coupling question).

### State's input (2026-10-12) — recommendation: keep OUT of Plus for now

State recommends **building x402 app-level in Architecture, server-side**, as a
pipeline behavior on **api-server**, as task **118** already describes — **not**
as a TimeWarp.State.Plus module yet.

**Reasons:**

- The tip jar has **no client or wallet state**. The payer is
  `tools/tip-buyer/buy-tip.mjs`; protocol work lives in `worker/tip.js` (Hono,
  `@x402/hono`, facilitator verify/settle).
- State 110 lists **auth/identity under not-candidates**; payment stake sits in
  the same "host API / identity-adjacent" bucket for now.
- **Generic parts** can be a **server package** (extend `TimeWarp.402` /
  sibling): 402 challenge and retry, payment-header encoding, facilitator
  client, per-rail schemes (USDC on Base via CDP, Lightning via NWC, Cardano
  via cardano402.com).
- **App-specific parts** stay in Architecture: pricing, which endpoints are
  paid, receive addresses, crediting the **104-010** ledger, settle email.
- A Plus **client** module (`PaymentState` + host-supplied `IPaymentSigner` /
  `IPaidApiClient`) only makes sense **later**, if browser checkout is needed.

**Risks to track in design:** protocol churn (v2 headers; v1 `X-PAYMENT` still
accepted by some buyers), server never holds keys, exactly-once settle and
idempotent crediting, AOT/trim safety of any .NET x402 SDK (unverified), tests
that mock the facilitator plus end-to-end runs on Sepolia.

**Decision during design (does not block filing):** record accept/reject of a
Plus client module in Results; default posture is State's recommendation above.

## Related tasks

| Repo | Task | Role |
|------|------|------|
| Architecture | [118](https://github.com/TimeWarpEngineering/timewarp-architecture/blob/master/kanban/to-do/118-add-real-domain-showcase-flag-agentic-ai-marketplace-as-in-tree-dogfood-domain.md) | Bamboo microfactory marketplace needs x402 metering (quote → pay → job → ship); api = agent plane |
| Architecture | [113](https://github.com/TimeWarpEngineering/timewarp-architecture/tree/master/kanban/done/113-golden-persistence-implementation-postgres-first-aspire-wired-with-actor-model-evaluation) | Actor/fleet gate; marketplace layer precedes fleet |
| Architecture | [104](https://github.com/TimeWarpEngineering/timewarp-architecture/tree/master/kanban/done/104-agent-ready-identity-and-x402-program) (done) | Identity + TimeWarp.X402 + tip/meter foundations |
| Architecture | [104-009](https://github.com/TimeWarpEngineering/timewarp-architecture/blob/master/kanban/done/104-009-port-voluntary-tip-jar-example-from-timewarp-software.md) (done) | First tip port (USDC / PaymentGate) |
| State | [110](https://github.com/TimeWarpEngineering/timewarp-state/blob/master/kanban/backlog/110-state-plus-a-la-carte-modules-pluggable-action-sets--predefined-state-for-any-site/task.md) | Plus a-la-carte modules; contract-coupling open question |
| Software | [019](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/kanban/backlog/019-extract-x402-testnet-example-into-timewarp-architecture-repo.md) (backlog) | **Superseded by this task** |

## Reference material — timewarp-software tip jar

Production tip jar at https://timewarp.software/api/tip (free docs
https://timewarp.software/tip/). Protocol: **x402 v2**, scheme `exact`.

### Payment flow

1. Buyer `GET`/`POST` `/api/tip` (alias `/api` normalizes to `/api/tip`).
2. If tips disabled / misconfigured → **503** (`tips_disabled` /
   `tips_misconfigured`) — never 402.
3. If enabled and unpaid → **402** with `PAYMENT-REQUIRED` (base64 JSON
   challenge; up to three accepts).
4. Buyer pays on the chosen rail, retries with `PAYMENT-SIGNATURE` payload.
5. Worker verifies/settles via facilitator (or in-worker NWC for Lightning).
6. Success → **200** thank-you + `PAYMENT-RESPONSE`. Body amount/network match
   the **settled** rail.

### Rails (production)

| Rail | Network | Amount | Facilitator / verify | Receive |
|------|---------|--------|----------------------|---------|
| USDC | Base mainnet `eip155:8453`, USDC `0x833589fCD6eDb6E08f4c7c32D4f71b54bdA02913` | `$0.10` (`TIP_PRICE`) | Coinbase CDP `https://api.cdp.coinbase.com/platform/v2/x402` | `TIP_PAY_TO` (public address) |
| Lightning | `bip122:000000000019d6689c085ae165831e93` | `1000` sats | In-worker NWC `lookup_invoice` (Alby-shaped) | `TIP_NWC_URL` / LNURL `tip@timewarp.software` |
| Cardano | `cardano:mainnet`, asset lovelace | `1000000` lovelace (1 ADA) | [cardano402.com](https://cardano402.com/) + `@x402/cardano` | `TIP_CARDANO_PAY_TO` |

Local / testnet (`wrangler.dev.toml`): Base Sepolia `eip155:84532`, Sepolia
USDC, facilitator `https://x402.org/facilitator` (no auth).

### Source files

| Path | Role | GitHub |
|------|------|--------|
| `worker/tip.js` | Hono + `@x402/hono` tip handler; 402/503 policy; USDC (+ LN/ADA accepts) | [tip.js](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/worker/tip.js) |
| `worker/index.js` | Short-circuit `/api/tip` → dynamic import tip (free routes never load x402 graph) | [index.js](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/worker/index.js) |
| `worker/lightning.js` / `lightning-constants.js` / `nwc.js` / `lnurl.js` | Lightning x402 accept + LNURL-pay (never 402) | [worker/](https://github.com/TimeWarpEngineering/timewarp-software/tree/master/worker) |
| `worker/cardano.js` | Cardano exact rail | [cardano.js](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/worker/cardano.js) |
| `worker/tip-notify.js` | Operator email on settle | [tip-notify.js](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/worker/tip-notify.js) |
| `tools/tip-buyer/buy-tip.mjs` | Manual buyer harness (`@x402/fetch` / ExactEvmScheme) | [buy-tip.mjs](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/tools/tip-buyer/buy-tip.mjs) |
| `tools/tip-buyer/explorer.mjs` | Settle explorer helper | [explorer.mjs](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/tools/tip-buyer/explorer.mjs) |
| `tests/worker/tip.test.js` | Unit tests (mocked facilitator / NWC) | [tip.test.js](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/tests/worker/tip.test.js) |
| `documentation/x402-tip-spike.md` | Full spike/policy doc | [x402-tip-spike.md](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/documentation/x402-tip-spike.md) |
| `public/tip/index.html` + `index.md` / `source/content/tip.md` | Static free tip docs (HTML/MD) | [public/tip/](https://github.com/TimeWarpEngineering/timewarp-software/tree/master/public/tip) |
| `wrangler.toml` / `wrangler.dev.toml` | Prod mainnet vs local Sepolia tip env | [wrangler.toml](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/wrangler.toml) |
| `package.json` | `@x402/core`, `@x402/evm`, `@x402/hono`, `@x402/cardano`, `@coinbase/x402`, … | [package.json](https://github.com/TimeWarpEngineering/timewarp-software/blob/master/package.json) |

## Acceptance criteria

- Tip jar is the first shipped slice: Architecture tip path documents and
  tests the 402 / 503 / free-never-402 / settle policy against the software
  reference; gaps vs Lightning/Cardano/notify are explicit (ported or deferred
  with rationale).
- Pay-per-call / metering placement for the agent plane is decided and
  sketched (api-server pipeline), ready for 118 marketplace nouns — not a
  Plus module by default (State's recommendation recorded).
- Generators / hosts stay dual-mode aware (`UseX402Packages`); no merchant keys
  in repo.
- Unit tests mock facilitator; optional Sepolia E2E documented.
- Build warning-free; related tip/meter tests green.
- Software 019 marked superseded (or note left for a software board move).

## Checklist

- [ ] Inventory Architecture tip/meter vs software tip jar (USDC / LN / ADA /
      notify / docs) — write gap list in Results
- [ ] Tip-jar first deliverable: close intentional gaps; keep free-never-402
- [ ] Decide Lightning / Cardano / settle-email: port now, later, or never
- [ ] Sketch api-server metering pipeline seam for 118 (replace sample
      pipeline behavior)
- [ ] Record State.Plus decision (default: out; optional later PaymentState)
- [ ] Tests: mocked facilitator + Sepolia walkthrough doc
- [ ] Cross-link / supersede software 019
- [ ] One PR; merge via `ganda pr merge` when Steven asks

## Notes

- Filing only. Do **not** claim-for-work or launch until Steven asks.
- Quote→pay→job→ship happy path and bamboo microfactory nouns remain **118**.
- Static tip docs (`public/tip/`) are free content; do not 402 them. Optional
  Blazor tip page is human-plane UX, not required for agent buyer path.

## Session

- Created: 2026-10-12 (Grok Bot filing from Steven voice + State consult)

## Results

### How to validate
