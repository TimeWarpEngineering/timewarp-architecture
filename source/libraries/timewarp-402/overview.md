# TimeWarp.402 — library layout

**PackageId:** `TimeWarp.402` (NuGet / product name).  
**C# namespace:** `TimeWarp.X402` — `TimeWarp.402` is not a legal C# identifier (leading digit after the dot).

Namespaces do not track folders (everything is `TimeWarp.X402`); folders exist for reader navigation.

| Folder | Contents |
|--------|----------|
| `options/` | `PaymentOptions`, config evaluator (ready / disabled / misconfigured) |
| `protocol/` | Headers, accepts, challenge builder, payTo validation |
| `facilitator/` | `IFacilitatorClient`, HTTP client, verify/settle models |
| `gate/` | `PaymentGate` + `MeteredCapabilityGate` outcomes — host maps to HTTP without ASP.NET |
| `ledger/` | `ICreditLedger` + in-memory impl (PrincipalId-keyed credits, idempotent receipts) |
| `funding/` | `SettlementFundingService` — Identity ↔ 402: settle → credit + TrustTier.Funded |

## Hard policy (free never 402)

- **Free / discovery routes never call this library's gate.** Host path isolation only (tip-jar lesson).
- **Disabled or misconfigured** paid surface → host **503** + `PaymentErrorPayload` — **never** `PAYMENT-REQUIRED` / 402.
- **Configured unpaid** → host **402** + `PAYMENT-REQUIRED`.
- **Verify/settle failure** → host **402** + fresh challenge.
- **Settled** → host **200** + `PAYMENT-RESPONSE`.
- **No merchant private keys** in this package; only public `payTo` + facilitator URL/auth-header factory.

## Host mapping (metered demo — 104-011 + 104-013)

`MeteredCapabilityGate`: prepaid credit debit first; else `PaymentGate` settle then
`SettlementFundingService.ApplyAsync` (credit + promote to Funded) then debit.
Hosts (web-server metered demo) map outcomes to 200/402/503 and set `PAYMENT-*` headers.
Free/discovery routes never call the gate.

**Debit never demotes TrustTier.** Funded means "has settled successfully (or was promoted)," not
"has positive balance." Zero balance after metered use leaves the principal Funded; gates that
need money check the ledger (or both ledger and `IsFundedAndActive`).

Register `SettlementFundingService` and `MeteredCapabilityGate` as **scoped** when
`IPrincipalStore` is scoped (EF/postgres); in-memory hosts may use singleton for both store and
services if they stay process-local.

## Tip jar (104-009)

Voluntary tip uses `PaymentGate` only — no principal required, no ledger credit, no tier change.
Funded promotion is for authenticated paid capability paths with a `PrincipalId`.
