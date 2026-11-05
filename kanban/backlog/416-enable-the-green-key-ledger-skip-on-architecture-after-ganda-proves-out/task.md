# Enable the green-key ledger skip on architecture after ganda proves out

## Description

Phase 2 of the green-key ledger for architecture: skip a CI run whose content key already passed in an earlier green run. Architecture is second in the rollout (ganda, then architecture, then nuru).

Source: [timewarp-intel PR #75](https://github.com/TimeWarpEngineering/timewarp-intel/pull/75), action A as reworked by Busara (shadow mode first on ganda; then ganda → architecture → nuru; Steven's yes first).

**Savings are estimates** from intel PR #74/#75: over architecture's last 93 push/PR runs, 45 (349 run-wall-min, 49%) had a code-input key that had already passed, about 479 run-wall-min per week, and runner-minutes about 4–5× that because of the parallel matrix. These are code-identical re-runs (PR → master merges, `feature/**` pushes, synchronize), which `detect-paths` can't catch. Not measured.

Filed 2026-10-25 at Steven's request ("File tasks to do this", 23:44 Asia/Bangkok).

## Requirements

- Port the ledger from ganda's dev CLI (timewarp-ganda 469 and 470) into architecture's `tools/dev-cli/dev.cs`. Each repo has its own dev CLI.
- Run a short shadow window first (log hit or miss, skip nothing) and compare with the estimate. Any false skip (a hit whose full run failed) blocks enabling.
- Same key and rules as ganda: code inputs including lint inputs jobs read (for example `SKILL.md` for `skill-lint`), SDK version, `global.json`, workflow file and scripts, runner image version, container image digests; same-repo master/PR runs write markers, never fork PRs; the nightly `schedule` run, `release` and `workflow_dispatch` never skip.
- `ContinuousIntegrationBuild` is already set when `CI=true` in `source/Directory.Build.props` (packable projects only); confirm the deterministic check (intel #75 C) covers the test projects too.
- Fit with `detect-paths`: the ledger skip happens before the group matrix, and the stable `ci` aggregate (task 366) still reports success.
- On master, carry the green run's package artifact forward, or make the release lookup tolerate skipped commits.
- Record skipped runs and minutes saved in Results.

## Checklist

- [ ] Confirm the gates in Notes are met
- [ ] Port the ledger; short shadow window
- [ ] Enable the skip; prove a tree-equal master push skips and `ci` is green
- [ ] Prove the nightly and release paths never skip
- [ ] Record skips and minutes saved
- [ ] Open a PR with run links as proof

## Notes

- Cross-repo gates (not expressible in `## Depends on`): timewarp-ganda 469's Results show about 40 shadow runs with no false skip, and timewarp-ganda 470 (skip on ganda) has merged.
- Next in the rollout: timewarp-nuru 540.
- If 415 (wslc runner) lands first, the runner image version in the key is the wslc image.
- Steven's yes before any workflow edit lands. The architecture repo bot owns the edit.

## Session

- Created: 989935 (2026-10-25)

## Results
