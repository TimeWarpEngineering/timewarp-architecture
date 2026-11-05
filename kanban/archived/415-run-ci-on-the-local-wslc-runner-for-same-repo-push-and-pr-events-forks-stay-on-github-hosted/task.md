# Run CI on the local wslc runner for same-repo push and PR events (forks stay on GitHub-hosted)

## Description

Move architecture's CI jobs (`detect-paths`, the `ci-group` matrix: fast, playwright, integration, publish; the `ci` aggregate; `skill-lint`; `template-smoke` where it fits) off `ubuntu-latest` onto the local wslc container runner on TWE-001 (`twe-runner-01`, labels `self-hosted,twe-001,wslc`) for **same-repo** `push` and `pull_request` events only. PRs from forks stay on GitHub-hosted runners.

timewarp-architecture is a **PUBLIC** repo. Untrusted fork code must never run on Steven's machines. That is the hard rule for this task.

Filed 2026-10-25 at Steven's request ("change Nuru and architecture to run on the local wslc runner", 23:44 Asia/Bangkok). This is Steven's explicit decision and supersedes intel PR #74 action I (Busara: KILL, "no self-hosted runners for the public repos"), so the fork-safety requirements below are mandatory, not optional.

**Savings are estimates** from intel [PR #74](https://github.com/TimeWarpEngineering/timewarp-intel/pull/74) / [PR #75](https://github.com/TimeWarpEngineering/timewarp-intel/pull/75): today architecture runs 8.3 m median / 8.8 m p90 on hosted 4 vCPU / 15 GiB, with about 5 parallel jobs (critical path `ci (playwright)` 8.0 m). On a single local runner those jobs may run one after another, so wall time could go **up**. No before/after has been measured, and TWE-001's cores and RAM are unverified.

## Requirements

- `runs-on` picks `["self-hosted", "twe-001", "wslc"]` only when the event is `push` to this repo, or `pull_request` whose `head.repo.full_name == github.repository`. Everything else (fork PRs) uses `ubuntu-latest`.
  - Ganda's expression is the model, but it also requires `github.event.repository.private == true`, which is false here. Drop only the private check; keep the same-repo check.
  - Target the `wslc` label so jobs land on the container runner, not the bare-WSL systemd runner `twe-001`.
- **Fork safety (public repo):** a fork PR runs the workflow file from the PR, so a fork can edit `runs-on`. The expression alone does not protect Steven's machines. Before switching, confirm with Steven and record in Results:
  - fork PR workflows need approval for all external contributors (or stricter), and approvers check workflow diffs before approving;
  - the runner group ("Default"; the org is on the Free plan) is opened to public repos only with Steven's yes; that setting is org-wide. See timewarp-infrastructure task 004.
- Stay on GitHub-hosted: `release`, `workflow_dispatch` probe, `dev deploy` and anything Aspire-deploy related, and any job that needs a Docker socket (wslc has none; Aspire does not support wslc). Check whether the integration group or `template-smoke` app tests start containers; if so, they stay hosted.
- **Playwright:** the wslc image has no browsers or Node. The `ci (playwright)` job needs Chrome for Testing + headless shell (+ ffmpeg) and their OS deps. The image runs as non-root `runner` with no sudo, so `install --with-deps` will fail. Bake the browsers and deps into the image (timewarp-infrastructure 004), or cache them on a volume. Also measure `--only-shell` (intel #74 E).
- Architecture uses .NET 11 RC2; the image ships SDK 10. `setup-dotnet` into the tool cache must work as non-root, or the SDK is baked.
- **Parallel jobs vs a single runner:** the matrix runs about 5 jobs in parallel on hosted. One wslc runner instance runs one job at a time. Check capacity first (`nproc`, free RAM, number of runner instances: timewarp-infrastructure 004). If there's one instance, measure wall time; moving only some jobs (for example the fast ones) may be the better split.
- Keep the required check names unchanged (the stable `ci` aggregate, task 366).
- Measure before (hosted) vs after (wslc) wall time and queue time over several runs; nuru and ganda will share the runner.
- Use the newest current releases of actions and tools.

## Checklist

- [ ] Confirm runner capacity and the runner-group public-repo setting (timewarp-infrastructure 004)
- [ ] Confirm the fork-PR approval setting with Steven; record it
- [ ] List per job: needs Docker? needs browsers? needs sudo? Decide which jobs move
- [ ] Playwright browsers and deps available in the wslc image (004) or a cache
- [ ] `runs-on` expression: same-repo push/PR to `wslc`, forks to `ubuntu-latest`
- [ ] Prove: a same-repo PR runs on `twe-runner-01-*`; a fork PR stays hosted
- [ ] Record before/after wall time and queue time
- [ ] Open a PR with run links as proof

## Notes

- Related: Playwright sharding (intel #74 C) adds jobs, which makes the single-runner question sharper.
- Cross-repo: timewarp-infrastructure 004 (capacity, wslc targeting, image tools), timewarp-nuru 539 (the matching nuru task).
- Steven's yes before any workflow edit lands. The architecture repo bot owns the edit.
- WSLC runner detail is from Akili's read of the repos and job API (intel #75); Busara did not verify it.

## Session

- Created: 989935 (2026-10-25)

## Results
