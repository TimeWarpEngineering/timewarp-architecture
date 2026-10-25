# Round 1 — general
**Date:** 2026-10-25
**Scope reviewed:** branch vs `a90e284c4` (commit `4a0af0ed9`), kanban/ excluded

## Summary

The change rewords client attributions in packed comments, docs, and two shipped skills. It
replaces the real Entra sample tenant with Contoso and `00000000-0000-0000-0000-000000000001`,
renames the three page-generator tests, and adds `dev check-client-names`. `dev build` and
`dev lint-skills` both run the same scan first. The wiring is correct: `dev workflow` (ci fast)
builds through `BuildCommand.Handler`, and `RunStepAsync` fails on the `Environment.ExitCode`
the scan sets. `skill-lint` calls `dev lint-skills` when skills change. The name list is under
`documentation/`, which is an unconditional exclude in `template.json` and is not in the pack
allow-list, and the scan checks both. I re-ran the checks: the acceptance `git grep` is empty,
the old tenant GUID `30f3971f…` is gone from every non-kanban path, and
`dotnet run tools/dev-cli/dev.cs -- check-client-names` exits 0.

Two problems. Moving to an all-digit GUID made two case-insensitivity tests vacuous, and the
scan's hardcoded root list skips several packed trees.

Goal restatement vs task: no misread. The restatement puts the gate in `AGENTS.md`, not in
`tw-pr`. That matches the task, because `tw-pr` (line 106) tells agents to read the repo's
`AGENTS.md` for the real gates.

## Issues

### Issue 1 — Severity: bug
- File: tests/tools/dev-cli-tests/entra-tenant-tests.cs:96 (also :148)
- Description: The task says "Assertions keep the same shape", but two tests lost what they
  measure. `Union_Should_DedupeCaseInsensitivePreserveFirstCasing` used to pass the lower-case id
  in the first list and `30F3971F-…` (upper case) in the second, so it checked the
  case-insensitive dedupe and that the first casing is kept. `SelectTenant_Given_.MatchById_DifferentGuidCasing_Should_Select`
  used to look up `30F3971F-…` against a lower-case tenant id. Both now use
  `00000000-0000-0000-0000-000000000001`, which has no hex letters, so the upper-case and
  lower-case strings are byte-identical. Both tests would still pass if `CollectTenantIds` used an
  ordinal `HashSet` or `SelectTenant` compared ids ordinally. The test names still promise
  case-insensitivity.
- Suggestion: Use a made-up GUID that has hex letters for the case variants, for example
  `aaaaaaaa-0000-0000-0000-000000000001` in the tenant and `AAAAAAAA-0000-0000-0000-000000000001`
  in the lookup and second list. Keep the first-casing assertion on the lower-case form.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/services/client-name-scan.cs:103
- Description: Scan roots are hardcoded to `source`, `tests`, `skills` (plus `tools` behind a
  pack probe). The pack allow-list in
  `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`
  also ships `msbuild/**`, `.template.config/**`, `kanban/overview.md`, `kanban/task-template.md`,
  the root build and solution files (`Directory.*.props/targets`, `timewarp-architecture.slnx`,
  `global.json`, `BannedSymbols.txt`, `aspire.config.json`, `.editorconfig`, `.gitattributes`,
  `.gitignore`, `unlicense.md`), and `readme.md` as the package readme. None of these is scanned.
  Today they are clean (I checked with `git grep`). But a client name added to
  `msbuild/*.props` or `.template.config/template.json` would ship and pass the gate, even though
  `AGENTS.md` now says the verb covers "every change to packed content". The scan already parses
  the csproj to decide on `tools/`. Keeping a separate root list that has to agree with the
  allow-list is what the AGENTS.md hard rule "When two things must agree, generate one from the
  other or add a build check" warns against.
- Suggestion: Build the scan set from the csproj `Content`/`None Pack=true` items, which
  `PackIncludesRepoPath` already partly parses: walk each include's base directory or explicit
  file, and apply that item's `Exclude` plus `skills/**/analysis/**`. If that is too much, add
  `msbuild`, `.template.config`, the explicit root files, the kanban skeleton, and `readme.md`,
  plus a test that fails when the allow-list has an `Include` the scan does not cover.
- Status: open
