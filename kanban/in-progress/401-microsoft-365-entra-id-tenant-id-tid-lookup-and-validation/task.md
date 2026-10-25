# Microsoft 365 / Entra ID tenant ID (tid) lookup and validation

## Description

Tenant IDs are GUIDs and easy to mistype. No public API looks up a tenant by display name, so
admins should not be asked to paste a raw GUID. Instead ask for the customer's primary domain
(e.g. `contoso.com`) and resolve it:

`GET https://login.microsoftonline.com/{domain}/.well-known/openid-configuration`

The `issuer` field is `https://login.microsoftonline.com/{tenant-id}/v2.0`; its GUID is the tenant
ID to store. A successful lookup also proves the domain belongs to a real Entra tenant.

Task 306 shipped the accepted-tenant list. Today the admin Authentication page
(`web-spa/features/admin/site-settings/pages/AuthenticationPage.razor`) takes a page-local
textarea of raw GUIDs, one per line, parsed with `EntraTenantIds.TryParse` into
`UpdateSiteSettings.Command.EntraAcceptedTenantIds`. This task adds the domain lookup in front
of that list. The `tid` claim check after sign-in already exists (306) and is out of scope.

## Scope

Design for the best template. `TimeWarp.Identity` is ours: change it, break its API, and
release it when needed. Do not route around the library to avoid a release (Steve 2026-10-25).

1. **Resolver in `TimeWarp.Identity`:** beside `EntraTenantIds`
   (`source/libraries/timewarp-identity/settings/`), which already owns tenant-ID rules. Domain
   -> tenant ID, with strict `issuer` parsing (host `login.microsoftonline.com`, path
   `/{guid}/v2.0`, a non-empty GUID) and typed results. The web server only hosts the endpoint.
2. **Accepted tenants store the domain too:** replace `SiteSettings.EntraAcceptedTenantIds`
   (`IReadOnlyList<Guid>`) with entries of tenant ID plus an optional domain (for example an
   `EntraAcceptedTenant` record). Rename members to match. A pasted GUID has no domain.
   Duplicates are keyed by tenant ID. Update the aggregate, `EntraTenantIds` normalize and JSON,
   the jsonb column with an EF migration that converts existing rows, the site-settings contracts
   and validator, the seeder, the sign-in policy (it still matches on the `tid` claim), and every
   caller. No compatibility shim for the old shape.
3. **Lookup contract:** a new query (for example `ResolveEntraTenant`, domain in, tenant ID out)
   per `tw-web-api-contracts`, authorized with `PermissionIds.SettingsWrite`, the same permission
   as `UpdateSiteSettings`.
4. **Admin UI:** on the Authentication page, add a domain box and a look-up action (a
   TimeWarp.State action, per `all-user-interactions-are-state-actions`). A successful lookup adds
   `{tenant ID, domain}` to the list. The list shows each entry as `contoso.com (guid)`, or the
   bare GUID when there is no domain. A known GUID can still be added by hand. The server resolves
   the ID; the client never computes it. Replace the page-local GUID textarea if a list editor
   reads better. That is the implementer's call, consistent with `tw-blazor` and FluentUI v5.
5. **Abuse limits:** rate-limit the lookup with the existing abuse rate-limiting module
   (`platform/abuse/abuse-rate-limiting-module-server.cs`). Cache results, both found and not
   found, in memory for a bounded time. Admin-only, so it is not a public tenant-existence oracle.
6. **Errors:** unknown domain, network failure or timeout, non-success status, and an unexpected
   issuer shape are distinct results surfaced in the UI. Nothing becomes a silent null.
7. **Where the notes go:** `documentation/` is retired. Put the resolver and issuer rules in the
   resolver's Design region, and update the tenant acceptance section of
   `web-spa/wwwroot/auth.md`.
8. **Version:** this changes a published package. Bump `<Version>` and the platform package pins
   in the same commit (`dev check-version`, `tw-pr`).

## Goal (implementer restatement)

An administrator names a customer's primary domain. The server reads Microsoft's OpenID discovery
document for that domain and takes the tenant id from an issuer of
`https://login.microsoftonline.com/{guid}/v2.0`. The browser never parses the issuer.

`EntraTenantResolver` in `TimeWarp.Identity` returns a typed outcome: found, not found, invalid
domain, network failure, timeout, unexpected HTTP status, or unexpected issuer. Found and not-found
results stay in memory for a bounded time, and each cache entry sets a size so a host `SizeLimit`
does not throw. The other failures are logged. A cancelled caller token still throws. Every other
exception is a network failure, so a host resilience timeout or a broken circuit cannot become an
untyped 500. `Register` removes the host's standard resilience handlers from the named discovery
client. `LookupTimeout` is the only deadline, and a 503 is one call that reports unexpected status.

Accepted tenants are entries of tenant id plus an optional domain. A pasted GUID has no domain.
Duplicates collapse by tenant id. Two different domains for one id are refused on save. Sign-in
still matches the `tid` claim. There is no public converter for the old GUID-array JSON. Migration
`ConvertEntraAcceptedTenants` rewrites stored GUID arrays into null-domain entries, and
`ef-entra-accepted-tenants-migration-tests.cs` is the test that pins that rewrite.

`ResolveEntraTenant` requires `SettingsWrite` and is limited by `entra-tenant-lookup` (20 permits
per 60 seconds; the abuse test tightens that to 2). The Authentication page adds `{tenant id, domain}`
after a lookup. When that tenant id is already on the draft, the page says it is already listed
under its stored domain and leaves the list unchanged. A known GUID can still be added by hand.

Issuer rules live in the resolver Design region. `web-spa/wwwroot/auth.md` describes tenant
acceptance. The package version stays ahead of the last published release.

Done when a valid domain yields the issuer GUID, an unknown domain is not-found, network failure,
timeout, unexpected status, and unexpected issuer stay distinct and logged, a 503 under
`ConfigureHttpClientDefaults(AddStandardResilienceHandler)` is unexpected status after one call,
callers without SettingsWrite are refused, the lookup is rate-limited, a repeat found or not-found
lookup hits the cache, the page adds and saves `{id, domain}`, a second domain for a listed tenant
does not change the list, existing rows migrate without changing `tid` acceptance, and `dev build`
is 0/0 with `dev check-version` passing.

## Checklist

- [x] Resolver with typed results in `TimeWarp.Identity` (HttpClient via the factory)
- [x] Accepted-tenant entries carry an optional domain: aggregate, normalize/JSON, migration,
      contracts, validator, seeder, policy, callers
- [x] `ResolveEntraTenant` contract and endpoint, `SettingsWrite` only
- [x] Rate limit and cache
- [x] Authentication page: domain look-up adds `{id, domain}`; the list shows domains
- [x] Design region and `auth.md`
- [x] Version bump and pins
- [x] Tests
- [x] Discovery client drops host resilience handlers; a 503 is unexpected status after one call
- [x] Exceptions other than caller cancellation are network failure; cache entries set Size
- [x] Public GUID-array converter removed; migration test remains the pin
- [x] A second domain for a listed tenant leaves the list unchanged

## Acceptance criteria

- [x] A valid domain returns the tenant GUID parsed from `issuer`
- [x] An unknown domain returns a distinct "tenant not found" result
- [x] A network failure, timeout, or unexpected issuer shape returns its own result and is logged
- [x] The lookup refuses callers without `SettingsWrite` and is rate-limited
- [x] A repeat lookup is served from cache
- [x] On the Authentication page, looking up a domain adds its tenant to the list with the
      domain shown, and saving stores both
- [x] Existing rows migrate: each stored GUID becomes an entry with no domain, and sign-in
      acceptance is unchanged
- [x] `dev build` 0/0, `dev check-version` passes, and the ingress golden is regenerated if affected

## Tests

- Resolver: issuer parsing (valid, missing, malformed, non-GUID, wrong host), and a stubbed
  `HttpMessageHandler` returning 200, 400, 404, and a timeout. This mocks Microsoft's endpoint,
  which is an externality, not our code. Also a cache hit.
- Accepted-tenant entries: normalize (duplicate IDs collapse, empty GUID refused), JSON round
  trip, migration of old rows, and the policy still matching on `tid`.
- Web-server integration: no permission gives 403, and the rate limit trips.
- SPA integration: the look-up action adds `{id, domain}` to the page's list.
- Contract serialization round-trip.
- No live call to Microsoft in CI.

## Links

- OpenID discovery: https://learn.microsoft.com/entra/identity-platform/v2-protocols-oidc
- Find tenant ID: https://learn.microsoft.com/entra/fundamentals/how-to-find-tenant
- Graph organization: https://learn.microsoft.com/graph/api/organization-get
- Related: task 306 (accepted tenants for Microsoft sign-in, done)

## Session

- Spec updated 2026-10-25 (Steve): resolver in TimeWarp.Identity (no routing around the library to avoid a release); accepted tenants store {id, domain?}; SettingsWrite; existing abuse rate limiter; notes in Design region + auth.md.
- Implemented 2026-10-25: `EntraTenantResolver`, `{tenantId, domain?}` entries, `ResolveEntraTenant`, abuse policy `entra-tenant-lookup`, Authentication list, migration `ConvertEntraAcceptedTenants`, version `2.0.0-beta.26`. `ganda repo audit` passed. The kanban-landed advisory names eight other claims with no worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244); those are not this task.

- Created: 691292 (2026-10-21)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-25T15:11:12Z
- Review round 1 (2026-10-25): review oracle (Claude Opus 5.5), effort 3, roster general, session not reported. The goal restatement matches the task, so there is no misread. Open findings: M1 bug, M2 and M3 suggestions, M4 and M5 nits (`review/round-1/merged.md`). The reviewer re-ran `dev build`, the TimeWarp.Identity tests, the resolve-entra-tenant runfile, web-infrastructure-tests (including the migration test against Postgres), and `dev check-version`; all passed. A fix pass is needed. No disposition yet.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-25T15:21:59Z
- Fix pass (implement-fix-2, 2026-10-25): M1 drops `AddStandardResilienceHandler` from `entra-tenant-discovery` and maps every exception except caller cancellation to network failure. M2 deletes `ConvertLegacyGuidArrayJson`. M3 reports a tenant id already on the draft as already listed under its stored domain and leaves the list unchanged. M4 sets cache entry `Size` to 1. M5 names the default 20-per-60-second limit and the abuse-test `PermitLimit` of 2. Identity `Lookup_Given_` 15, `Issuer_Given_` 7, `SiteSettings_Given_` 25, SPA `ResolveEntraTenant_Action_Should_` 4, all passed. `dev check-version`: source `2.0.0-beta.26` is one prerelease ahead of NuGet `2.0.0-beta.25`.
- Review round 2 (2026-10-25): review oracle (Claude Opus 5.5), effort 2, roster general, session not reported. Scope was the fix delta `e1d6e20b9..81ad53928`. M1–M5 are fixed. New M6 (suggestion, open): a domain lookup no longer fills in the domain of a GUID-only or migrated row (`review/round-2/merged.md`). The reviewer re-ran `dev build` (0/0) and TimeWarp.Identity tests (316/316). A fix pass is needed. No disposition yet.

## Results

An admin enters a primary domain. `EntraTenantResolver` reads the OpenID discovery `issuer` and stores `{tenantId, domain?}`. A pasted GUID stays an entry with no domain. `ResolveEntraTenant` requires `SettingsWrite`, is limited by `entra-tenant-lookup` (20 per 60 seconds), and caches found and not-found results for 10 minutes. Unknown domains, network failure, timeout, unexpected status, and unexpected issuer are separate results. The named client `entra-tenant-discovery` removes host resilience handlers, so a Microsoft 503 is unexpected status after one call, and `LookupTimeout` is the only deadline. Any exception other than caller cancellation is a network failure. Cache entries set `Size` to 1. Migration `ConvertEntraAcceptedTenants` rewrites a stored GUID array into null-domain entries; there is no public converter for that shape. Sign-in still matches the `tid` claim. Looking up a second domain for a tenant already on the draft says it is already listed under its stored domain and does not change the list. Package version and the task-124 pins are `2.0.0-beta.26`. The ingress golden already lists `api/settings`, so it was left as it was. The Authentication page was not opened in a browser; the look-up action is covered by the in-proc SPA test.

### How to validate

Smoke: Sign in as an administrator, open `/Admin/Authentication`, enter `contoso.com`, choose Look up, then Save. Look up a second verified domain of that same tenant.
Expect: The list shows `contoso.com` followed by its tenant GUID, and a later read of site settings returns that same tenant id and `contoso.com`. The second domain shows that the tenant is already listed under `contoso.com` and does not add a row. A value that is not a domain shows a domain error and does not call the server. A member GET of `api/settings/entra-tenant?domain=contoso.com` is 403. The default `entra-tenant-lookup` limit is 20 permits per 60 seconds. The abuse test `EntraTenantLookup_Should_Return_Structured_429_After_Limit` overrides that to `PermitLimit` 2, and under that override a third anonymous GET of the route inside the window is 429 with policy `entra-tenant-lookup`. A row that still holds a GUID array migrates to two fields per id, domain null, and those ids still pass the sign-in `tid` check.

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5
- Round 2: M1–M5 fixed; Blocked — open M6

## Learnings

- Misread (round 2): the fix pass rewrote the goal restatement to say "when that tenant id is
  already on the draft, the page ... leaves the list unchanged". Task Scope 4 says a successful
  lookup adds `{tenant ID, domain}`. Round-1 M3 was only about a second, *different* domain. The
  restatement extended M3 to null-domain rows, so the lookup no longer fills in the domain of a
  migrated row (M6).
