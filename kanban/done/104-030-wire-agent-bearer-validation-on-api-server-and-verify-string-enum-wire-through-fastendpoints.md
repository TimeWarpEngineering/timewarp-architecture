# Wire agent bearer validation on api-server and verify string-enum wire through FastEndpoints

## Parent

104

## Description

104-004 landed agent key registration, scoped opaque bearer tokens, and the protected
`GET /api/identity/agent/me` surface on **web-server** only. Bearer validation was
**explicitly not** bound to api-server. When agents (or any client) need protected routes
on **api-server (FastEndpoints)**, wire the same agent-token authentication scheme and
scope policies there.

Also close the residual serialization gap from task **108**: string enums are applied via
`CommonServerModule` → `ConfigureHttpJsonOptions`. FastEndpoints is expected to honor that
by default when using `WriteAsJsonAsync` / HTTP JSON options, but **no test proves it**, and
**no contract enum currently crosses the api-server seam**. Before treating api-server as an
agent-facing host, verify the PascalCase string-enum wire shape holds through FastEndpoints
(add a minimal enum-bearing response or assert on a future identity/agent response if those
routes move or are dual-hosted).

## Requirements

- Agent-token authentication scheme + `identity:read` (and related) policies available on api-server
  in the same spirit as web-server (no parallel opaque-token stack)
- Protected FastEndpoints sample or real identity/agent route(s) exercise bearer validation
- **Verify string-enum wire shape holds through FastEndpoints** (task 108 follow-up): response
  JSON uses PascalCase member names (`"Agent"`, `"Keyed"`), not integers; integers fail closed
  where seam options apply
- Integration tests on api-server host prove both bearer auth and enum string emission
- Document host split: which identity endpoints live where (web-server vs api-server) if dual

## Checklist

- [x] Wire agent bearer validation on api-server (scheme + policies + DI)
- [x] At least one protected FastEndpoint proves bearer + scopes
- [x] **Verify string-enum wire shape holds through FastEndpoints** (raw JSON assert or contracts test on api-server path)
- [x] Integration tests green under `dev test` / targeted Fixie
- [x] Notes: relationship to 104-004 deferred scope and task 108

## Notes

- Deferred from **104-004** Results: "api-server bearer validation → later task if needed."
- Triggered by **108** post-review: CommonServerModule covers web-server (tested);
  FastEndpoints inheritance of `ConfigureHttpJsonOptions` is presumed but unproven until an
  enum crosses that seam under test.
- Identity agent HTTP surface today is web-server-only (`api/identity/agent/*`); do not move
  those routes unless product requires dual host — this task is about **api-server host
  capability** for agents, not necessarily relocating the existing me endpoint.
- Depends on: 104-004 (done), 108 (done).

## Results

### What shipped

1. **api-server agent-token scheme + policies** (`api-server/program.cs`):
   - `AddAuthentication().AddScheme<…, AgentTokenAuthenticationHandler>(agent-token)`
   - Policies: `agent-scope:identity:read`, `agent-scope:demo:invoke` (scheme-restricted + scope claim)
   - `AgentBearerStoresModule` → `IPrincipalStore` + `IAgentTokenStore` (in-memory, same ports as web)
   - `IAgentCallerContext` / `AgentCallerContext`
   - Middleware: `UseAuthentication` → `UseAuthorization` → `UseFastEndpoints` (was FE-before-auth)

2. **Platform cluster** `api/platform/identity-host/`:
   - Defaults (scheme/claim/policy strings — must stay identical to web)
   - Authentication handler (parity with web's handler; principal re-read after Validate)
   - Caller context + stores module (bearer validation only — no ceremony challenge stores)

3. **Protected sample** `GET api/agent/bearer/me` (`Features.AgentBearerSamples.GetAgentBearerIdentity`):
   - `[EndpointAuthorize(Policy = "agent-scope:identity:read")]`
   - Response carries `PrincipalKind` + `TrustTier` (first enum-bearing api-server FE response)

4. **Tests** (co-located Jaribu, 4/4 green standalone + api-jaribu aggregator 9/9):
   - OK + raw JSON `"kind":"Agent"` / `"trustTier":"Keyed"` (not integers)
   - 401 no header (WWW-Authenticate: Bearer)
   - 401 garbage token (`invalid_token`)
   - 403 demo:invoke-only (`insufficient_scope`)
   - Seeds principal + `IAgentTokenStore.Issue` on the api host (in-memory = process-local)

5. **Docs**: `documentation/developer/how-to-guides/how-to-agent-identity-host-split-web-vs-api.md`

### Design notes

- Ceremonies (register/token issue) remain **web-server-only** (104-004).
- In-memory stores are **per process**: a web-issued token does not validate on api until a shared store is wired. Documented; tests mint on api host.
- Handler/defaults are duplicated under api (cannot reference web assemblies). Constants and behavior track web's `AgentTokenDefaults` / `AgentTokenAuthenticationHandler`.
- Closes 108 residual for api-server FastEndpoints string enums.

### Verification

- `./bin/dev build` — 0/0
- `dotnet run …/get-agent-bearer-identity-tests.cs` — 4/4
- `dotnet test tests/container-apps/api/api-jaribu-tests` — 9/9


### How to validate

**Automated**
```bash
dotnet run source/container-apps/api/features/agent-bearer-sample/get-agent-bearer-identity/get-agent-bearer-identity-tests.cs
# or api-jaribu aggregator if present
# expect: 200 + kind/trustTier string enums; 401 no/garbage token; 403 insufficient scope
./bin/dev build
# expect: 0/0
```

**Manual (api-server host; token must be issued against the same process store)**
```bash
# In tests, Issue via IAgentTokenStore on the api host — web-issued tokens do not share in-memory store
curl -si -H "Authorization: Bearer <api-host-token>" https://localhost:7255/api/agent/bearer/me | head -30
# expect: 200 JSON with "kind":"Agent" (or similar PascalCase string), not integer enums
```

**Docs:** `documentation/developer/how-to-guides/how-to-agent-identity-host-split-web-vs-api.md`

**Not in scope:** moving web identity ceremony routes onto api-server.

## Session

- Created: 2026-07-20 (capture deferred 104-004 + 108 FastEndpoints enum verification)
- 2026-08-04: implemented api-server agent bearer + sample FE + string-enum tests + host-split doc; moved done
