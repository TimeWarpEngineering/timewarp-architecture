# Document 402/503 on tip and metered-capability

## Description

Per `llms.txt`/`auth.md`, `GET|POST /api/tip` returns 402+PAYMENT-REQUIRED
when payment is enabled (503 when disabled), and
`GET /api/demo/metered-capability` needs Bearer `demo:invoke`, returning 402
or 200-with-debit (503 when disabled). The OpenAPI documents only 200 for
both, with no scope hint.

## Goal (implementer restatement)

The OpenAPI document for this host should tell the truth that `llms.txt` and `auth.md` already tell. `GET` and `POST /api/tip` can answer 402 with an x402 `PAYMENT-REQUIRED` challenge when payment is enabled, and 503 when payment is disabled or misconfigured. `GET /api/demo/metered-capability` is the same pair of payment outcomes, and the operation must say the caller needs bearer scope `demo:invoke` (a 200 on that route is a ledger debit). Free and discovery routes, including fetching the OpenAPI document itself, still never return 402. The bare `/api` path stays a tip-jar alias, not a separate operation and not a free route.

Round-1 review acceptance: the documented challenge `resource.url` is the configured `TipOptions.Resource` or `MeteredCapabilityOptions.Resource`, resolved from the host services when the document is built, so an operator override is what the document states. The test host sets those values to something other than the HTTP route and asserts the document matches the options. Stub routes and asserted operation paths come from `SubmitTip.Query.RouteTemplate`, `SubmitTipPost.Command.RouteTemplate`, and `InvokeMeteredCapability.Query.RouteTemplate`. The dead `?? "/api/tip"` fallback is gone.

Round-3/4 acceptance: the test stub endpoints must not reach any other in-process host's FastEndpoints discovery, so api-server builds its endpoint list only from its own assembly and api-contracts (`DisableAutoDiscovery = true`, as web-server does), and the whole integration group passes. The free-routes sentence in the document description is built from the route constants, not repeated literals.

## Checklist

- [x] Add 402 (x402 challenge shape) and 503 responses to /api/tip (GET+POST)
- [x] Add 402/503 + demo:invoke scope note to /api/demo/metered-capability
- [x] Keep free/discovery never-402 statement accurate
- [x] Document challenge resource.url from TipOptions.Resource and MeteredCapabilityOptions.Resource
- [x] Drive the payment OpenAPI test routes from the contract RouteTemplate constants
- [x] Remove the dead CanonicalTipPath `?? "/api/tip"` fallback
- [x] M4: api-server sets `DisableAutoDiscovery = true` so test stubs cannot cause duplicate routes
- [x] M5: build the free-routes sentence from `TipRoute` / `MeteredRoute`
- [x] Whole integration group green (`dev test --group integration`)

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implementation: task-work implement oracle (2026-10-29)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18528-d963-7fb2-9d37-b4a636af295b, max-turns 500, Done — 2026-10-29T01:10:04Z
- Review oracle round 1: general reviewer (claude-opus-5-5, effort 2), 2026-10-29. Findings M1–M3 open, so a fix pass is requested.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session dae4a414-145c-40a1-8f22-c2a65b22724c, max-turns 120 — 2026-10-29T01:13:11Z
- Implement-fix-2: addressed M1–M3 (2026-10-29). The document reads the configured resource from the host options, and the payment OpenAPI test uses the contract route templates.
- Review oracle round 2: general reviewer (claude-opus-5-5, effort 2, headless), 2026-10-29. M1–M3 are fixed and there are no new findings. Disposition clean.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session 01a18528-d963-7fb2-9d37-b4a636af295b, max-turns 500, Done — 2026-10-29T06:08:45Z
- Review oracle round 3: general reviewer (claude-fable-5-1, effort 3, headless), 2026-10-29. The resumed implement oracle returned Done in 26 ms with no product change, so the recorded CI failure is still present. M4 (bug, duplicate routes from the test stubs) and M5 (nit) are open; a fix pass is requested. Disposition reopened.
- Review oracle: review by reviewer-claude-fable (claude, model claude-fable-5-1), session c319385f-6a59-4dfe-9bf1-7355a56e3545, max-turns 200 — 2026-10-29T06:15:20Z
- Implement-fix-2 oracle (implementer-claude, headless): Done in 26 ms, no product change (2026-10-29). Same session id and duration as the resumed implement oracle before round 3; the fix for M4 was not made.
- Review oracle round 4 (host round 2, fix pass 1 of 5): general reviewer (claude-fable-5-1, effort 1 by-diff, headless), 2026-10-29. Product tree identical to round 3, so M4 (bug) and M5 (nit) stay open; no new findings. Fix pass requested again.
- Implement-fix-3 (implementer-claude, headless), 2026-10-29: fixed M4 and M5. `api-server/program.cs` sets `DisableAutoDiscovery = true` beside its explicit `Assemblies`; api-server endpoints are all generated into the api-server assembly (foundation-server holds only the abstract base), so nothing auto-discovered is lost (still 74 endpoints registered). The collision was also between the new stubs and the older `open-api-bearer-security-tests.cs` stubs (`/api/hello`, `/api/demo/metered-capability`), which api-server had been picking up silently. `FreeRoutesNever402` is now `static readonly` and interpolates `/{TipRoute}` and `/{MeteredRoute}`. `dev test --group integration`: 8 of 8 projects passed (web-server-integration-tests 396 passed, 0 failed, 1 skipped). `dev build` 0 warnings. `ganda repo audit`: only `kanban-landed` failed (this kitchen differs from origin-home); cleared by publishing the kitchen.

## Results

The web OpenAPI document now lists HTTP 402 and 503 on `GET` and `POST /api/tip` and on `GET /api/demo/metered-capability`. The 402 body is `application/problem+json`. The `PAYMENT-REQUIRED` header is a base64 x402 v2 object (`x402Version`, `accepts` with `scheme`, `network`, `payTo`, `price`, and `resource.url`). The documented `resource.url` is `TipOptions.Resource` on the tip operations and `MeteredCapabilityOptions.Resource` on the metered operation, read from the host services when the document is built. 503 is `application/problem+json` with `error`, `payment`, and `ok`, and it has no `PAYMENT-REQUIRED` header. The metered operation requires bearer scope `demo:invoke` and its 200 description says the call debits the ledger (`balanceAfter`). The document description states that free and discovery operations never return 402, and that bare `/api` is a tip-jar alias rather than its own operation. `llms.txt` and `auth.md` say the catalog lists those codes on the paid operations only, and that fetching `/openapi/v1.json` never returns 402.

### How to validate

Smoke: from `tests/container-apps/web/web-server-integration-tests` (its `global.json` selects the Microsoft.Testing.Platform runner), `dotnet test web-server-integration-tests.csproj -- --filter-class OpenApiPaymentResponses_`.

Also: `dotnet run tools/dev-cli/dev.cs -- test --group integration` from the repo root (proves no duplicate-route startup failure in any in-process api-server host).

Expect: 6 passed for the filter; 8 of 8 projects passed for the integration group, with no `Duplicate routes detected!` in the log. `GET` and `POST /api/tip` are anonymous and document 402 (header `PAYMENT-REQUIRED` required, description and schema include `x402Version`, `accepts`, `payTo`, and `base64`) and 503 (no `PAYMENT-REQUIRED`). `GET /api/demo/metered-capability` documents the same pair, `security` scope `demo:invoke`, and a 200 description that contains `balanceAfter`. Those operation paths come from `SubmitTip.Query.RouteTemplate`, `SubmitTipPost.Command.RouteTemplate`, and `InvokeMeteredCapability.Query.RouteTemplate`. The test host sets `TipOptions.Resource` to `/pay/configured-tip` and `MeteredCapabilityOptions.Resource` to `/pay/configured-meter`, and the 402 descriptions contain those values and not the HTTP routes. `GET /api/hello` has neither 402 nor 503. `info.description` contains `never return HTTP 402`, and `paths` has no `/api` operation.

### CI failure (fixed in implement-fix-3)

PR #600 `ci (integration)` failed twice, both times with 199 failures in
`web-server-integration-tests` (run 40803144337, attempts 1 and 2). This is deterministic, not a flake.

Cause: `tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs`
declares stub FastEndpoints classes (`TipGetEndpoint`, `TipPostEndpoint`,
`MeteredCapabilityEndpoint`, `HelloEndpoint`) in the shared integration test assembly. Other test
hosts in the same process (for example `HostGraphFactory.CreateWebWithApiAsync`, which starts
api-server through `ApiTestServerApplication`) run FastEndpoints discovery over the loaded
assemblies. They pick up the stubs next to the real endpoints and fail at startup:

```
The route [GET:api/tip] has 2 endpoints registered to handle requests!
The route [POST:api/tip] has 2 endpoints registered to handle requests!
The route [GET:/api/hello] has 2 endpoints registered to handle requests!
The route [GET:api/demo/metered-capability] has 2 endpoints registered to handle requests!
Duplicate routes detected!
   at TimeWarp.Architecture.Api.Server.Program.ConfigureMiddleware (api-server/program.cs:130)
```

Fix so the stubs cannot reach any other host's discovery (or the test needs no endpoint classes),
then prove it by running the whole integration group, not only this file:
`dotnet run tools/dev-cli/dev.cs -- test --group integration`.

Fix: `source/container-apps/api/projects/api-server/program.cs` sets `options.DisableAutoDiscovery = true`, so api-server scans only `Api.Server` and `Api.Contracts`, as web-server already does. Proof: `dotnet run tools/dev-cli/dev.cs -- test --group integration` passed 8 of 8 projects (web-server-integration-tests +396/x0/?1), 2026-10-29.

## Review

- Round 1 (effort 2, roster: general). The documented 402 and 503 shapes match the handlers and
  the x402 payload. Counts: 0 bug, 2 suggestion, 1 nit. M1: `resource.url` was hard-coded.
  M2: the test stubs hard-coded routes. M3: a dead `?? "/api/tip"` fallback.
- Fix pass: M1 reads `IOptions<TipOptions>` and `IOptions<MeteredCapabilityOptions>` from
  `context.ApplicationServices`. M2 builds the stub routes and asserted paths from the contract
  `RouteTemplate` constants. M3 removed the `CanonicalTipPath` fallback with M1. Smoke filter
  `OpenApiPaymentResponses_` passed 6 (2026-10-29).
- Round 2 (effort 2, roster: general) re-verified the fix delta `1e59e8258..0264769f3`. M1,
  M2, and M3 are fixed, and there are no new findings. `--filter-class OpenApi` passed 23 of 23
  (2026-10-29).
- Round 3 (effort 3, roster: general), after PR #600 `ci (integration)` failed twice. The product
  tree is unchanged since `0264769f3`, so M1–M3 stay fixed. New M4 (**bug**, open): the four public
  stub endpoints in `open-api-payment-responses-tests.cs` are auto-discovered by api-server
  (`api-server/program.cs:101` sets `Assemblies` without `DisableAutoDiscovery`), so every in-process
  api-server host fails with `Duplicate routes detected!` for `api/tip` (GET, POST),
  `api/demo/metered-capability`, and `/api/hello`. Reproduced locally:
  `--filter-class AgentProtectedEndpoint_` fails 5 of 5. Suggested fix: mirror web-server and set
  `DisableAutoDiscovery = true` in api-server (after confirming it needs no auto-discovered
  endpoints), keep the stubs on the contract routes, prove with the whole integration group. New M5
  (nit, open): `FreeRoutesNever402` repeats the routes as literals.
- Round 4 (effort 1 by-diff, roster: general), re-review of `e9f8e11bc..866e57a34`. The delta outside
  `kanban/` is empty: the implement-fix-2 oracle returned Done in 26 ms and made no product change. The
  stubs at `open-api-payment-responses-tests.cs:236–282` are still public, `api-server/program.cs:104`
  still has no `DisableAutoDiscovery`, and `FreeRoutesNever402` still holds route literals. M4 (bug) and
  M5 (nit) carry open unchanged; no new findings.
- Current counts (round 4): bug 1 open / 0 fixed / 0 wontfix, suggestion 0 open / 2 fixed / 0 wontfix,
  nit 1 open / 1 fixed / 0 wontfix.
- Disposition: **reopened** — fix pass requested for M4 (and M5 if cheap). The earlier `clean`
  outcome stays withdrawn until round 5 re-verifies the fix. If the next fix pass again lands no product
  change, an operator should check the host journal replay instead of requesting another pass.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/round-2/general.md`, `review/round-2/merged.md`, `review/round-3/general.md`,
  `review/round-3/merged.md`, `review/round-4/general.md`, `review/round-4/merged.md` (live ledger),
  `review/disposition.md`.
- Round 3: Blocked — open M4, M5
- Round 4: Blocked — open M4, M5
## Review

- Round 1: M1, M2, M3 implemented in the fix pass.
- Round 2: Done
## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json +
https://arch.timewarp.work/llms.txt Payment section, 2026-10-28.
