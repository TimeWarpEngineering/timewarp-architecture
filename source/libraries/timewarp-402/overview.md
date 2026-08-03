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

## Hard policy (free never 402)

- **Free / discovery routes never call this library's gate.** Host path isolation only (tip-jar lesson).
- **Disabled or misconfigured** paid surface → host **503** + `PaymentErrorPayload` — **never** `PAYMENT-REQUIRED` / 402.
- **Configured unpaid** → host **402** + `PAYMENT-REQUIRED`.
- **Verify/settle failure** → host **402** + fresh challenge.
- **Settled** → host **200** + `PAYMENT-RESPONSE`.
- **No merchant private keys** in this package; only public `payTo` + facilitator URL/auth-header factory.

## Host mapping (metered demo — 104-011)

`MeteredCapabilityGate`: prepaid credit debit first; else `PaymentGate` pay-then-credit-then-debit.
Hosts (web-server metered demo) map outcomes to 200/402/503 and set `PAYMENT-*` headers.
Free/discovery routes never call the gate.

## Not in this package yet

- ASP.NET middleware / tip demo host (104-009)
- Identity settle → Funded tier (104-013)
