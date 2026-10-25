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

## Checklist

- [ ] Resolver with typed results in `TimeWarp.Identity` (HttpClient via the factory)
- [ ] Accepted-tenant entries carry an optional domain: aggregate, normalize/JSON, migration,
      contracts, validator, seeder, policy, callers
- [ ] `ResolveEntraTenant` contract and endpoint, `SettingsWrite` only
- [ ] Rate limit and cache
- [ ] Authentication page: domain look-up adds `{id, domain}`; the list shows domains
- [ ] Design region and `auth.md`
- [ ] Version bump and pins
- [ ] Tests

## Acceptance criteria

- [ ] A valid domain returns the tenant GUID parsed from `issuer`
- [ ] An unknown domain returns a distinct "tenant not found" result
- [ ] A network failure, timeout, or unexpected issuer shape returns its own result and is logged
- [ ] The lookup refuses callers without `SettingsWrite` and is rate-limited
- [ ] A repeat lookup is served from cache
- [ ] On the Authentication page, looking up a domain adds its tenant to the list with the
      domain shown, and saving stores both
- [ ] Existing rows migrate: each stored GUID becomes an entry with no domain, and sign-in
      acceptance is unchanged
- [ ] `dev build` 0/0, `dev check-version` passes, and the ingress golden is regenerated if affected

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

- Created: 691292 (2026-10-21)