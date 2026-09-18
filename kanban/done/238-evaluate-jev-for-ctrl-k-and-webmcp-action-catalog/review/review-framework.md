# Review framework — task 238

**Date:** 2026-09-18
**Host task:** kanban/in-progress/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/
**Diff scope:** branch `task/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog` vs `origin/master` (commit `dd8f3260`)
**Plan / brief:** Evaluate TypeSafe Jev as the ranker for a shared Ctrl-K / WebMCP ActionSet catalog. Research kitchen only — no palette UI, no production `JevOracleProvider`, no TimeWarp SDK. Recommendation recorded: **defer**. Live Choice + Noul blocked on missing keys.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok session `01a0b3d4-baec-78e1-a408-66cb39eccafa` (2026-09-18); implementer grok session `01a0b3c7-6d34-7220-9f1e-b8fe9459db1a` (2026-09-18)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Brief for general

This is a **research / recommendation** task, not a product-code change. `source/` must stay untouched. Review the kitchen under `research/` plus `task.md` Results against the Requirements.

Verify:

1. **Catalog decision** — new opt-in (`CatalogAction` / `CatalogTool`), not `[TrackAction]` / `[TrackEvent]`. Fields `name` / `description` / `inputSchema` / `execute` / `visibility`. Description SSOT is Purpose first sentence or a small attribute — not Jev, not hollow XML (177).
2. **Repo split** — catalog shape + source-gen → timewarp-state (child only if adopt); Ctrl-K UI + `navigator.modelContext.registerTool` → this repo. One shared roster. 104-019 `tools[]` stays empty.
3. **Ranking experiment** — labeled corpus (~30 phrases including “just looking” negatives); C# shortlist then Jev Choice + Noul request shapes; dry-run default; `--live` requires a key and exits 2 when missing. Confidence log-only; auto-dispatch vs show-shortlist counted separately. Choice max 255; spike shortlists 8.
4. **Recommendation** — adopt / defer / reject is explicit. If defer/reject: no children minted, GitHub **#102** not closed from this id.
5. **MCA §2.3(f)** — no published live latency/quality/cost numbers; `live-results.json` gitignored; dry-run scores are C# shortlist, not TypeSafe measurements.
6. **Out of scope** — no palette UI, no production provider under `source/`, no TimeWarp TypeSafe SDK, no ActionSet generator change.

Falsifiable claims to re-verify:

- `dotnet run run-rank-experiment.cs` from `research/` → exit 0, Items 34, shortlist hit 25/25
- `--live` with keys unset → exit 2, names BLOCKER
- `catalog.json` has 20 items; `corpus.json` has 34 (25 command, 9 negative)
- `TimeWarpPage` search field remains decorative (no `@bind-Value`, no hotkey)
- No new children under `kanban/` from this branch besides 238 itself
- GitHub #102 still open (empty 2021 issue)
- No `source/` product files in the diff

Finding template: `review/round-1/general.md` (see tw-implementation-review).
