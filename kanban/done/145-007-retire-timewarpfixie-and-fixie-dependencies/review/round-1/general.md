# Round 1 — general
**Date:** 2026-08-02
**Scope reviewed:** Fixie retirement 145-007 (static tree + package/docs claims; commits `b3bfd42a`..HEAD as framed by review-framework)

## Summary

Claims **1–4** and the **present-tense docs** half of claim **6** hold on the current tree:

| Claim | Result |
|-------|--------|
| 1. No Fixie / Fixie.TestAdapter / TimeWarp.Fixie PackageReference or PackageVersion (except comments) | **PASS** — grep of `*.csproj` / `*.props` / `*.targets` / `*.xml` finds zero package refs; CPM only pins `TimeWarp.Jaribu` + `TimeWarp.Jaribu.TestingPlatform`. |
| 2. Former Fixie suites are Jaribu MTP with project-local `global.json` | **PASS** — every suite-shaped `tests/**/*.csproj` with `IsTestProject=true` references `TimeWarp.Jaribu.TestingPlatform` + `TestingPlatformDotnetTestSupport=true` and has a sibling `global.json` with `"runner": "Microsoft.Testing.Platform"`. `timewarp-testing` is correctly `IsTestProject=false` (shared library). |
| 3. TimeWarpTestingConvention deleted; `[NotTest]` removed | **PASS** — no `TimeWarpTestingConvention` / `testing-convention` under `tests/`; no `[NotTest]` anywhere. Remaining `ThrowIfNotTestAssembly` is TimeWarp.State production guard, unrelated. |
| 4. `test-command.cs` is MTP-only | **PASS** — Design region + `BuildTestCommand` always run bare `dotnet test -c Release` with cwd = project dir; no Fixie/VSTest dual branch. |
| 5. `dev build` 0/0; template-smoke SUCCEEDED; `ganda repo audit` PASS | **NOT RE-RUN** in this review pass; task Session log only records analyzer-suite migrations + per-project `dotnet test`, not full gates. Checklist items still unchecked. Treat gate claims as **unverified by R1 general**. |
| 6. Docs/AGENTS zero-Fixie present tense | **Mostly PASS** — `AGENTS.md`, `documentation/test-structure.md`, integration-testing + file-naming standards, and `tw-web-api-contracts` skill use present-tense Jaribu / “do not reintroduce Fixie”. Residual stale **comments** and one kanban overview label remain (nits below). Historical `kanban/done|archived|analysis/**` Fixie mentions left as allowed. |

**Verdict:** product retirement shape looks complete for packages, conventions, suite projects, and `dev test`. Do not close 145-007 on claim 5 without fresh gate evidence. Clean up the two cross-suite comments that still assert the *other* suite is Fixie.

## Issues

### Issue 1 — Severity: nit
- File: `/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/dev/tests/libraries/timewarp-identity-tests/in-memory-principal-store-contract-tests.cs`
- Description: Header comment still says the EF fixture in `web-infrastructure-tests` is “still-Fixie … until that suite migrates.” That suite is already Jaribu MTP (`web-infrastructure-tests.csproj` + `global.json`).
- Suggestion: Reword to dual-fixture / instance-base re-export rationale without claiming a remaining Fixie suite.
- Status: open

### Issue 2 — Severity: nit
- File: `/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/dev/tests/container-apps/web/web-infrastructure-tests/ef-principal-store-contract-tests.cs`
- Description: Header comment says “Identity-tests still Fixie until its own migration.” `timewarp-identity-tests` is already Jaribu MTP.
- Suggestion: Drop the migration-debt sentence; keep the Jaribu static re-export Design if useful.
- Status: open

### Issue 3 — Severity: nit
- File: `/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/dev/kanban/overview.md`
- Description: Feature checklist still labels “**Integration Tests (Fixie):**”. Live guidance in a non-archived overview; conflicts with zero-Fixie present tense.
- Suggestion: Rename to Jaribu / integration tests (or “suite / co-located Jaribu”) without framework branding if the checklist stays.
- Status: open

### Issue 4 — Severity: suggestion
- File: task evidence (`145-007-…md` Session / Checklist); gates not exercised in this review
- Description: Claim 5 (build 0/0, template-smoke SUCCEEDED, audit PASS when artifacts cleaned) was not independently verified. Task Session only documents analyzer-suite migrations; checklist boxes for “dev build / full dev test / template-smoke / audit” remain unchecked.
- Suggestion: Before disposition/done, record fresh command output for `dev build`, `dev template-smoke`, and `ganda repo audit` (with smoke/artifacts cleaned as required). Optionally note full `dev test` separately if not re-run this session.
- Status: open
