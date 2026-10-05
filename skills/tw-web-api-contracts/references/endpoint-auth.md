# Hosted endpoint auth: policy names and scheme lists

**Policy names (task 111 / TWA0024):** `[EndpointAuthorize] Policy` must equal a policy the
hosting server actually registers. Web product policies are `PermissionIds.*` const references
(registered by `AddPermissionPolicies`; policy name == permission id). Named non-permission
policies (api-server `AgentTokenDefaults.IdentityReadPolicy`, web
`IdentitySessionDefaults.AuthenticatedPolicy`) are constant-evaluated `AddPolicy` first
arguments. Contracts cannot reference server-layer constants, so agreement is a **server-build**
check — a drifted literal 403s at runtime; **TWA0024** flags it at compile time.
Prefer a contracts-visible const (`PermissionIds`, or a family-local const the server
`AddPolicy` also uses) over a comment-coordinated string literal.

**Scheme lists (task 161):** hosted `[EndpointAuthorize]` must set `AuthenticationSchemes` using
`AuthenticationSchemeNames` (web) or the matching server scheme string (api). PermissionIds
policies registered via `AddPermissionPolicies` have **no** `AddAuthenticationSchemes`; ASP.NET
Core 10's `PolicyEvaluator` then authenticates only the host default scheme (`identity-session`
on web). `agent-token` and `mock-identity-session` never run unless the generated FastEndpoint
emits `AuthSchemes(...)`. Named-policy scheme lists still Combine when present (api-server
agent-scope policies) — still declare them on the contract so a policy-registration change
cannot drop them. Do **not** put scheme lists back on permission policies (see `IPermissionEvaluator` Design region in `source/container-apps/web/platform/authorization/i-permission-evaluator-application.cs`).

| Surface | `AuthenticationSchemes` |
|---------|-------------------------|
| Admin BFF (closed-box mock) | `IdentitySession + "," + MockIdentitySession` |
| Credential management (human or agent) | `IdentitySession + "," + AgentToken` |
| Agent-token-only | `AgentToken` |
