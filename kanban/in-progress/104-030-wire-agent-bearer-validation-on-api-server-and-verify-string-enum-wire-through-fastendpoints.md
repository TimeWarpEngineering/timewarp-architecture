# Wire agent bearer validation on api-server and verify string-enum wire through FastEndpoints

## Parent

104

## Description

104-004 landed agent key registration, scoped opaque bearer tokens, and the protected
`GET /api/identity/agent/me` surface on **web-server (MVC)** only. Bearer validation was
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

- [ ] Wire agent bearer validation on api-server (scheme + policies + DI)
- [ ] At least one protected FastEndpoint proves bearer + scopes
- [ ] **Verify string-enum wire shape holds through FastEndpoints** (raw JSON assert or contracts test on api-server path)
- [ ] Integration tests green under `dev test` / targeted Fixie
- [ ] Notes: relationship to 104-004 deferred scope and task 108

## Notes

- Deferred from **104-004** Results: "api-server bearer validation → later task if needed."
- Triggered by **108** post-review: CommonServerModule covers web-server/MVC (tested);
  FastEndpoints inheritance of `ConfigureHttpJsonOptions` is presumed but unproven until an
  enum crosses that seam under test.
- Identity agent HTTP surface today is web-server-only (`api/identity/agent/*`); do not move
  those routes unless product requires dual host — this task is about **api-server host
  capability** for agents, not necessarily relocating the existing me endpoint.
- Depends on: 104-004 (done), 108 (done).

## Session

- Created: 2026-07-20 (capture deferred 104-004 + 108 FastEndpoints enum verification)
