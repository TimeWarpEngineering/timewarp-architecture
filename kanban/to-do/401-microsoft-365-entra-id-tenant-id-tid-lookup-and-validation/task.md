# Microsoft 365 / Entra ID tenant ID (tid) lookup and validation

## Description

Tenant IDs are GUIDs and easy to mistype. No public API looks up a tenant by display name, so
admins should not be asked to paste a raw GUID. Instead ask for the customer's primary domain
(e.g. `contoso.com`) and resolve it:

`GET https://login.microsoftonline.com/{domain}/.well-known/openid-configuration`

The `issuer` field is `https://login.microsoftonline.com/{tenant-id}/v2.0`; its GUID is the tenant
ID to store. A successful lookup also proves the domain belongs to a real Entra tenant.

Alternatives:
- Authenticate via `/common` (or `/organizations`), then enforce a tenant allow-list after sign-in
  using the `tid` claim (builds on task 306, admin-configurable accepted tenants).
- If the app already holds a token for the tenant, Microsoft Graph `GET /organization` returns the
  tenant display name (and verified domains) for confirmation.

## Scope

1. **Helper:** resolve domain -> tenant ID; optionally validate that an existing GUID matches a
   given domain. Parse `issuer` strictly (expected host and `/v2.0` shape, GUID segment).
2. **Admin UI guidance:** ask for the domain, not the raw GUID; show the resolved tenant ID for
   confirmation; validate server-side (never trust a client-resolved value).
3. **Integration notes** for the multi-tenant auth templates: how the resolved ID feeds the accepted
   tenants list (task 306) and the `tid` claim check.
4. **Security:** Microsoft deliberately limits tenant enumeration. Rate-limit the lookup endpoint,
   cache results (positive and negative, bounded TTL), require an authenticated admin, and do not
   turn the feature into a public "does this tenant exist" oracle.
5. **Errors:** surface real failures (unknown domain, network error, unexpected issuer format,
   non-success status) as distinct, clear errors. No swallowing, no silent fallback.

## Checklist

- [ ] Domain -> tenant ID resolver (HttpClient, typed result/errors)
- [ ] GUID-vs-domain validation
- [ ] Admin UI: domain input, resolved ID shown, server-side validation
- [ ] Rate limiting + caching on the lookup
- [ ] Integration notes for multi-tenant templates / task 306 allow-list
- [ ] Tests

## Acceptance criteria

- [ ] Given a valid domain, the resolver returns the tenant GUID parsed from `issuer`
- [ ] Given an unknown domain, a distinct "tenant not found" error is returned (not an exception swallowed into null)
- [ ] Network failures and unexpected issuer formats produce distinct, logged errors
- [ ] Validating a GUID against a domain reports match / mismatch
- [ ] Admin UI accepts a domain, not a raw GUID, and the server re-resolves before saving
- [ ] Lookup is admin-only, rate-limited, and cached; behaviour documented
- [ ] Docs: integration notes for multi-tenant auth templates

## Tests

- Unit: issuer parsing (valid, missing, malformed, non-GUID, wrong host); resolver with a stubbed
  HttpMessageHandler for 200 / 400 / 404 / timeout; GUID-vs-domain match and mismatch; cache hit.
- Integration: admin endpoint rejects unauthenticated callers and enforces the rate limit.
- Optional live smoke (opt-in, not CI): resolve a known public domain.

## Links

- OpenID discovery: https://learn.microsoft.com/entra/identity-platform/v2-protocols-oidc
- Find tenant ID: https://learn.microsoft.com/entra/fundamentals/how-to-find-tenant
- Graph organization: https://learn.microsoft.com/graph/api/organization-get
- Related: task 306 (accepted tenants for Microsoft sign-in)

## Session

- Created: 691292 (2026-10-21)