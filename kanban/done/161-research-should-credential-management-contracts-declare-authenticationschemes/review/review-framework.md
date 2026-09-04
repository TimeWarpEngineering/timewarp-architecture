# Review framework — task 161

**Date:** 2026-09-04
**Host task:** kanban/in-progress/161-research-should-credential-management-contracts-declare-authenticationschemes/
**Diff scope:** branch `task/161-research-should-credential-management-contracts-de` vs `origin/feature/overnight` (product commit `b88603c0`). Product + tests + docs + skill; exclude kitchen `task.md` from product review (reviewer may cite Results claims to re-verify).
**Plan / brief:** Research whether hosted `[EndpointAuthorize]` contracts must declare `AuthenticationSchemes`, or whether named-policy `AddAuthenticationSchemes` is sufficient. Falsifiable FastEndpoints TestServer proof (`ProbeScheme_Given_`); coverage audit of credential/agent-token paths; hybrid litmus (option c) folded into skill/ADR/Design; last Policies-only hosted contract (`GetAgentBearerIdentity`) now lists `agent-token`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:**
- Round 1: Grok review oracle 2026-09-04 (this task-work review node)
- Round 2: Grok review oracle 2026-09-04 (same node; re-verify M1 after coverage-label fix)

## Round 2 note

Round 1 is immutable. Product code did not change. Round 2 re-verifies M1 against the post-fix coverage-audit wording in `task.md` only; do not rubber-stamp round 1.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `tests/container-apps/web/web-server-integration-tests/features/identity/fast-endpoint-auth-schemes-tests.cs`
- `source/container-apps/api/features/agent-bearer-sample/get-agent-bearer-identity/get-agent-bearer-identity-contracts.cs`
- `source/container-apps/web/features/identity/authentication-scheme-names-contracts.cs`
- `source/analyzers/timewarp-architecture-attributes/endpoint-authorize-attribute.cs`
- `source/container-apps/web/features/admin/roles/get-roles/get-roles-contracts.cs`
- `source/container-apps/web/platform/identity-host/http-current-principal-accessor-server.cs`
- `source/container-apps/web/platform/identity-host/i-current-principal-accessor-application.cs`
- `source/container-apps/web/features/identity/credential-list-tests.cs`
- `skills/tw-web-api-contracts/SKILL.md`
- `documentation/developer/conceptual/architectural-decision-records/approved/0010-permission-centric-authorization.md`
- `documentation/developer/how-to-guides/how-to-agent-identity-host-split-web-vs-api.md`
- `documentation/developer/reference/api-endpoint-source-generator.md`
- `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.md`

## Surrounding call sites to re-verify

- Remaining `[EndpointAuthorize]` without `AuthenticationSchemes` (claim: GetAgentBearerIdentity was last hosted Policies-only)
- `AddPermissionPolicies` / PermissionIds policy registration (no `AddAuthenticationSchemes`)
- api-server `AgentTokenDefaults` / IdentityReadPolicy still listing `agent-token`
- FastEndpoint generator emission of `AuthSchemes(...)` from `[EndpointAuthorize(AuthenticationSchemes)]`
- Credential-management contracts already listing dual schemes (182-006)
- Coverage table in task Results vs actual tests (GetCredentials/Revoke/AddPasskey/AddAgentKey/GetAgentIdentity/InvokeMeteredCapability; aspire-tests)
- Mechanism claims vs FastEndpoints `EndpointSecurityPolicies.BuildAuthorizeAttributes` and ASP.NET Core `PolicyEvaluator.AuthenticateAsync` / `AuthorizationPolicy.CombineAsync`
