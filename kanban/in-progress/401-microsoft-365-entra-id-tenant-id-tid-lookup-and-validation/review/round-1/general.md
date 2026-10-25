# Round 1 — general
**Date:** 2026-10-25
**Scope reviewed:** same as framework (branch vs `a2577574f`)

## Summary

The change does what the task asks. It adds the issuer parser and the resolver in TimeWarp.Identity,
moves accepted tenants to `{tenantId, domain?}` with an SQL migration (pinned by a real-Postgres
test), adds a `SettingsWrite` lookup endpoint with an abuse policy and a cache, and replaces the
GUID textarea with a list editor driven by state actions. The goal restatement matches the task.

There is one bug. The library's tests run the resolver on a bare HttpClient. The web server's
Aspire service defaults add the standard resilience handler to every client, so in the host a
Microsoft 5xx or 429 is retried and then reported as a timeout. Polly exceptions can also escape
the typed results. The other findings are cleanup.

Gates re-run by the reviewer: `dev build` passed (0 warnings, 0 errors). TimeWarp.Identity tests
passed 314/314. The `resolve-entra-tenant-tests.cs` runfile passed 6/6. web-infrastructure-tests
passed 83/83 with 0 skipped, which includes the migration test against Postgres in Docker.
`dev check-version` passed (`2.0.0-beta.26` is one ahead of NuGet). The reviewer did not run the
web-server or web-spa integration suites, because they start the Aspire AppHost.

## Issues

### Issue 1 — Severity: bug
- File: source/libraries/timewarp-identity/settings/entra-tenant-resolver.cs:101
- Description: `Register` adds the named client `entra-tenant-discovery`. In the web server,
  `AddServiceDefaults` (`aspire-service-defaults/extensions.cs:42`) runs
  `ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler())`, which also applies to
  this client. That changes the outcomes in the host:
  - Microsoft 5xx, 408, and 429 are retried with backoff, and the 10-second `LookupTimeout` ends
    the lookup. Reproduced with a throwaway probe: `ConfigureHttpClientDefaults(AddStandardResilienceHandler)`,
    `EntraTenantResolver.Register`, and a primary handler that returns 503 gave
    `Outcome=Timeout Status= calls=3 elapsed=10.1s`. The contract expects
    `UnexpectedStatus` with status 503 after one call. The admin sees "timed out" and the
    "unexpected HTTP status" warning is never logged.
  - Polly's `TimeoutRejectedException` (the 10-second attempt timeout) and
    `BrokenCircuitException` are neither `HttpRequestException` nor `OperationCanceledException`.
    The catches at lines 162 and 167 miss them, so they surface as an untyped 500 with no
    resolver log.
  - The Design region (line 17) says LookupTimeout "is the only deadline". In the host that is
    not true: the resilience attempt timeout (10 s) and total timeout (30 s) also apply.
  The resolver tests construct the factory without these defaults, so they do not show this.
- Suggestion: make the resolver own its pipeline. Either remove the default resilience handlers
  from the named client in `Register` (for example `RemoveAllResilienceHandlers()` from
  Microsoft.Extensions.Http.Resilience), or catch every exception except caller cancellation from
  the send as `NetworkFailure`. Doing both is better. Add a resolver test that registers
  `ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler())` before `Register`.
  It should assert that 503 gives `UnexpectedStatus` 503 after one call. Then update the Design
  region.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/libraries/timewarp-identity/settings/entra-tenant-ids.cs:208
- Description: `ConvertLegacyGuidArrayJson` is a new public API in a published package. Only one
  unit test calls it (`site-settings-tenant-acceptance-tests.cs:136`). The migration does the
  conversion in its own SQL (`20261025144841_ConvertEntraAcceptedTenants.cs`). The Design region
  at line 16 says this method "is the conversion the migration test pins". That is wrong:
  `ef-entra-accepted-tenants-migration-tests.cs` runs the SQL and never calls the method. The
  result is two conversions that nothing ties together, and a converter for the old shape in the
  library's public surface. The task asked for no compatibility shim.
- Suggestion: delete the method and its unit test. Point the Design region at the migration and
  its EF test as the one record of the conversion.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.resolve-entra-tenant.cs:72
- Description: Customer tenants often have several verified domains (for example `contoso.com` and
  `contoso.onmicrosoft.com`). Suppose the admin looks up a second domain of a tenant that is
  already listed. `TryNormalize` refuses it, and the page shows "Tenant {guid} is listed with more
  than one domain." The list shows one row for that tenant, so the admin cannot tell what is
  wrong or that the tenant is already accepted.
- Suggestion: before normalizing in `HandleSuccess`, check whether the draft already has
  `response.TenantId`. If it does, say so in plain words, for example "contoso.onmicrosoft.com
  is tenant {guid}, already listed as contoso.com.", and leave the list as it is. The
  server-side refusal of two domains for one id can stay as it is.
- Status: open

### Issue 4 — Severity: nit
- File: source/libraries/timewarp-identity/settings/entra-tenant-resolver.cs:129
- Description: The resolver writes to the app-wide `IMemoryCache` singleton without an entry
  `Size`. A consuming app that sets `MemoryCacheOptions.SizeLimit` would throw
  `InvalidOperationException` on every first lookup. The template does not set `SizeLimit` today,
  but TimeWarp.Identity is a published library.
- Suggestion: set `Size = 1` through `MemoryCacheEntryOptions`. It has no effect when there is no
  limit.
- Status: open

### Issue 5 — Severity: nit
- File: kanban/in-progress/401-microsoft-365-entra-id-tenant-id-tid-lookup-and-validation/task.md:150
- Description: "How to validate" says a third anonymous GET inside the window is 429. That is
  true only with the test override (`PermitLimit = 2` in `abuse-rate-limiting-tests.cs`). The
  shipped default is 20 per 60 seconds (`appsettings.json`), so a smoke tester following the text
  will not see a 429.
- Suggestion: say "the 21st request inside 60 seconds (default `EntraTenantLookup` limit)", or
  name the override.
- Status: open
