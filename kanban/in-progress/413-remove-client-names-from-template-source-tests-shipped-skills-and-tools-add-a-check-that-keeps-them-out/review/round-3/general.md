# Round 3 — general
**Date:** 2026-10-25
**Scope reviewed:** plain-search rework, `f04972a83..973480be5` (product commit `4fa6334cb`), kanban/ excluded; plus a whole-branch re-check of the cleanup (`git grep` for the names over the scan set).

## Summary

The rework replaces the pack-rule scanner with `git grep -n -z -i -I -F -f <temp pattern file>`
over tracked files, with pathspecs `:!documentation`, `:!kanban`, and `:!skills/*/analysis/**`.
The service no longer reads `template.json` or the template project and is about 200 lines
including regions (down from ~740). `dev build`, `dev lint-skills`, and `dev check-client-names`
share `ReportAsync`; the CI `workflow` verb runs `BuildCommand` and stops on
`Environment.ExitCode != 0`, so a hit fails `ci (fast)`. The four temp-repo tests cover clean,
planted (with path, line, and list casing), excluded folders (documentation, analysis, and the
board tree built from chars), and comment lines in the list. No test source has a quoted
board-column path literal. The implementer restatement matches the task and the rework section.

Verified in this round:

- `git grep -n -z` output is `path\0line\0text\n` (checked with `od -c`), which is what `ParseHits` splits on.
- `dotnet run tools/dev-cli/dev.cs -- check-client-names` — `Client-name scan passed.`, exit 0.
- `git grep -n -i -E 'crunchit|copic' -- . ':!documentation' ':!kanban' ':!skills/*/analysis/**'` — empty.
- `tests/tools/dev-cli-tests` `dotnet test -c Release` — 342 passed, 0 failed.
- `web-spa-playwright-tests` `--filter-method Not_Reference_Kanban_As_A_Path` — 1 passed.
- `dotnet run tools/dev-cli/dev.cs -- build --quiet` — scan passed, build succeeded, exit 0.

A planted-name run on the real tree was not repeated here (the product tree is read-only for
this oracle); `PlantedName_Should_ReportPathAndLine` covers it against a temp repo.

## Issues

None.
