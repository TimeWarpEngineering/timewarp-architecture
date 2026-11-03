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
- Process calls stay Amuru wrappers around the real tools. No tool internals are re-implemented.
- `dotnet build` of `tools/dev-cli/dev.cs` is 0 warnings, the dev-cli Jaribu tests pass, `dev build`
  passes, `./bin/dev build`-equivalent AOT self-install still publishes, and `ganda repo audit` is clean.

## Checklist

- [x] Split multi-type files
- [x] Decompose template-smoke-harness by responsibility
- [x] Decompose aspire-deploy and deploy-operate by responsibility
- [x] Trim endpoints
- [x] dev build 0 warnings, dev-cli tests green

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
  - `TemplateSmokeHarness` is split into partial files for the core, platform pins, generated content,
    test runs and generation.
  - `template-smoke-command.cs` is 36 lines. The work moved to `TemplateSmokeRunner`, `TemplateSmokeMatrix`,
    `TemplateSmokePackaging` and `TemplateSmokeGeneratedAppChecks`.
- **Other edits:**
  - Callers in other endpoints and services and in the dev-cli tests now use the new class names.
  - `dev-cli-tests.csproj` now globs `services/deploy/*.cs` and `services/entra/*.cs`.
  - The `skills/tw-deploy/SKILL.md` path to `deploy-operate.cs` is updated.
- **Still over 500 lines:** the test files `aspire-deploy-tests.cs` (972) and `deploy-operate-tests.cs` (907).
  They belong to sibling task 469-006.

Gates run in this worktree:
- `dotnet build tools/dev-cli/dev.cs`: 0 warnings, 0 errors.
- dev-cli-tests: 342 of 342 passed, the same count as the baseline before the change.
- AOT self-install published `bin/dev`, which lists the same 35 commands as before.
- `dev build`: passed.
- `dev lint-skills`: passed, 9 skills.
- `ganda repo audit`: passes. Its one advisory is `kanban-landed` live claims on other task ids.
- `dev template-smoke`: started, but it had not finished when this walk had to answer. PR CI runs the
  default smoke, so it is still pending.

### How to validate

Smoke:

```bash
dotnet build tools/dev-cli/dev.cs
(cd tests/tools/dev-cli-tests && dotnet run)
dotnet run tools/dev-cli/dev.cs -- self-install && ./bin/dev --capabilities | grep -c '"pattern"'
find tools/dev-cli -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' | xargs wc -l | sort -n | tail -3
./bin/dev template-smoke
```

Expect:
- The build has 0 warnings.
- The tests report 342 passed and 0 failed.
- The capabilities count is 35.
- No dev-cli `.cs` file is over 500 lines.
- `dev template-smoke` passes the default variant.

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1a2d9-00bc-74e0-bf45-a6d476140429, max-turns 500, Blocked — 2026-11-03T19:18:22Z
- Implement oracle: implementer-claude (Opus 5.5) 2026-11-04. Three helpers split the deploy, entra and template-smoke families in parallel; the implementer fixed one cross-class reference, then ran the gates above.

## Blocked

2026-11-03: placeholder — work not finished
