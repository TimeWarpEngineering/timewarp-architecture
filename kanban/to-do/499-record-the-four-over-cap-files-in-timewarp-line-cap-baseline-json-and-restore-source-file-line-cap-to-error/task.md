# Record the four over-cap files in .timewarp/line-cap-baseline.json and restore source-file-line-cap to error

## Description

[Brief description of the task]

## Checklist

- [ ] Item 1
- [ ] Item 2

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-22)

## Notes

[Additional context]

## Description

`ganda repo audit` on master reports `source-file-line-cap` as a Warning with four files over
500 lines: three EF Core migration Designer/snapshot files under
`source/container-apps/web/platform/postgres/migrations/` and the done-task research runfile
`kanban/done/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/research/run-rank-experiment.cs`.
Task 469-001 downgraded the check to `warning` in `.editorconfig` because the ganda build at
the time had no per-file exemption. ganda master now has one: the ratchet file
`.timewarp/line-cap-baseline.json` maps a repo-relative path to its recorded line count, and a
recorded file passes while it is at or under that count. See ganda
`documentation/developer/standards/source-file-line-cap.md`. The baseline can only shrink once
origin/master carries it, so the first commit records all four and later commits may only drop
entries or lower counts.

Steve, 2026-11-22: the EF migrations belong in the baseline, and so does the research runfile
since it lives in a kitchen.

## Precondition

The installed ganda must be a build that reads `.timewarp/line-cap-baseline.json`. The
beta.38 binary installed 2026-11-21 13:52 predates that feature (its failure text has no
baseline wording). Verify with `ganda repo audit` after the baseline exists: the check must
pass, not report the four files. If it still reports them, stop and report that ganda needs
updating; do not work around it.

## Checklist

- [ ] Add `.timewarp/line-cap-baseline.json` recording the four paths at their current line
      counts (wc -l at the time of the commit; the check counts logical lines, so confirm the
      recorded count is >= the audit's reported count).
- [ ] In `.editorconfig` under `[ganda.audit]`, remove `source-file-line-cap.severity = warning`
      and its comment so the check runs at its default Error severity.
- [ ] `ganda repo audit` exits 0 with `source-file-line-cap` PASS on the branch.
- [ ] Temporarily grow one recorded file by a line and confirm the check fails, then revert.
      Record that proof in `## Results`.
- [ ] Retire `documentation/developer/guides/analyzer-package-split.md` (Steve, 2026-11-22:
      "retire it"). documentation/ is retired; live rules live in a Design region or skill,
      history lives in the kitchen (done task 486 holds the split record). Steps:
      - Move the live rule into the header of
        `source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md`,
        replacing its `Spec: documentation/...` line: the split test (a rule that checks an
        Architecture concept such as contracts, endpoints, aggregates, slices, Aspire, or the
        template stays here; a rule that checks only C# file or name shape belongs in
        TimeWarp.SourceGenerators), the next free TWA id, and "history: task 486". Drop the
        stale State sentence there too: State no longer references any TimeWarp.Architecture
        package (state task 153, 2026-11-10); do not list consumers.
      - Repoint the AGENTS.md "Diagnostics TWA / TWE / SG" row from the guide to that header.
      - Delete the guide. `grep -rn analyzer-package-split --exclude-dir=kanban .` returns nothing.
- [ ] No product code changes. Template pack is unaffected (`.timewarp/` is not template
      content; confirm `.template.config` does not include it or add an exclude if it does).

## Depends on

Updated ganda on the implementer's machine (see Precondition).
