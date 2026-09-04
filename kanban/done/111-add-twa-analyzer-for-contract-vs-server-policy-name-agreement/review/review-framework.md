# Review framework — task 111

**Date:** 2026-09-04
**Host task:** kanban/in-progress/111-add-twa-analyzer-for-contract-vs-server-policy-name-agreement/
**Diff scope:** branch `task/111-add-twa-analyzer-for-contract-vs-server-policy-nam` vs parent `661fc20f` (product commit `7b92afbb`; kitchen Results `4fdaff0a`). Do not review the stacked overnight history vs `origin/master`. Exclude kitchen `task.md` from product review (reviewer may cite Results claims to re-verify). Ignore uncommitted local `.gitignore`.
**Plan / brief:** Add TWA0024, a fail-closed agreement analyzer: hosted `[EndpointAuthorize] Policy` must equal a policy this server registers (`AuthorizationOptions`/`AuthorizationBuilder.AddPolicy`, or `PermissionIds` when `AddPermissionPolicies` is called). Mechanism is analyzer, not generation (contracts cannot reference server constants). Cover the three motivating families (identity-session, agent-scope, credential-management / PermissionIds).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:**
- Round 1: Grok review oracle 2026-09-04 (this task-work review node)
- Round 2: Grok review oracle 2026-09-04 (same node; re-verify M1 after AGENTS.md verb fix)

## Round 2 note

Round 1 is immutable. Product analyzer/tests did not change. Round 2 re-verifies M1 against the AGENTS.md stack-paragraph wording only; do not rubber-stamp round 1. Scan the fix delta for new defects.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `source/analyzers/timewarp-architecture-convention-analyzers/endpoint-authorize-policy-agreement-analyzer.cs`
- `source/analyzers/shared/hosted-route-discovery.cs`
- `source/analyzers/timewarp-architecture-convention-analyzers/endpoint-coverage-analyzer.cs`
- `source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md`
- `source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj`
- `source/analyzers/timewarp-architecture-attributes/endpoint-authorize-attribute.cs`
- `source/Directory.Build.props`
- `tests/analyzers/timewarp-architecture-analyzers-tests/endpoint-authorize-policy-agreement-analyzer-tests.cs`
- `AGENTS.md`
- `skills/tw-web-api-contracts/SKILL.md`
- `documentation/developer/reference/api-endpoint-source-generator.md`

## Surrounding call sites to re-verify

- TWA0006 pairing (`GetPairedContractAssemblies`) still correct after extraction
- Diagnostic ID TWA0024 uniqueness vs existing TWA table and generator IDs
- Real hosted `[EndpointAuthorize]` policies vs registered `AddPolicy` / `PermissionIds` (web + api)
- CORS `AddPolicy` is not treated as an authorization policy
- ClientOnly / missing Policy / contracts-only compilations stay silent
- Analyzer tests cover the three motivating families (clean + drift)
- Docs/skill/AGENTS table match the implemented rule
