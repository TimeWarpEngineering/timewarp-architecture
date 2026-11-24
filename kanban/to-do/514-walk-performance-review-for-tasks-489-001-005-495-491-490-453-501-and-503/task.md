# Walk performance review for tasks 489-001..005, 495, 491, 490, 453, 501, and 503

## Description

Follow-up cross-walk performance analysis covering the **11 finished walks** in `timewarp-architecture` since Task 500 (**489-001..005** work-set, overnight tasks **495**, **491**, **490**, **453**, and post-update morning tasks **501**, **503**), measuring the impact of yesterday's changes (`roslynk` uninstall, Cloudflare MCP removal, Task 501 Playwright CI disable, and `ganda 1.0.0-beta.38+393401fa`) and surfacing remaining workflow bottlenecks.

Full report with measured SigNoz OTEL data, before/after comparison, per-walk timelines, and standalone per-item context lives in `report/analysis.md`.

Use this task to walk through each finding one by one and decide which follow-up tasks to file or route to `/tw-correct`.

## Checklist

- [x] Collect SigNoz OTEL traces, walk logs, agent transcripts, PR CI checks, and `ganda usage stats` for all 11 finished walks (`report/analysis.md`)
- [x] Item 1 (`dotfiles` task **012**) — Root-caused: not a `ganda 393401fa` or profile bug; interactive Claude `Bash` strips `OTEL_*` and `BASH_ENV` was unset when launching `setsid -f bash -c ... ganda task work`. Filed `dotfiles` task **012** to export `BASH_ENV="$HOME/.bashrc"` in `bash/.bashrc`
- [ ] Item 2 (`timewarp-ganda`) — Review Grok `grok-4.7-build` reasoning/API latency (`82–99%` of wall time, `~4h 40m` across 6 walks) vs Claude (`1.1–2.8m` review rounds, `6.6–24.8m` implementations) and decide provider/effort routing
- [ ] Item 3 (`timewarp-ganda`) — Add provider 429 quota cooldown file (OpenCode Go weekly limit aborted Task 495 after `13m 38s` with 0 edits — 2nd occurrence in 2 days)
- [ ] Item 4 (`timewarp-ganda` / `.githooks/`) — Eliminate redundant `post-commit` `self-install` + `repo attest` (`81–136s`/commit on `501`/`503`) and host `audit` + `merge` triple-audit (`52–54s` still live on `501`/`503`)
- [ ] Item 5 (`timewarp-architecture`) — Re-measure post-Playwright CI bottlenecks (`8.4–9.2m`: `classify` `1m 43s`, `ci (integration)` `6–7m`, `ci (publish)` `6m 23s`) once Task 504 (`wslc` + `green-key`) lands
- [ ] Item 6 (`timewarp-architecture`) — Fix implementer test-harness traps (`StaticHtmlRenderer` `OnAfterRenderAsync` helper/docs from `490`'s `18m` detour; combined Aspire test filter; `ActionCatalog_Should` / `StateJsonContext` `SYSLIB1031` guard)
- [ ] Item 7 (`timewarp-ganda`) — Fix `task work-set` friction: `open-pr` merge-commit PR titles (4/5 `489` PRs), standalone `task work --restart` checking `origin/master` instead of feature branch, and red-child ordering
- [ ] Item 8 (`timewarp-ganda`) — Fix `task-work.progress.log` `timing total` double-counting (`503` printed `1h06m` vs `54m38s` actual; `490` printed `2h36m` vs `1h56m` actual) and misleading composite `review` outlier alerts
- [ ] Hand off to `/tw-correct` (`timewarp-architecture` & `timewarp-flow`) — Repeated agent mistakes: (A) `dotnet test` from repo root / parallel `obj` collisions (14+ times across 4 tasks), (B) piping `dev build` / `ganda repo audit` into `head`/`tail` (5 times across 4 tasks), (C) un-baselined analyzer suppressions (2 tasks)

## Session

- Created: 2026-11-24 (follow-up to Task 500 after `ganda 393401fa`, `roslynk` uninstall, and Tasks 501/503)

## Notes

- Tasks **504** and **507** were still running (`alive`) at analysis time (`2026-11-24T04:07Z`) and were excluded per `tw-walk-retro` Step 0.
- Prior baseline: Task **500** (`kanban/to-do/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`).
