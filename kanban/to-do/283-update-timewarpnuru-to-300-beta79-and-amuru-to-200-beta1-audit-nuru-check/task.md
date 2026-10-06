# Update TimeWarp.Nuru to 3.0.0-beta.79 and Amuru to 2.0.0-beta.1 (audit nuru check)

## Description

`ganda repo audit` now **blocks every merge**: `nuru: TimeWarp.Nuru is outdated (current:
3.0.0-beta.76, latest: 3.0.0-beta.79)`. PR #434 was merged with `--skip-audit` on 2026-10-06;
this task clears the check for everything after it.

`ganda repo audit --fix --checks nuru` changes `Directory.Packages.props` as follows:
- `TimeWarp.Nuru` / `TimeWarp.Nuru.DevCli`: 3.0.0-beta.76 → 3.0.0-beta.79
- `TimeWarp.Amuru` / `TimeWarp.Amuru.Tools`: 1.1.1 → **2.0.0-beta.1** (a major version,
  presumably required by Nuru beta.79)

Amuru 2 changes APIs that the dev CLI and the agent-identity CLI use.

**Head start:** a worker stopped partway through this on the 282 branch. Its uncommitted diff (the
pins plus API adaptations across ~27 files in `tools/dev-cli/` and `tools/agent-identity-cli/`)
is saved at:

`/tmp/claude-1000/-home-steve-worktrees-github-com-TimeWarpEngineering-timewarp-architecture-master/39e3a05a-ad7b-4ff6-bbe2-ee5ef56cddad/scratchpad/nuru-beta79-amuru2-wip.patch`

Apply it as a starting point (`git apply`), verify every hunk, and finish the job. It is **not**
verified: it never built or ran tests.

## Requirements

1. Confirm from Nuru beta.79's nuspec that it requires Amuru 2.0.0-beta.1. Move forward only;
   never pin TimeWarp packages backward or use `VersionOverride`.
2. Align every pin: CPM, runfile `#:package` directives (`tools/**`, `tests/**`, any other
   `.cs` runfiles), and the template files.
3. Fix Amuru 2 / Nuru beta.79 API breaks in `tools/dev-cli`, `tools/agent-identity-cli` and
   anywhere else, following the packages' migration notes (the `amuru` / `tw-nuru` skills may help).
   Reconcile Purpose/Design regions on touched files.
4. Rebuild `bin/dev` (`dotnet run tools/dev-cli/dev.cs -- self-install`), and smoke-test it with
   `./bin/dev --capabilities` and a couple of read-only commands, for example `dev check-version`
   and `dev db nuke` **without** `--yes`, which only lists.
5. If Amuru 2 changed behavior a `dev` command depends on (process exit codes, output capture,
   streaming), cover it with a test in `tests/tools/dev-cli-tests`.

## Checklist

- [x] Nuru beta.79 → Amuru 2 requirement checked. It is **not** required: the beta.79 nuspec
      depends on `TimeWarp.Amuru` ≥ 1.1.1. Amuru 2.0.0-beta.1 is the forward pin that
      `ganda repo audit --fix --checks nuru` writes, so it stays (move forward only, no `VersionOverride`)
- [x] All pins aligned. CPM is the only place versions live: runfile `#:package` directives
      (`.githooks/*.cs`) are unversioned, and the template tree has no Nuru or Amuru pins
- [x] API breaks fixed (dev-cli, agent-identity-cli); regions reconciled (none described the old API)
- [x] `bin/dev` rebuilt and smoke-tested
- [x] Gates: `dev build` 0/0, dev-cli tests, `dev test`, `dev template-smoke`, `ganda repo audit`
      (no blocking failures)
- [x] Do **not** start an AppHost, and do not run `dev db nuke --yes`
- [ ] Implementation review; host `open-pr`

## Session

- Created: 2026-10-06 (cockpit; split out of 282's merge per Steve)
- 2026-10-06 implementer (headless): applied the 282 WIP patch, verified every hunk, fixed the
  remaining break, and ran all gates.

## Results

**Pins** (`Directory.Packages.props`): `TimeWarp.Nuru` / `TimeWarp.Nuru.DevCli` 3.0.0-beta.76 →
3.0.0-beta.79; `TimeWarp.Amuru` / `TimeWarp.Amuru.Tools` 1.1.1 → 2.0.0-beta.1.

**Nuru beta.79 breaks** (Nuru now uses `TimeWarp.Mediator.Contracts` 14.0.0-beta.4):
- Handlers return `Task<Unit>` instead of `ValueTask<Unit>` (all dev-cli and agent-identity-cli
  endpoints, plus `WorkflowCommand.RunStepAsync`).
- `Unit` moved from `TimeWarp.Nuru` to `TimeWarp.Mediator`. Mediator's `Unit` also has a static
  `Task` member, so `global using static …Unit` made every `Task.Delay` / `Task.FromResult` in
  the dev CLI ambiguous (CS0229). The WIP patch never built, so it missed this. The fix drops the
  static using and writes `return Unit.Value;`.

**Amuru 2 breaks/behavior:** I checked our code against the 2.0 release notes' upgrade guide.
None of the changed surfaces is used in `tools/` or `.githooks/`:
- no `WithStandardInput`, `TtyPassthroughAsync`, `Git.*` helpers, fzf, `WithCollect`,
  `WithTerminalLogger`, or `WithProject`+`WithFile`.
- `PassthroughAsync` without stdin keeps its old behavior.

So requirement 5 (add a test) does not apply: no `dev` command depends on a changed behavior. The
`.githooks` runfiles and agent-identity-cli build clean on Amuru 2.

**Gates:**
- `dev build` 0 warnings / 0 errors.
- dev-cli-tests 102/102; agent-identity-cli-tests 11/11.
- `dev test` exit 0, all suites passed.
- `dev template-smoke` SUCCEEDED.
- `ganda repo audit` passes. The nuru check is clean. One advisory warning is left:
  `memsearch-scaffold` reports the three post-* hooks as outdated. Its `--fix` would add back the
  `ganda memsearch index-repo` calls that ganda task 339 removed on purpose, so I did not apply
  it. This looks like the installed ganda (1.0.0-beta.37) being older than task 339. The warning
  does not block.

### How to validate

Smoke:
```bash
dotnet run tools/dev-cli/dev.cs -- self-install
./bin/dev --capabilities | head
./bin/dev check-version
./bin/dev db nuke            # no --yes: lists only, exits 1
./bin/dev build
(cd tests/tools/dev-cli-tests && dotnet test -c Release)
ganda repo audit
```

Expect:
- self-install succeeds, and `--capabilities` prints the JSON endpoint list.
- check-version prints "Version in source is new".
- `db nuke` prints "Refusing to nuke without --yes" and the volume list.
- `dev build` reports 0 Warning(s) / 0 Error(s).
- dev-cli-tests show 102/102 passed.
- The audit passes with no `nuru` failure. The only remaining item is the advisory `memsearch-scaffold` warning.
