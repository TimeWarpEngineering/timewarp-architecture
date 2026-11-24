# Walk performance review for tasks 499, 483, 494, and 498

## Description

Cross-walk performance analysis of the last four finished `ganda task work` walks in `timewarp-architecture` (**499**, **483**, **494**, **498**) using SigNoz OTEL spans, walk evidence logs, agent transcripts, PR CI check timings, and `ganda usage stats`.

Full report with measured data, SigNoz trace IDs, `ganda 1.0.0-beta.38` (Task 707) verification, and per-item context lives in `report/analysis.md`.

Use this task to walk through each finding one by one and decide which follow-up tasks to file (or close).

## Checklist

- [x] Collect SigNoz OTEL traces, walk logs, transcripts, and PR CI timings for tasks 499, 483, 494, and 498 (`report/analysis.md`)
- [x] Item 3 — Mitigate `roslynk` background `dotnet restore` storm and unauthenticated Cloudflare MCP servers (uninstalled `roslynk 3.0.0` and disabled Cloudflare MCPs on 2026-11-23)
- [x] Item 2 (`timewarp-architecture`) — Filed tasks **501** (disable Playwright in CI), **502** (research Playwright value), **503** (thin-trigger `workflow.yml` + `dev workflow` refactor, depends on 501), and **504** (`wslc` runners + `green-key` skip, depends on 503)
- [ ] Item 1A (`timewarp-ganda`) — Decide & file task: set `GANDA_ATTEST_HOOK=0` in `home fast-forward` (`39–52s` per fast-forward, still live on `beta.38`)
- [ ] Item 1B (`timewarp-ganda`) — Decide & file task: attest in `audit` node for Task 707 reuse + short-TTL cache `package-pin-currency` (still live on `beta.38`)
- [ ] Item 1C (`timewarp-ganda` / oracle brief) — Decide & file task: skip `post-commit` attest hook on intermediate WIP commits
- [ ] Item 5 (`timewarp-architecture`) — Decide Option A (keep `IngressSmoke` enabled + fast-fail `SetupOnce` & check `~/.aspire/bundle`) vs Option B (disable `IngressSmoke` + research task)
- [ ] Item 4A (`timewarp-ganda`) — Decide & file task: master-audit pre-flight before launching `implement`
- [ ] Item 4B (`timewarp-ganda`) — Decide & file task: provider 429 quota cooldown file
- [ ] Item 6A (`timewarp-ganda`) — Decide & file task: investigate `20s` pre-walk startup gap & coalesce `ganda.claims.fetch` (still live on `beta.38`)
- [ ] Item 6B (`timewarp-ganda`) — Decide & file task: emit merge-tail OTEL spans (`merge`/`kanban-done`/`land-done`/`gc`) & fix `timing total` log math (still live on `beta.38`)
- [ ] Item 7 (`timewarp-flow`) — Hand off to `/tw-correct`: agents piping `dev build` / `ganda repo audit` into `tail`/`grep` without saving output
- [ ] Item 8 (`timewarp-ganda`) — Review minor items (Grok review routing/effort on small diffs, stale PR titles on resumed/merged branches, transitive major bumps)

## Session

- Created: 3905211 (2026-11-23)
- 2026-11-23: Wrote full cross-walk OTEL and transcript analysis to `report/analysis.md`.

## Notes

- Task 489-001 was still running (`alive`) during analysis and was excluded per `tw-walk-retro` Step 0.
- `ganda` `1.0.0-beta.38+c8f6a9ca` (Task 707) was installed at `2026-11-23T06:41:34Z`, before Task 499 ran (`08:31:55Z`), so findings observed on 499 are verified live on the current binary.
