# Refactor dev-cli deploy and entra services and commands over 500 lines

## Parent

469

## Description

The dev-cli deploy and entra code is the densest cluster of over-limit files, and several of
them stack many top-level types. Split each file into one type per file, then look at the
remaining large classes for mixed responsibilities (argument parsing, process orchestration,
output rendering, and cloud API calls tend to sit together).

## Files (lines, top-level types)

- tools/dev-cli/services/template-smoke-harness.cs (1315, 3)
- tools/dev-cli/services/aspire-deploy.cs (919, 10)
- tools/dev-cli/services/deploy-operate.cs (814, 9)
- tools/dev-cli/endpoints/template-smoke-command.cs (662, 1)
- tools/dev-cli/endpoints/entra-setup-command.cs (662, 1)
- tools/dev-cli/services/entra-setup.cs (604, 1)
- tools/dev-cli/services/entra-tenants.cs (537, 4)
- tools/dev-cli/endpoints/deploy-command.cs (525, 1)

## Requirements

- One top-level type per file. Each file 500 lines or fewer.
- Endpoints stay thin. Logic that an endpoint file holds moves to a service.
- Behavior unchanged. tests/tools/dev-cli-tests still pass. `dev` AOT self-install still
  builds (see tw-dev-cli; the installed `bin/dev` goes stale after dev-cli edits).
- Wrap tools, do not reconstruct their internals, when moving process-calling code.

## Goal (implementer restatement)

Break the eight over-limit dev-cli deploy, entra, and template-smoke files into files of 500 lines
or fewer that each hold one top-level type, without changing what any `dev` verb does.

Acceptance criteria:

- Every record, enum, and class that shared a file with another top-level type gets its own file.
- `TemplateSmokeHarness`, `AspireDeploy`, and `DeployOperate` are decomposed along their
  responsibilities (parameter and secret resolution, cluster and Azure probes, report text,
  migration helpers, compose and Kubernetes open helpers, and so on). Each resulting file is 500
  lines or fewer.
- `EntraSetup` and `EntraTenants` end at 500 lines or fewer.
- `template-smoke`, `entra setup`, and `deploy` endpoints keep their options and route surface and
  hand the work to a service; the endpoint files shrink well under 500 lines.
- No class is spread across `partial` files to get under the limit. `partial` stays only where
  `[GeneratedRegex]` needs it. `TemplateSmokeHarness` composes separate types, one per
  responsibility (fix pass after PR #626).
- Process calls stay Amuru wrappers around the real tools. No tool internals are re-implemented.
- `dotnet build` of `tools/dev-cli/dev.cs` is 0 warnings, the dev-cli Jaribu tests pass, `dev build`
  passes, `./bin/dev build`-equivalent AOT self-install still publishes, and `ganda repo audit` is clean.

## Checklist

- [x] Split multi-type files
- [x] Decompose template-smoke-harness by responsibility
- [x] Decompose aspire-deploy and deploy-operate by responsibility
- [x] Trim endpoints
- [x] dev build 0 warnings, dev-cli tests green
- [x] Fix pass: replace the `TemplateSmokeHarness` partial files with composed types

## Results

All eight files are split. Each file holds one top-level type, and every dev-cli `.cs` file is now
500 lines or fewer. The largest is `services/aspire-run.cs` at 496 lines, which this task did not touch.

- **Deploy** (`services/deploy/`, pure, Compile-included by dev-cli-tests):
  - Each of the 17 records has its own file.
  - `AspireDeploy` is split into `AspireDeployParameters`, `AspireDeploySecrets`,
    `AspireDeployCluster`, `AspireDeployAzure`, `AspireDeployEnvironment` and `AspireDeployText`.
    The core keeps the targets and argument builders.
  - `DeployOperate` is split into `DeployKubernetes`, `DeployCompose`, `DeployAca`,
    `DeployMigrationHistory`, `DeployStaleScript` and `DeployBrowserChoice`.
- **Deploy endpoint:** `deploy-command.cs` is 52 lines. Orchestration moved to `services/deploy-runner.cs`
  and `services/deploy-smoke-runner.cs`. The migrate step is passed in as a delegate.
- **Entra** (`services/entra/`, pure, Compile-included by dev-cli-tests):
  - The records and enum each have their own file.
  - `EntraSetup` is split into `EntraRedirectUris`, `EntraUserSecrets`, `EntraClientSecrets` and `EntraAzJson`.
  - `EntraTenants` is split into `EntraTenantJson` and `EntraAppLookup`. The duplicate
    `TryReadStringProperty` is now one copy on `EntraAzJson`.
- **Entra endpoint:** `entra-setup-command.cs` is 62 lines. The work moved to `EntraSetupRunner`, which
  uses an options record, a context, and tenant, app and secrets steps.
- **Template smoke** (`services/template-smoke/`):
  - `TemplateSmokePaths` and `PlatformPackagePropsSsot` each have their own file.
  - Pure helpers are new classes: `PlatformPinRules`, `JaribuOutputParser`, `SourceNameLiteralScanSets`,
    `TemplateSmokeTestTables` and `TemplateSmokeAppHostParameters`.
  - `TemplateSmokeHarness` is no longer partial. It is a composition root that builds one instance of
    each responsibility type and exposes it as a property:
    - `TemplateSmokeArchitectureSuffixes` (`Suffixes`) derives the suffixes from the props SSOT and caches them.
    - `TemplateSmokePlatformPins` (`Pins`) holds the CPM pin rewrite and equality checks and the sourceName
      rewrite scans.
    - `TemplateSmokeGeneratedContent` (`Content`) checks skills, the kanban skeleton, deploy parameters and the
      ingress port.
    - `TemplateSmokeTestRuns` (`TestRuns`) runs the co-located tests, the Jaribu aggregators and the generated
      suites.
    - `TemplateSmokeGeneration` (`Generation`) holds `GenerateAsync` and `SmokeOneAsync`. It uses `Pins` for the
      package-ID check.
    - `TemplateSmokeInitializerAssets` (`InitializerAssets`) checks the initializer import graph.
  - Callers use these properties, for example `Harness.Generation.SmokeOneAsync`. `TemplateSmokePackaging` now
    takes `TemplateSmokeArchitectureSuffixes`. `TemplateSmokeGeneratedAppChecks` now takes `TemplateSmokePlatformPins`.
  - In dev-cli, `partial` is left only on classes that declare `[GeneratedRegex]` methods, and each of those
    classes is in a single file.
  - `template-smoke-command.cs` is 36 lines. The work moved to `TemplateSmokeRunner`, `TemplateSmokeMatrix`,
    `TemplateSmokePackaging` and `TemplateSmokeGeneratedAppChecks`.
- **Other edits:**
  - Callers in other endpoints and services and in the dev-cli tests now use the new class names.
  - `dev-cli-tests.csproj` now globs `services/deploy/*.cs` and `services/entra/*.cs`.
  - The `skills/tw-deploy/SKILL.md` path to `deploy-operate.cs` is updated.
- **Still over 500 lines:** the test files `aspire-deploy-tests.cs` (972) and `deploy-operate-tests.cs` (907).
  They belong to sibling task 469-006.

Gates run in this worktree (re-run after the fix pass, 2026-11-04):
- `dotnet build tools/dev-cli/dev.cs`: 0 warnings, 0 errors.
- dev-cli-tests: 342 of 342 passed, the same count as the baseline before the change.
- AOT self-install published `bin/dev`, which lists the same 35 commands as before.
- `dev build`: passed.
- `dev lint-skills`: passed, 9 skills. This ran in the first pass only, because the fix pass did not change `skills/`.
- `ganda repo audit`: passes. Its one advisory is `kanban-landed` live claims on other task ids.
- `dev template-smoke` (default variant): passed with exit 0. Its checks passed: platform pins, unsafe
  namespace literals, package ID rewrite and package mode, and the co-located standalone tests.
  The fix-pass run also passed the initializer import graph and the tier-3 aggregators: web 458 of 458,
  api 9 of 9, and timewarp-testing 14 of 14.

### How to validate

Smoke:

```bash
dotnet build tools/dev-cli/dev.cs
(cd tests/tools/dev-cli-tests && dotnet run)
dotnet run tools/dev-cli/dev.cs -- self-install && ./bin/dev --capabilities | grep -c '"pattern"'
find tools/dev-cli -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' | xargs wc -l | sort -n | tail -3
grep -rhoE "partial class \w+" tools/dev-cli --include=*.cs | sort | uniq -c | sort -rn | head -3
./bin/dev template-smoke
```

Expect:
- The build has 0 warnings.
- The tests report 342 passed and 0 failed.
- The capabilities count is 35.
- No dev-cli `.cs` file is over 500 lines.
- Every `partial class` count is 1, so no class spans more than one file.
- `dev template-smoke` passes the default variant.

### Review

Round 1, roster general, effort 3. Disposition: **clean**. Final counts: 0 open, 0 fixed, 0 wontfix across bug, suggestion, and nit. No wontfix and no escalation.

The review compared the branch to merge-base `f95c82ea0`. The implementer restatement matches the original task (same eight files, one type per file, 500-line cap, unchanged verbs). No misread.

Paths:

- `review/review-framework.md`
- `review/round-1/general.md`
- `review/round-1/merged.md`
- `review/disposition.md`

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1a2d9-00bc-74e0-bf45-a6d476140429, max-turns 500, Blocked — 2026-11-03T19:18:22Z
- Implement oracle: implementer-claude (Opus 5.5) 2026-11-04. Three helpers split the deploy, entra and template-smoke families in parallel; the implementer fixed one cross-class reference, then ran the gates above.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-sonnet-5-5), session e5520f23-b338-4da6-bb58-51798600dd6b, max-turns 500, Done — 2026-11-03T19:36:07Z
- Review oracle: general reviewer (grok, vendor xai, model grok-4.7), session 01a1a2ea-60e5-7ee3-b154-2ef4e8c7dd68, effort 3, max-turns 200, clean — 2026-11-04
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1a2ea-60e5-7ee3-b154-2ef4e8c7dd68, max-turns 200, artifacts general.md, merged.md — 2026-11-03T19:49:15Z
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session ffcf55ad-f8cc-4656-9f6b-cd22dbc5e701, max-turns 500, Done — 2026-11-03T20:01:46Z
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1a301-9b53-7202-a6ef-9ef8866d3900, max-turns 200, artifacts none, warning: 1 turn below the floor of 8 — 2026-11-03T20:02:43Z
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1a301-9b53-7202-a6ef-9ef8866d3900, max-turns 200, artifacts none, warning: 1 turn below the floor of 8 — 2026-11-03T20:02:43Z

## Blocked

2026-11-03: review verdict refused: grok structured output stopped with num_turns of 1 or fewer. Round folder: /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/task-469-002-refactor-dev-cli-deploy-and-entra-services-and-com/kanban/in-progress/469-002-refactor-dev-cli-deploy-and-entra-services-and-commands-over-500-lines/review/round-2. Oracle log: /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/task-469-002-refactor-dev-cli-deploy-and-entra-services-and-com/kanban/in-progress/469-002-refactor-dev-cli-deploy-and-entra-services-and-commands-over-500-lines/oracle-review-20261103t200243681z.log.

## Review

- Round 1: Done

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
- Implement oracle fix pass: implementer-claude (Opus 5.5) 2026-11-04. Replaced the five
  `TemplateSmokeHarness` partial files with six composed types and re-ran the gates above.
- Fix pass requested (2026-11-04): PR #626 splits `TemplateSmokeHarness` into five `partial`
  files (`template-smoke-harness*.cs`, ~1050 lines as one class). The spec says `partial` is
  not an accepted way to get under the limit. Keep the `[GeneratedRegex]` static partials.
  Decompose the harness into separate types by responsibility (generation, generated-content
  checks, platform pins, test runs) that the harness composes, each 500 lines or fewer, then
  update tests and re-run the gates.
