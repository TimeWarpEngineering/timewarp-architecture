# Add TWA analyzer for contract-vs-server policy-name agreement

## Description

Origin: 110 review round-1 M2 (general). Policy name `"identity-session-authenticated"` now lives
as six comment-coordinated string literals across two assemblies — the `[EndpointAuthorize(Policy =
"identity-session-authenticated")]` literal repeated on five web-contracts contracts
(create/update/delete/get-role, get-roles) plus the `IdentitySessionDefaults.AuthenticatedPolicy`
constant in web-server — agreeing only by convention (a `// matches
IdentitySessionDefaults.AuthenticatedPolicy` comment, not a compiler-checked reference). web-contracts
cannot reference web-server's constant directly (wrong dependency direction), which is why this
pattern exists at all — it also already applies to `get-agent-identity.cs`'s
`[EndpointAuthorize(Policy = "agent-scope:identity:read")] // matches
AgentTokenDefaults.IdentityReadPolicy`.

Fail-closed + the current test suite catch a *drifted* literal today (a policy name that stops
matching means the endpoint 401s where it shouldn't, or vice versa — visible), but nothing catches
it at build time, and the coupling is easy to miss when adding new policies. A prefer-analyzers
directive candidate (see AGENTS.md "Prefer analyzers/source generators over convention-by-memory"):
generate one side from the other, or add a build-time agreement check between the contract-side
string literals and the server-side policy-name constants.

## Checklist

- [x] Decide the mechanism: new TWA analyzer cross-referencing contract `[EndpointAuthorize(Policy =
      "...")]` literals against known server policy-name constants, vs. a source-generation approach
      that derives one side from the other
- [x] Cover all three known instances: the five roles contracts'
      `"identity-session-authenticated"` vs. `IdentitySessionDefaults.AuthenticatedPolicy`,
      `get-agent-identity.cs`'s `"agent-scope:identity:read"` vs.
      `AgentTokenDefaults.IdentityReadPolicy`, and (104-005 review round-1 M2) the four
      credential-management contracts' (`get-credentials.cs`, `revoke-credential.cs`,
      `add-passkey.cs`, `add-agent-key.cs`) `"credential-management"` literal vs.
      `CredentialManagementDefaults.Policy`
- [x] Tests (analyzer positive/negative, or generator round-trip)
- [x] Docs: note the new check in AGENTS.md's TWA table and the web-api-contracts skill if the
      convention changes
- [x] dev build 0/0; full dev test
- [x] Implementation review disposition (effort 1, same id; no sibling apply-review task)

## Notes

Do not expand task 110's scope to cover this — 110 left the coupling documented via comments only
(`// matches IdentitySessionDefaults.AuthenticatedPolicy`) and deliberately did not build a new
analyzer for it.

104-005 review round-1 (M2) is a THIRD motivating instance: `CredentialManagementDefaults.Policy`
("credential-management") duplicated as a string literal across `get-credentials.cs`,
`revoke-credential.cs`, `add-passkey.cs`, and `add-agent-key.cs`. Same coupling, same interim
mitigation (fail-closed + `// matches CredentialManagementDefaults.Policy` comment) — not fixed
per-task, tracked here.

Task 182 moved web product policies onto `PermissionIds` (policy name == permission id,
`AddPermissionPolicies` registers `All`). The original role/credential/web-agent literals are
gone from web-contracts; api-server still uses `AgentTokenDefaults.IdentityReadPolicy`
(`"agent-scope:identity:read"`) as a comment-coordinated literal on
`get-agent-bearer-identity-contracts.cs`. The analyzer covers both the historical shapes and the
PermissionIds path.

## Session

- Implementer: Grok session 01a06cce-8be4-7e93-945e-cbe0ce915e8a (2026-09-04)
- Review oracle: Grok session 01a06ce7-0a33-7f50-936f-ac978090feb1 (2026-09-04)

## Results

**Mechanism:** agreement analyzer **TWA0024**, not source generation. Contracts cannot reference
server-layer constants (wrong dependency direction). Web already has `PermissionIds` as the
product policy SSOT; generating one side from the other would duplicate that catalog or invert
the contracts→server dependency. The server compilation sees both sides (same pairing as TWA0006:
this assembly plus `*contracts` with the same first name segment).

**Rule:** a hosted `[EndpointAuthorize] Policy` value must equal a policy this server registers:

- constant-evaluated `AuthorizationOptions` / `AuthorizationBuilder.AddPolicy` first arguments
- plus `PermissionIds` public const strings except `ClaimType`, when
  `PermissionPolicyRegistration.AddPermissionPolicies` is called

CORS `AddPolicy` is ignored. ClientOnly / missing Policy / contracts-only compilations are silent.

**The three motivating instances** (as analyzer tests, reflecting post-182 names):

| Instance | Clean | Drift |
|----------|-------|-------|
| identity-session | literal `"identity-session-authenticated"` vs `IdentitySessionDefaults.AuthenticatedPolicy` on `AddPolicy` | `"identity-session-authed"` → TWA0024 |
| agent-scope | literal `"agent-scope:identity:read"` vs `AgentTokenDefaults.IdentityReadPolicy` on `AddPolicy` | `"identity.read"` against api's registered name → TWA0024 |
| credential-management | `PermissionIds.CredentialManageSelf` with `AddPermissionPolicies` | historical `"credential-management"` → TWA0024 |

**Files changed**

- `source/analyzers/timewarp-architecture-convention-analyzers/endpoint-authorize-policy-agreement-analyzer.cs` (new)
- `source/analyzers/shared/hosted-route-discovery.cs` (`GetPairedContractAssemblies`)
- `source/analyzers/timewarp-architecture-convention-analyzers/endpoint-coverage-analyzer.cs` (use shared pairing)
- `source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md`
- `source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj`
- `source/analyzers/timewarp-architecture-attributes/endpoint-authorize-attribute.cs` (Design)
- `source/Directory.Build.props` (package-range comment)
- `tests/analyzers/timewarp-architecture-analyzers-tests/endpoint-authorize-policy-agreement-analyzer-tests.cs` (new; 12 tests)
- `AGENTS.md` TWA table + package range (review M1: stack-paragraph verb)
- `skills/tw-web-api-contracts/SKILL.md`
- `documentation/developer/reference/api-endpoint-source-generator.md`

**Test outcomes:** `./bin/dev build` 0/0. Analyzer suite 157 passed (includes 12 TWA0024).
`./bin/dev test` passed (pre-existing skips: `RunForever`, quarantined SPA weather fetch).
Round-1 re-verify: `Should_Enforce_Policy_Agreement` 12/12; web-server and api-server Release 0/0.

### Review disposition

**Outcome:** clean. **Rounds:** 2. **Effort:** 1 (general only). **Final open count:** 0.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

M1 (nit): AGENTS.md stack paragraph was missing a verb (`TWA0024 that a named Policy is
registered`). Fixed on this id to `TWA0024 enforces that a named Policy is registered`. No
product-code findings. No wontfix / escalations.

Paths: `review/review-framework.md`, `review/round-1/{general,merged}.md`,
`review/round-2/{general,merged}.md`, `review/disposition.md`.

### How to validate

**Smoke**

```bash
cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release -- --filter-class Should_Enforce_Policy_Agreement
```

**Expect**

- 12 tests passed (clean agreement for the three policy families; TWA0024 on drifted
  identity-session / agent-scope / credential-management literals; CORS `AddPolicy` ignored;
  ClientOnly and contracts-only compilations silent).

**Automated gate**

```bash
./bin/dev build   # expect: 0 Warning(s) 0 Error(s)
./bin/dev test    # expect: Tests completed successfully
```

**Not in scope:** scheme-name agreement (`AuthenticationSchemeNames` vs server scheme constants)
and generating policy constants into contracts.
