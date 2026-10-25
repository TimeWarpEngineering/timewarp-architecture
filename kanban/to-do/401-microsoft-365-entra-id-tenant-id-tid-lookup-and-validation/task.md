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

1. **Resolver (web-server, not a library):** domain -> tenant ID. Parse `issuer` strictly
   (host `login.microsoftonline.com`, path `/{guid}/v2.0`, a non-empty GUID). Put it in the web
   server's identity or settings feature, not in `TimeWarp.Identity`. That library is a
   published package, and changing it would need a release.
2. **Lookup contract:** a new query (for example `ResolveEntraTenant`, domain in, tenant ID out)
   per `tw-web-api-contracts`, authorized with `PermissionIds.SettingsWrite`, the same permission
   as `UpdateSiteSettings`.
3. **Admin UI:** on the Authentication page, add a domain box and a look-up action (a
   TimeWarp.State action, per `all-user-interactions-are-state-actions`). A successful lookup
   shows the domain and the resolved tenant ID, and adds the ID to the accepted list. The list
   still stores GUIDs only, with no domain column. The textarea stays so a known GUID can still be
   pasted. The server resolved the ID, so the client never computes it.
4. **Abuse limits:** rate-limit the lookup with the existing abuse rate-limiting module
   (`platform/abuse/abuse-rate-limiting-module-server.cs`). Cache results, both found and not
   found, in memory for a bounded time. Admin-only, so it is not a public tenant-existence oracle.
5. **Errors:** unknown domain, network failure or timeout, non-success status, and an unexpected
   issuer shape are distinct results surfaced in the UI. Nothing becomes a silent null.
6. **Where the notes go:** `documentation/` is retired. Put the resolver and issuer rules in the
   resolver's Design region, and add a short "find a tenant by domain" paragraph to the tenant
   acceptance section of `web-spa/wwwroot/auth.md`.

## Checklist

- [ ] Resolver with typed results (HttpClient via the factory)
- [ ] `ResolveEntraTenant` contract and endpoint, `SettingsWrite` only
- [ ] Rate limit and cache
- [ ] Authentication page: domain box, look-up action, resolved ID shown and added to the list
- [ ] Design region and `auth.md` paragraph
- [ ] Tests

## Acceptance criteria

- [ ] A valid domain returns the tenant GUID parsed from `issuer`
- [ ] An unknown domain returns a distinct "tenant not found" result
- [ ] A network failure, timeout, or unexpected issuer shape returns its own result and is logged
- [ ] The lookup refuses callers without `SettingsWrite` and is rate-limited
- [ ] A repeat lookup is served from cache
- [ ] On the Authentication page, looking up a domain adds its tenant ID to the accepted list, and
      saving stores it

## Tests

- Resolver: issuer parsing (valid, missing, malformed, non-GUID, wrong host), and a stubbed
  `HttpMessageHandler` returning 200, 400, 404, and a timeout. This mocks Microsoft's endpoint,
  which is an externality, not our code. Also a cache hit.
- Web-server integration: no permission gives 403, and the rate limit trips.
- SPA integration: the look-up action adds the resolved ID to the page's list.
- Contract serialization round-trip.
- No live call to Microsoft in CI.

## Links

- OpenID discovery: https://learn.microsoft.com/entra/identity-platform/v2-protocols-oidc
- Find tenant ID: https://learn.microsoft.com/entra/fundamentals/how-to-find-tenant
- Graph organization: https://learn.microsoft.com/graph/api/organization-get
- Related: task 306 (accepted tenants for Microsoft sign-in, done)

## Session

- Spec updated 2026-10-25 to current code (after 306/410): resolver in web-server, SettingsWrite, existing abuse rate limiter, notes in Design region + auth.md (documentation/ retired).

- Created: 691292 (2026-10-21)