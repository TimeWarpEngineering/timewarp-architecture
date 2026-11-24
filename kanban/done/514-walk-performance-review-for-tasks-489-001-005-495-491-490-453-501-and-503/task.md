# Walk performance review for tasks 489-001..005, 495, 491, 490, 453, 501, and 503

## Description

Follow-up cross-walk performance analysis covering the **11 finished walks** in `timewarp-architecture` since Task 500 (**489-001..005** work-set, overnight tasks **495**, **491**, **490**, **453**, and post-update morning tasks **501**, **503**), measuring the impact of yesterday's changes (`roslynk` uninstall, Cloudflare MCP removal, Task 501 Playwright CI disable, and `ganda 1.0.0-beta.38+393401fa`) and surfacing remaining workflow bottlenecks.

Full report with measured SigNoz OTEL data, before/after comparison, per-walk timelines, and standalone per-item context lives in `report/analysis.md`.

Use this task to walk through each finding one by one and decide which follow-up tasks to file or route to `/tw-correct`.

## Checklist

- [x] Collect SigNoz OTEL traces, walk logs, agent transcripts, PR CI checks, and `ganda usage stats` for all 11 finished walks (`report/analysis.md`)
- [x] Item 1 (`dotfiles` task **012**) — Root-caused: not a `ganda 393401fa` or profile bug; interactive Claude `Bash` strips `OTEL_*` and `BASH_ENV` was unset when launching `setsid -f bash -c ... ganda task work`. Filed `dotfiles` task **012** to export `BASH_ENV="$HOME/.bashrc"` in `bash/.bashrc`
- [x] Item 2 (`timewarp-ganda`) — Confirmed `harness-grok.json` is already on `reasoningEffort: "high"` (not `xhigh`); leaving Grok 4.7 as-is for now while waiting for 4.8
- [x] Item 3 (`timewarp-ganda` task **704**) — Already tracked in `timewarp-ganda` task **704**; amended **704** with `495`/`498` evidence, opt-in-only exclusion for small/testing pools (`opencode` Go, future `agy`), and Cursor Ultra (`$200/mo`) in `usage-plans.jsonc`
- [x] Item 4 (`timewarp-ganda` task **716**) — Filed `timewarp-ganda` task **716** covering all four pieces: walk `audit`+`merge` dedupe/attest reuse, `DevBinaryAttestGate` content-hash skip for `post-commit` `self-install`, `package-pin-currency` short-TTL cache, and `GANDA_ATTEST_HOOK=0` WIP policy (revisit paths after `706-*` refactor)
- [x] Item 5 (`timewarp-architecture`) — Re-measured on merged PR #670 (Task 504 `wslc` + `green-key`): master merge CI dropped `9m 27s → 1m 26s` (`green-key` skip), PR CI dropped `8m 41s → 7m 36s` (`ci (integration)` on `ubuntu-latest` is now the `5m 55s` critical path); closing for now
- [x] Item 6 (`timewarp-architecture` task **515**) — Filed task **515** to extract a shared `InteractiveHtmlRenderer` + `InvokeAfterRenderAsync` test helper in `web-spa-integration-tests` and document combined MTP test-filter usage (skipping `StateJsonContext` since `489-003` already added `TypeInfoPropertyName` + comment)
- [x] Item 7 (`timewarp-ganda` task **717**) — Filed `timewarp-ganda` task **717** to skip merge-commit subjects when `open-pr` picks a PR title and teach `task work --restart` to honor the branch's recorded `--into` feature base when checking predecessors
- [x] Item 8 (`timewarp-ganda` task **718**) — Filed `timewarp-ganda` task **718** to exclude nested review substeps from `timing total`, keep `XmYs` precision on step lines, and separate pure `review` from `implement-fix` in `WalkRetroSuggestion`
- [x] Hand off to `/tw-correct` — (A) Filed `timewarp-architecture` task **516** (`--project` + `--filter-class`/`--filter-method` on `dev test`), (B) Filed `timewarp-architecture` task **517** (`dev build` quiet by default with `--verbose`; `ganda repo audit` already covered by `timewarp-ganda` task **683**), (C) Skipped (`ganda repo audit`'s `AnalyzerSuppressionCheck` already enforces `.timewarp/suppression-baseline.json`)

## Session

- Created: 2026-11-24 (follow-up to Task 500 after `ganda 393401fa`, `roslynk` uninstall, and Tasks 501/503)

## Notes

- Tasks **504** and **507** were still running (`alive`) at analysis time (`2026-11-24T04:07Z`) and were excluded per `tw-walk-retro` Step 0.
- Prior baseline: Task **500** (`kanban/to-do/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`).
