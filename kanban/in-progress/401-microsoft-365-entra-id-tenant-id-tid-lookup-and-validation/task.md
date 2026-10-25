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

Admins should name a customer's primary domain and have the server resolve the Entra tenant id from
Microsoft's OpenID discovery document. `GET https://login.microsoftonline.com/{domain}/.well-known/openid-configuration`
returns an `issuer` of `https://login.microsoftonline.com/{guid}/v2.0`. That GUID is the tenant id.
The client never parses the issuer.

The resolver lives in `TimeWarp.Identity` next to `EntraTenantIds`, uses `IHttpClientFactory`, and
returns typed outcomes: found, tenant not found, invalid domain, network failure, timeout, unexpected
HTTP status, and unexpected issuer. Found and not-found results are cached in memory for a bounded
time. Network failure, timeout, unexpected status, and unexpected issuer are logged. Nothing collapses
to a silent null.

Accepted tenants become entries of tenant id plus an optional domain (`EntraAcceptedTenant`). A pasted
GUID has no domain. Duplicates collapse by tenant id. The aggregate, JSON, EF jsonb column, contracts,
validator, seeder, sign-in policy, and every caller take the new shape. There is no reader for the old
GUID array. An EF migration rewrites each stored GUID into an entry with a null domain. Sign-in still
accepts or rejects on the `tid` claim only.

The web server hosts `ResolveEntraTenant` (domain in, tenant id and domain out), authorized with
`PermissionIds.SettingsWrite`, and rate-limited by the existing abuse module. The Authentication page
replaces the raw-GUID textarea with a list: a domain lookup adds `{id, domain}` and shows
`contoso.com (guid)`; a known GUID can still be added by hand and shows as a bare GUID. Saving stores
both fields.

Issuer and resolver rules live in the resolver's Design region. The tenant-acceptance section of
`web-spa/wwwroot/auth.md` is updated. The published package version and platform pins move together
when `dev check-version` says the current version is not already ahead of NuGet.

Done when a valid domain yields the issuer GUID, an unknown domain is a distinct not-found result,
network/timeout/bad issuer are their own logged results, callers without SettingsWrite are refused and
the lookup is rate-limited, a repeat lookup hits the cache, the page adds and saves `{id, domain}`,
existing rows migrate without changing `tid` acceptance, and `dev build` is 0/0 with `dev check-version`
passing.

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

## Results

An admin enters a primary domain. `EntraTenantResolver` reads the OpenID discovery `issuer` and stores `{tenantId, domain?}`. A pasted GUID stays an entry with no domain. `ResolveEntraTenant` requires `SettingsWrite`, is limited by `entra-tenant-lookup` (20 per 60 seconds), and caches found and not-found results for 10 minutes. Unknown domains, network failure, timeout, unexpected status, and unexpected issuer are separate results. Migration `ConvertEntraAcceptedTenants` rewrites a stored GUID array into null-domain entries; sign-in still matches the `tid` claim. Package version and the task-124 pins are `2.0.0-beta.26`. The ingress golden already lists `api/settings`, so it was left as it was. The Authentication page was not opened in a browser; the look-up action is covered by the in-proc SPA test.

### How to validate

Smoke: Sign in as an administrator, open `/Admin/Authentication`, enter `contoso.com`, choose Look up, then Save.
Expect: The list shows `contoso.com` followed by its tenant GUID, and a later read of site settings returns that same tenant id and `contoso.com`. A value that is not a domain shows a domain error and does not call the server. A member GET of `api/settings/entra-tenant?domain=contoso.com` is 403. A third anonymous GET of that route inside the limit window is 429 with policy `entra-tenant-lookup`. A row that still holds a GUID array migrates to two fields per id, domain null, and those ids still pass the sign-in `tid` check.