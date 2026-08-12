# Rate-limit principal registration and payment challenge endpoints

## Parent

104

## Description

App-level rate limits so unpaid 402 floods and mass register cannot melt origin. Keep 402 responses cheap. Edge (Cloudflare) is extra later (023).

## Requirements

- Limits on register
- Limits on payment challenge
- Configurable defaults
- Structured 429 for agents where applicable

## Checklist

- [x] Middleware or policy
- [x] Tests or verified manual notes
- [x] Design region: edge vs app

## Notes

Cheap identity, expensive power — also cheap rejection.

### Depends on

104-003, 104-004, 104-008

## Session

- Created: 2026-07-16
- 2026-08-04: Implemented app-level ASP.NET RateLimiter (path-classified GlobalLimiter).
  Wired into web-server pipeline after UseRouting. Co-located Jaribu tests green (3/3).
  Disposition: done.

## Results

### What shipped

- **`platform/abuse/`** cluster (non-Features namespace `TimeWarp.Architecture.Abuse`):
  - `abuse-rate-limit-options-application.cs` — configurable sliding windows; Design region
    documents **edge vs app** (Cloudflare outer ring / 104-023 later; app protects origin;
    partition = RemoteIpAddress; PROXY/forwarded headers are a later ingress concern).
  - `abuse-rate-limit-options-validator-application.cs` — ValidateOnStart.
  - `abuse-rate-limiting-module-server.cs` — `AddRateLimiter` with path-classified
    `GlobalLimiter` + structured `application/problem+json` 429 (`SharedProblemDetails` shape,
    `policy` + optional `retryAfterSeconds` extensions, `Retry-After` header).
  - `abuse-rate-limiting-tests.cs` — real-host Jaribu proof (tight PostConfigure limits).

### Surfaces limited

| Policy | Paths | Default |
|--------|-------|---------|
| `principal-registration` | `api/identity/passkey/register[/options]`, `api/identity/agent/register[/options]` | 10 / 60s sliding |
| `payment-challenge` | `api/tip`, `api/demo/metered-capability` | 30 / 60s sliding |

Master switch: `AbuseRateLimitOptions:Enabled` (false → no-op limiters).

### Pipeline

`UseRouting` → **`UseRateLimiter`** → auth → FE. Rejection never reaches ceremony handlers or PaymentGate.

### Why GlobalLimiter (not FE endpoint metadata)

FastEndpoints endpoint `RequireRateLimiting` metadata was not applied reliably; path-based
`GlobalLimiter` runs in the ASP.NET middleware pipeline and matches final paths after tip-alias /
markdown rewrites.

### Proof

```text
dotnet run source/container-apps/web/platform/abuse/abuse-rate-limiting-tests.cs
# Principal Registration_…  PASSED
# Payment Challenge_…       PASSED
# Unrelated Route_…         PASSED
./bin/dev build → 0 warnings / 0 errors
```

### Out of scope

- Edge/Cloudflare rate limits (104-023)
- PROXY-protocol true client IP behind shared ingress (112 notes)
- Rate limits on authenticated credential-add paths

### How to validate

**Automated**
```bash
dotnet run source/container-apps/web/platform/abuse/abuse-rate-limiting-tests.cs
# expect: 3/3 — register and payment-challenge policies emit structured 429 under low limits
./bin/dev build
# expect: 0/0
```

**Manual** (optional; Development has limits enabled)
```bash
./bin/dev run
# Rapid-fire passkey register/options or GET /api/tip beyond default sliding window
# expect: HTTP 429 application/problem+json with policy extension
```

**Depends on:** `AbuseRateLimitOptions` in web-server appsettings (Enabled true to enforce).

**Not in scope:** Cloudflare edge (023).

