# Reconcile IAuthApiRequest with EndpointAuthorize so generated endpoints fail closed

## Description

Surfaced reviewing task 109 (web-server MVC → generated FastEndpoints). The FastEndpoint generator
honors only `[EndpointAuthorize]`; it reads `IAuthApiRequest`/`[AuthApiRequest]` **zero times**. An
unannotated contract generates `AllowAnonymous()` — a **fail-open** default. The repo has two
disconnected auth-intent vocabularies that don't talk:

- `IAuthApiRequest` / `[AuthApiRequest]` — the older, skill-documented marker (client attaches the
  token; `AuthApiRequestValidator` runs) — generator-blind.
- `[EndpointAuthorize]` — the new server-side marker the generator emits from.

**Concrete consequence today:** seven contracts declare `IAuthApiRequest` — including admin role
**create/update/delete/get/get-all** — and now generate as public `AllowAnonymous()` endpoints
whose own contract says they require auth. A contract author following the documented
`IAuthApiRequest` pattern who doesn't also know to add `[EndpointAuthorize]` ships a public
endpoint, silently.

**Not a live regression** — the old MVC shims carried no `[Authorize]`, and web-server had no
`UseAuthentication` until 104-003, so these were server-anonymous before and after. But it cements
a contradiction into generated code and bakes "admin role CRUD is public" into a template others
copy. It also violates task 109's own acceptance criterion: *"the contract must become the single
source of auth intent, not a hand-maintained sidecar."* The 109 review marked disposition clean
without cross-checking `IAuthApiRequest` contracts against generated auth — this task closes that
gap.

## Requirements

- **Single source of auth intent.** Decide the reconciliation at plan (both viable; not mutually
  exclusive):
  - **Generator honors `IAuthApiRequest`**: a contract implementing `IAuthApiRequest` (or carrying
    `[AuthApiRequest]`) generates a non-anonymous endpoint. Requires deciding the default scheme/
    policy when only `IAuthApiRequest` is present (no explicit policy) — likely "require
    authenticated user, any registered scheme" unless `[EndpointAuthorize]` refines it. Where both
    are present, `[EndpointAuthorize]` wins (it is the more specific, server-facing statement).
  - **TWA analyzer guard (prefer-analyzers directive — strongly recommended regardless):** flag any
    contract whose generated endpoint would be `AllowAnonymous` while it declares `IAuthApiRequest`
    (or, stricter: flag ANY `[ApiEndpoint]` contract that declares neither explicit
    `[EndpointAuthorize]` nor an explicit anonymous opt-out — force every endpoint to state its auth
    posture). Turns the fail-open default into a build break.
- **Explicit anonymous opt-out.** Anonymous must be a *stated* choice, not a silent default — e.g.
  an `[EndpointAllowAnonymous]` marker (or `[EndpointAuthorize]`'s absence only permitted when an
  explicit anonymous marker is present). The identity ceremony endpoints (register/token options +
  complete, passkey ceremonies, get-current-session) are legitimately anonymous and must carry the
  explicit opt-out so the analyzer passes and intent is visible.
- **Fix the seven live contracts**: admin roles CRUD + `get-sign-in-token` + `get-current-user` —
  give them real auth (`[EndpointAuthorize]` with the appropriate policy/scheme) OR an explicit
  anonymous opt-out with a documented reason. Do NOT leave them generated-anonymous-by-omission.
  (Roles CRUD almost certainly wants a real admin policy — coordinate with whatever role/authz the
  template intends; if no admin policy exists yet, that is its own scoping decision to record.)
- **Deduplicate the vocabularies going forward**: document (skill + AGENTS.md) which marker is
  canonical for server auth so authors aren't choosing between two. If `IAuthApiRequest` remains
  the client-facing signal, state explicitly that it alone does NOT secure the server endpoint and
  the analyzer enforces the pairing.
- **Tests**: analyzer positive/negative (IAuthApiRequest-without-endpoint-auth flagged; explicit
  anonymous passes; EndpointAuthorize passes); integration test that an `IAuthApiRequest` endpoint
  actually rejects an unauthenticated request (the roles endpoints — proving the fix is real, not
  just annotation).

## Checklist

- [ ] Plan: generator-honors-IAuthApiRequest vs analyzer-guard vs both; default-scheme decision
- [ ] Explicit anonymous opt-out marker; identity ceremony endpoints annotated with it
- [ ] Generator and/or analyzer change; fail-open default eliminated
- [ ] Seven live IAuthApiRequest contracts given real auth or documented explicit-anonymous
- [ ] Vocabulary canonicalized in skill + AGENTS.md
- [ ] Analyzer tests + integration test proving an IAuthApiRequest endpoint rejects anon
- [ ] dev build 0/0; full dev test

## Notes

- Origin: 2026-07-20 review of task 109. Evidence: generator
  `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs`
  `BuildAuthConfiguration` (no-attribute → `AllowAnonymous()`); `IAuthApiRequest` contracts under
  `web-contracts/features/admin/roles/**`, `auth/`, `authentication/`.
- Relates to 104-030 (api-server bearer wiring) — same generator, same auth-emission path; consider
  sequencing so the reconciliation lands before more endpoints are generated on api-server.
- ADR-0007 (endpoints are generated FastEndpoints on both servers) should gain a line on how auth
  intent is expressed once this is decided.

## Session

- Created: 2026-07-20
