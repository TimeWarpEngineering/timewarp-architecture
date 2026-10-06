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

- [ ] Nuru beta.79 → Amuru 2 requirement confirmed
- [ ] All pins aligned (CPM, runfiles, template)
- [ ] API breaks fixed (dev-cli, agent-identity-cli, others); regions reconciled
- [ ] `bin/dev` rebuilt and smoke-tested
- [ ] Gates: `dev build` 0/0, dev-cli tests, `dev test`, `dev template-smoke`, `ganda repo audit`
      (no blocking failures)
- [ ] Do **not** start an AppHost, and do not run `dev db nuke --yes`
- [ ] Implementation review; host `open-pr`

## Session

- Created: 2026-10-06 (cockpit; split out of 282's merge per Steve)

## Results

*(fill when done)*

### How to validate

*(required before done)*
