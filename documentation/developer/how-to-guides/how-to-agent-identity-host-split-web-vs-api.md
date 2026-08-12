# Agent identity host split: web-server vs api-server

Identity for agents is split by **capability**, not duplicated as two full identity systems.

## What lives where

| Surface | Host | Notes |
|---------|------|--------|
| Passkey register / authenticate | **web-server** | Browser session cookie (`identity-session`) |
| Agent key registration | **web-server** | `api/identity/agent/key/*` |
| Agent token issuance | **web-server** | `api/identity/agent/token*` — opaque bearer, store-backed |
| Agent “who am I” (product) | **web-server** | `GET api/identity/agent/me` (`identity:read`) |
| Credential list / revoke | **web-server** | Cookie or `credential:manage` agent scope |
| Metered capability demo | **web-server** | `demo:invoke` + x402 |
| Agent bearer **validation** | **web-server and api-server** | Same scheme name (`agent-token`), same scope claim types, same `IAgentTokenStore` + `IPrincipalStore` ports |
| Agent bearer sample (teaching) | **api-server** | `GET api/agent/bearer/me` — proves policies + string-enum wire on FastEndpoints |

Ceremonies that **mint** principals, credentials, and tokens stay on **web-server**. **api-server**
hosts the same **validation** stack so agents can call protected product routes there without a
second opaque-token design.

## Shared contract (must not diverge)

| Item | Value |
|------|--------|
| Scheme | `agent-token` |
| Scope claim | `timewarp:scope` |
| Principal id claim | `timewarp:principal_id` |
| Policy examples | `agent-scope:identity:read`, `agent-scope:demo:invoke` |
| Token port | `TimeWarp.Identity.IAgentTokenStore` |
| Principal port | `TimeWarp.Identity.IPrincipalStore` |

api-server constants: `api/platform/identity-host/agent-token-defaults-server.cs`  
web-server constants: `web/platform/identity-host/agent-token-defaults-server.cs`  
(plus web `IdentitySessionDefaults.PrincipalIdClaimType` for the principal id claim)

## Token store locality (important)

Default template wiring uses **in-memory** `IAgentTokenStore` / `IPrincipalStore` **per process**.

- A token issued on web-server is **not** visible to api-server’s in-memory store (and vice versa).
- Integration tests for api-server **seed** principal + `IAgentTokenStore.Issue` on the api host.
- Multi-instance or dual-host production needs a **shared** token (and principal) store (e.g. Redis
  for grants, EF/Postgres for principals) behind the same ports — the authentication handler shape
  does not change.

## String enums through FastEndpoints

Both hosts apply `ContractSerializationDefaults` via `CommonServerModule` →
`ConfigureHttpJsonOptions` (camelCase properties; PascalCase string enums; integers fail closed).

- **web-server:** proven on `GET api/identity/agent/me` (`Agent_Protected_Endpoint_Tests`).
- **api-server:** proven on `GET api/agent/bearer/me` (`get-agent-bearer-identity-tests.cs`).

## Adding a protected agent route on api-server

1. Contract with `[ApiEndpoint]` + `[EndpointAuthorize(Policy = "agent-scope:…")]`.
2. Register the matching policy on api-server (already: `identity:read`, `demo:invoke`).
3. Ensure the process can resolve `IAgentTokenStore` / `IPrincipalStore` for the principals that
   will call it (shared store in multi-host deploys).

## Related

- Task **104-004** — agent keys + tokens on web-server  
- Task **104-030** — api-server bearer validation + FE string-enum verification  
- Task **108** — contract-seam string enums (`ContractSerializationDefaults`)
