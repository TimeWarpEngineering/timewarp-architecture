# Agent end-to-end path register key then pay then call with quota token

## Parent

104

## Description

Scripted or automated path: agent registers key → hits metered API → 402 → pays → receives/uses token with quota → succeeds. No human in the loop.

## Requirements

- Documented sequence
- Automated test or script in repo
- No Entra, no human sponsor

## Checklist

- [x] E2E test or tools script
- [x] README/Results with curl-level steps

## Notes

This is the money-path demo for agents.

### Depends on

104-013, 104-011, 104-004

## Session

- Created: 2026-07-16
- Implement + close: 2026-08-04

## Results

### Summary

Continuous **no-human** agent money path:

1. **Jaribu host E2E (CI, mock facilitator)** — single continuous method
   `Money_Path_E2E_Register_Then_402_Then_Pay_Then_Prepaid_Quota` in
   `invoke-metered-capability-tests.cs`:
   - Register P-256 agent key + issue bearer (`demo:invoke` + `identity:read`)
   - Unpaid `GET api/demo/metered-capability` → **402** + `PAYMENT-REQUIRED`
   - Same bearer + `PAYMENT-SIGNATURE` → mock verify/settle → **200**
     `fundingSource=payment`, principal **Funded**, balance 0 after credit-then-debit
   - Seed residual ledger credit (server-side **quota**, not a JWT claim) → second call
     with **same** bearer, no payment header → **200** `fundingSource=credit`, debit
   - Debit never demotes Funded (104-013)

2. **CLI narration** — `tools/agent-identity-cli` command **`money-path`**:
   - keygen → register → token(demo:invoke,identity:read) → unpaid 402 (prints challenge)
   - optional `--payment-signature` for live settle retry → whoami (TrustTier)
   - Live facilitator settle is optional (human wallet); CI owns the mock path above

No Entra. No human sponsor. Opaque bearer is unchanged across pay; quota is ledger balance.

### Build / tests

- `./bin/dev build`: **0/0**
- `web-jaribu-tests` filter InvokeMeteredCapability: **6/6** (prior 5 + Money_Path_E2E)
- `web-jaribu-tests` filter Money_Path: **1/1**
- `agent-identity-cli-tests`: **11/11**
- CLI: `dotnet run tools/agent-identity-cli/agent.cs -- money-path --help` green

### Curl-level sequence

Web-server Development (`https://localhost:63611` or fixed-port test host `https://localhost:7000`).
`$SIGN` = ECDSA P-256 DER signature over the ceremony prefix + challenge (use
`agent-identity-cli` / SDK — not bare openssl one-liners). Signing details: 104-004 Results.

```bash
# 0. Prefer the CLI (executable docs)
dotnet run tools/agent-identity-cli/agent.cs -- money-path --server https://localhost:63611 --force
# Optional live settle after a real x402 PAYMENT-SIGNATURE is built for the challenge:
#   ... money-path --payment-signature "$PAYMENT_SIGNATURE"

# 1. Register agent key (no human)
curl -s -X POST https://localhost:63611/api/identity/agent/register/options
# => {"challenge":"<b64url>"}
# Sign UTF8("TimeWarp.Identity.AgentKey.Register.v1:") || challengeBytes
curl -s -X POST https://localhost:63611/api/identity/agent/register \
  -H "Content-Type: application/json" \
  -d "{\"publicKey\":\"$PUBLIC_KEY\",\"challenge\":\"$CHALLENGE\",\"signature\":\"$REG_SIG\"}"
# => {"principalId":"<guid>","keyId":"<b64url>"}

# 2. Token with demo:invoke (and identity:read for whoami)
curl -s -X POST https://localhost:63611/api/identity/agent/token/options
# Sign UTF8("TimeWarp.Identity.AgentKey.Token.v1:") || challengeBytes
curl -s -X POST https://localhost:63611/api/identity/agent/token \
  -H "Content-Type: application/json" \
  -d "{\"keyId\":\"$KEY_ID\",\"challenge\":\"$TOK_CHALLENGE\",\"signature\":\"$TOK_SIG\",\"scopes\":[\"demo:invoke\",\"identity:read\"]}"
# => {"accessToken":"...","scopes":["demo:invoke","identity:read"],...}

# 3. Unpaid metered call → 402 + PAYMENT-REQUIRED (base64 challenge JSON)
curl -si https://localhost:63611/api/demo/metered-capability \
  -H "Authorization: Bearer $ACCESS_TOKEN" | head -20
# HTTP/1.1 402 Payment Required
# PAYMENT-REQUIRED: <base64 requirements>

# 4. Paid call — live: real x402 PAYMENT-SIGNATURE for the challenge (facilitator verify+settle).
#    CI: mock facilitator accepts any well-formed base64 JSON header (see Jaribu E2E).
PAYMENT_SIGNATURE="<base64 signed payment payload>"
curl -si https://localhost:63611/api/demo/metered-capability \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "PAYMENT-SIGNATURE: $PAYMENT_SIGNATURE"
# HTTP/1.1 200 OK
# PAYMENT-RESPONSE: <base64 settle result>
# {"message":"Metered capability delivered.","balanceAfter":0,"fundingSource":"payment"}

# 5. whoami — same bearer; TrustTier is Funded after settle (not demoted by debit)
curl -s https://localhost:63611/api/identity/agent/me \
  -H "Authorization: Bearer $ACCESS_TOKEN"
# => {...,"trustTier":"Funded","scopes":["demo:invoke","identity:read"]}

# 6. Prepaid quota (server ledger). After settle, price was credited then debited → balance 0.
#    Further prepaid uses need residual credit (larger settle, tip credit, or ops seed).
#    With balance >= price, same bearer without PAYMENT-SIGNATURE → fundingSource=credit.
```

### Design notes

- **Quota** = `ICreditLedger` balance keyed by `PrincipalId`, not a claim on the opaque token.
- **Funded** = "has settled at least once"; debit to zero does **not** demote (104-013).
- Free routes never 402; disabled metered surface → 503 (product decision 8).
- Program exit sunny-path suite (104-022) also covers agent pay path; this task owns the
  continuous money-path pin + CLI + curl docs.

### Review

clean, effort 1 (no separate review round — thin composition of 011/013/029)

### Next

104-022 program exit sunny paths (partially landed); Wave 4 remaining 021.

### How to validate

**Automated (money path)**
```bash
dotnet run source/container-apps/web/features/metered-capability/invoke-metered-capability/invoke-metered-capability-tests.cs -- --filter-method Money_Path
# expect: register → unpaid 402 → mock pay → Funded → prepaid second call 200
# CLI smoke (narrated):
dotnet run tools/agent-identity-cli/agent.cs -- money-path --force
```

**Curl-level** (against a running web-server with mock facilitator in test host; live host needs TIP/metered config)
1. Agent keygen + register + token (`tools/agent-identity-cli` or 104-004 Results)
2. `GET /api/demo/metered-capability` with bearer → 402
3. Retry with `PAYMENT-SIGNATURE` (mock settle in tests) → 200
4. Seed credit / prepaid → second call 200 without payment header

**Not in scope:** Entra; live chain settle without `--payment-signature`.

