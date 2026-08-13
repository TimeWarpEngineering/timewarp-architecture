# How to swap the permission evaluator for an external PDP

Replace the template’s **in-process** role→permission expansion with OpenFGA, Cedar, SpiceDB, or
any custom policy decision point (**PDP**) **without** rewriting endpoints, page policies, or SPA
AuthorizeView policy names.

**Architecture:** [ADR-0010 — Permission-centric authorization](../conceptual/architectural-decision-records/approved/0010-permission-centric-authorization.md).

**Default:** generated apps and this template **do not** require OpenFGA (or any external PDP) in
Aspire AppHost. The golden path is in-process `PermissionEvaluator` + `IRolePermissionStore`.

## Purpose

Product surfaces enforce **permissions** (`PermissionIds` strings as policy names). The only
decision port is `IPermissionEvaluator`. Swapping the registered implementation is how you plug in
an external engine.

## What you must not change

Leave these alone so contracts, generators, and SPA policies keep working:

| Surface | Why |
|---------|-----|
| `PermissionIds` / policy names on contracts and pages | Enforcement vocabulary SSOT |
| `PermissionRequirement` + `PermissionRequirementHandler` | Always call the port — never roles/claims bags |
| `PermissionPolicyRegistration.AddPermissionPolicies` | Server policy registration |
| SPA `AddPermissionClaimPolicies` + `PermissionIds.ClaimType` | Session still projects **evaluator** output |

## The seam

```text
source/container-apps/web/features/authorization/i-permission-evaluator-application.cs
```

```csharp
public interface IPermissionEvaluator
{
  Task<bool> HasPermissionAsync(
    PrincipalId principalId,
    string? authenticationScheme,
    string permissionId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<string>> GetPermissionsAsync(
    PrincipalId principalId,
    string? authenticationScheme,
    CancellationToken cancellationToken = default);
}
```

- **Server:** `PermissionRequirementHandler` → `HasPermissionAsync` only.
- **SPA session:** `GetCurrentSession` → `GetPermissionsAsync` under `identity-session`; claims
  projected for WASM `RequireClaim` policies.
- **Scheme-aware (default implementation):**
  - Human schemes (`identity-session`, `mock-identity-session`) → effective roles →
    `IRolePermissionStore`.
  - `agent-token` → ambient scopes from `IAgentPermissionScopeSource` expanded by
    `AgentScopePermissionSeed` only (no human role inheritance; principal id must match).
  A custom PDP must honor the same scheme split (or document a deliberate alternative).

## Where the default is registered

`source/container-apps/web/projects/web-server/program.cs`:

```csharp
serviceCollection.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
```

Consumer replace (after your own module / composition root runs, or instead of the default line):

```csharp
// Do not register the default PermissionEvaluator when using an external PDP.
serviceCollection.AddScoped<IPermissionEvaluator, OpenFgaPermissionEvaluator>();
// If the default was already registered (e.g. template program.cs then your module):
// using Microsoft.Extensions.DependencyInjection.Extensions;
// serviceCollection.Replace(
//   ServiceDescriptor.Scoped<IPermissionEvaluator, OpenFgaPermissionEvaluator>());
```

Also register HTTP clients, credentials, and options your adapter needs.

**Leave registered:**

```csharp
serviceCollection.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
// and PermissionPolicyRegistration.AddPermissionPolicies(options) in AddAuthorization
```

## What a custom evaluator must honor

1. Map `(PrincipalId, authenticationScheme, permissionId)` → allow/deny (`HasPermissionAsync`).
2. `GetPermissionsAsync` returns the **same expanded set** used for SPA session projection
   (single SSOT for server and SPA). Prefer stable catalog order where you know ids.
3. Prefer **scoped** lifetime (per-request context; avoid captive `DbContext` / `HttpClient` mistakes).
4. Fail **closed** on PDP errors in **your** adapter (deny / empty). The default in-process
   `PermissionEvaluator` expands human roles or agent scopes (or returns empty for unknown schemes);
   it does not catch store exceptions and map them to deny—treat that as your adapter’s responsibility.
5. Keep the identity-session **cookie PrincipalId-only** — do not bake grants into the cookie
   (rebundle / PDP changes take effect next request).

## SPA implications

Blazor WASM does **not** host the evaluator. After a correct swap, `GetCurrentSession` still runs on
the server and calls `IPermissionEvaluator`; `IdentitySessionAuthenticationStateProvider` still
projects `Response.Permissions` → claims. No SPA rewrite if the adapter is correct.

## External engines (illustrative only)

These sketches are **consumer-owned**. The template does **not** ship OpenFGA models, Cedar policy
stores, migrations, or Aspire resources for them.

### OpenFGA (sketch)

- Represent principals as users (`user:principal:{guid}`).
- Represent permissions as relations or objects your model defines.
- `HasPermissionAsync` → OpenFGA Check; `GetPermissionsAsync` → ListObjects / expand as appropriate.
- Map product roles to tuples **in the PDP** or keep role membership in-app and only ask the PDP
  for final checks — pick one story and stick to it.

### Cedar (sketch)

- `permit(principal, action == permissionId, resource)` against your policy store.
- Pass principal id and optional resource when you adopt instance-level checks.

## AppHost / ops default

**Do not** add OpenFGA (or Cedar/SpiceDB) to the template AppHost as a required dependency for this
swap. External PDPs are optional ops complexity for apps that need them. Document connection strings
and secrets in **your** app, not in the template’s default `aspire-app-host`.

## Entra branch

When `Authentication:UseEntra` is true, SPA may still use a separate claims path. **When you next
touch Entra**, migrate SPA permission claims to the same evaluator-backed source as the passkey path
(`GetCurrentSession.Permissions`). Do not maintain a second permission map. See [ADR-0010](../conceptual/architectural-decision-records/approved/0010-permission-centric-authorization.md).

## Testing without a real PDP

- Register a **fake** `IPermissionEvaluator` in DI for host/integration tests (existing host-test
  patterns).
- Co-located `permission-evaluator-tests.cs` exercises the **default** in-process implementation only.
- **No runtime OpenFGA** (or other external engine) is required to validate this how-to or the
  template’s default path.

## Related

- [ADR-0010](../conceptual/architectural-decision-records/approved/0010-permission-centric-authorization.md)
- `PermissionIds`, `IPermissionEvaluator`, `PermissionEvaluator`, `AgentScopePermissionSeed`,
  `IAgentPermissionScopeSource`
- Agent scopes → permission bundles shipped under **182-006**
- Lockout guards (last-admin / protected-core) remain application-layer, not PDP-specific

<!-- markdownlint-disable-file MD013 -->
