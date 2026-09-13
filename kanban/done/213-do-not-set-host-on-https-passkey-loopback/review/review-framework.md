# Review framework — task 213

**Date:** 2026-09-13
**Host task:** kanban/in-progress/213-do-not-set-host-on-https-passkey-loopback/
**Diff scope:** branch `task/213-do-not-set-host-on-https-passkey-loopback` vs `origin/master` (product commit `e1e8dd5d`; kanban brief/results excluded from product review)
**Plan / brief:** Stop rewriting HTTP `Host` on HTTPS InteractiveServer/Auto `WebService` loopback (TLS SNI / cert name mismatch vs ASP.NET localhost dev cert). Carry circuit/page host on internal `X-TimeWarp-Circuit-Host` set only by `IdentitySessionCookieForwardingHandler` from `Request.Host.Host` (never `X-Forwarded-Host`). `HttpRequestHostAccessor.GetRequestHost()` prefers that header when present and non-empty, else `Request.Host.Host`. Keep 212 RP-ID goal. Do not disable SSL validation, switch loopback to HTTP, or enable `UseForwardedHeaders`. Rewrite 212 Host-copy tests that asserted `Headers.Host == arch.timewarp.work`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a098a1-9f6b-7d72-a42a-bb4aa75567ab (2026-09-13)

## Rounds

- **Round 1:** general — M1 bug (unconditional circuit-host header trust on public path) → fixed on this task (`cd1f5b56`)
- **Round 2:** general re-review of M1 + fix delta (loopback-only honor). Prior `round-1/` is frozen.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
