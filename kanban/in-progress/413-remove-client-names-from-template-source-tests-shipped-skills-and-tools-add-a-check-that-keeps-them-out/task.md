# Remove client names from template source, tests, shipped skills and tools; add a check that keeps them out

## Description

The template ships to every generated app, so it must not name clients. The pre-merge check
(`git diff origin/master...<branch> -- source tests | rg -i 'crunchit|copic'`) only looks at new
lines, so older names were never caught. On master at `fc8c0a7c1` (2026-10-25):

- `source/` and `tests/`: 55 matches. Most are design comments ("port of COPIC CopicSection",
  "COPIC rule", "crunchit 008-004"), plus test names like `..._Crunchit_Clients`.
- **A real client tenant in shipped files:**
  - `tests/tools/dev-cli-tests/entra-tenant-tests.cs` uses "CrunchIt, LLC", `crunchitfs.com`,
    `crunchitfs.onmicrosoft.com`, and what looks like the real tenant GUID.
  - `tests/tools/dev-cli-tests/entra-setup-tests.cs` uses "CrunchIt, LLC".
  - `source/container-apps/web/projects/web-spa/wwwroot/auth.md` has
    `dev entra setup --tenant crunchitfs.com`, `@crunchitfs.com`, and "crunchit on Azure Container Apps".
- Shipped skills: `skills/tw-blazor/SKILL.md` (about line 223, "port of COPIC `CopicSection`") and
  `skills/tw-blazor-css-strategy/SKILL.md` (about line 125, "quoted in crunchit `kanban/done/...`").
- `tools/`: 2 matches.

Find them all with:
`git grep -n -i -E 'crunchit|copic' -- source tests tools skills ':!skills/*/analysis/**'`

## Required outcome

- **Comments and docs:** reword so the rule stands on its own, with no source attribution. For
  example, "Compose Card for chrome" instead of "Port of COPIC CopicSection. Compose Card…", and
  "TimeWarp.State rule" instead of "COPIC rule". Keep the rule and drop the provenance. Repo
  history and `skills/*/analysis/` hold provenance (`skills-are-public-no-history`).
- **Test data:** use neutral examples. Use `Contoso, Ltd` / `contoso.com` /
  `contoso.onmicrosoft.com`, and a clearly made-up GUID such as
  `00000000-0000-0000-0000-000000000001`. Assertions keep the same shape.
- **Test names:** rename to say the behavior, not the client. For example,
  `Should_Emit_Additional_Static_Routes_With_Primary_Url` instead of `..._Crunchit_Clients`.
- **`wwwroot/auth.md`:** use `contoso.com` in the examples, and replace "crunchit on Azure
  Container Apps" with "for example, Azure Container Apps".
- **Out of scope:** `kanban/`, `documentation/`, and `skills/*/analysis/`. None of them is packed.
- **Check that keeps them out:** a gate that fails when a client name appears in packed content.
  - The list of names must not itself ship. Put it in a file the template pack excludes, and
    confirm the exclusion against `.template.config/template.json` and the
    `timewarp-templates` pack.
  - The gate runs in CI on every PR, for example a `dev` verb or a step in the `ci (fast)` job, and
    locally through `dev build` or `ganda repo audit`, whichever is less code.
  - It scans the packed set (`source/`, `tests/`, `tools/` if packed, `skills/` minus
    `analysis/`), not only the diff.
  - Recommendation: a `dev check-client-names` verb that reads a names file from the unpacked
    location, called from CI, with `tw-pr` updated to list it with the other gates. The
    implementer confirms nothing simpler exists (for example an existing audit check that takes
    a word list).

## Rework: plain search instead of pack-rule evaluation (Steve 2026-10-25)

PR #577's `tools/dev-cli/services/client-name-scan.cs` re-implements the template's MSBuild
pack rules (about 740 lines). Steve chose a plain search instead. Replace it:

- **Scan:** search the tracked files (`git grep -i -F -f documentation/developer/client-names.txt`
  or the same through Amuru) over the whole repo, excluding `documentation/`, `kanban/`, and
  `skills/*/analysis/` with git pathspecs. Report `path:line: matched <name>` and fail. Do not read
  `template.json` or the template csproj. Keep the service a few dozen lines.
- **Keep:** the `dev check-client-names` verb, the calls from `dev build` and `dev lint-skills`,
  the names file, and the `tw-pr` and `AGENTS.md` mentions (update any wording that describes
  the pack-rule scan).
- **Tests:** replace `client-name-scan-tests.cs` with a few tests against a temp git repo:
  clean tree passes, a planted name fails with path and line, and a name under an excluded
  folder passes. Do not write the string literal `"kanban` followed by `/`, `\` or `"` in any test
  source. `screenshot-path-guard-tests.cs` (`Not_Reference_Kanban_As_A_Path`) fails on it, which
  is what made PR #577's `ci (playwright)` red. Build excluded folder names some other way, or
  test the exclusion with `documentation/` only.
- **Gates:** `dev build` 0/0, dev-cli tests green, the web-spa-playwright-tests unit test
  `Not_Reference_Kanban_As_A_Path` green, and `dev check-client-names` passes on the branch and
  fails on a planted name.

## Goal (implementer restatement)

Shipped template files must not name a client. That cleanup is already on this branch:
comments state the rule without a source credit, sample Entra data is Contoso with a
made-up tenant id, and `wwwroot/auth.md` uses `contoso.com`. History under `kanban/`,
`documentation/`, and `skills/*/analysis/` stays, because those trees are not the scan set.

The remaining work is the gate. `dev check-client-names` reads
`documentation/developer/client-names.txt` and searches tracked files with
`git grep -i -F`, the same search `dev build` and `dev lint-skills` already call. It does
not read `template.json` or the template project. Git pathspecs leave out `documentation/`,
the kanban tree, and `skills/*/analysis/`. A hit prints `path:line: matched <name>` and
fails the command. The name list stays under `documentation/`, so the pack does not ship
it and the search does not treat the list as a hit. `ganda repo audit` has no word-list
check. `AGENTS.md` is where `tw-pr` tells agents to read this repo's gates, and that
sentence describes this search.

Done means a temp git repo with a clean tree passes, a planted name fails with path and
line, and the same name under an excluded folder passes. Test source does not contain a
`"kanban"` path literal. `dev build` is 0/0, the dev-cli tests pass, the playwright guard
`Not_Reference_Kanban_As_A_Path` passes, and `dev check-client-names` passes on this branch
and fails when a name is planted in a tracked file.

## Checklist

- [x] All matches in `source/` and `tests/` reworded or replaced
- [x] Real tenant name, domain and GUID replaced with Contoso examples and a made-up GUID
- [x] `wwwroot/auth.md` examples neutral
- [x] `skills/tw-blazor/SKILL.md` and `skills/tw-blazor-css-strategy/SKILL.md` cleaned
- [x] `tools/` matches cleaned
- [x] Gate added (names list unpacked, runs in CI on PRs, scans tracked files); `tw-pr` reads it from `AGENTS.md`
- [x] `git grep -n -i -E 'crunchit|copic' -- source tests tools skills ':!skills/*/analysis/**'` is empty
- [x] `dev build` 0/0, dev-cli tests green, `Not_Reference_Kanban_As_A_Path` green, `dev check-client-names` passes here and fails on a planted name
- [x] Case-variant Entra tests use a GUID with hex letters and keep the first casing
- [x] Scan is `git grep -i -F` over tracked files, excluding `documentation/`, the kanban tree, and `skills/*/analysis/`; it does not read the template pack files

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-25)
- Implementation: 01a170b0-8b68-7c61-9c79-7b9fa8da6a91 (2026-10-25). Reworded packed comments, replaced the sample tenant with Contoso and `00000000-0000-0000-0000-000000000001`, and added `dev check-client-names`. `ganda repo audit` exited 0. The `kanban-landed` warning is the same advisory as task 410 (eight other live claims have no worktree) and was left alone.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-25T02:08:21Z
- Review round 1 (2026-10-25): review oracle (claude-opus-5-5), effort 3, roster general. Wrote `review/round-1/general.md` and `review/round-1/merged.md`. 2 open findings (M1 bug, M2 suggestion), so a fix pass is needed.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-25T02:11:54Z
- Fix pass (2026-10-25): implement-fix-2. The two Entra case tests use `aaaaaaaa-0000-0000-0000-000000000001` against its upper-case form, and the union keeps the first casing. `dev check-client-names` lists files from the pack allow-list (`Content`, and `None` with `Pack=true`) minus each item's `Exclude` and `skills/**/analysis/**`. An MSBuild property in an include or exclude fails the scan. `tests/tools/dev-cli-tests` Release: 353 passed. `dotnet run tools/dev-cli/dev.cs -- check-client-names` printed `Client-name scan passed.` Full solution build, template-smoke, and lint-skills were not re-run; the pack contents are unchanged and the scan verb is what `dev build` and `dev lint-skills` call first.
- Review round 2 (2026-10-25): review oracle (claude-opus-5-5), effort 2, roster general, scope fix delta `f384c75b7..4ccd816aa`. Wrote `review/round-2/general.md`, `review/round-2/merged.md`, and `review/disposition.md`. M1 and M2 are fixed, nothing new was raised, and the disposition is clean.
- Plain-search rework (2026-10-25): Steve rejected the pack-rule scanner. `ClientNameScan` is now `git grep -i -F` over tracked files. Pathspecs skip `documentation/`, the kanban tree, and `skills/*/analysis/`. The service does not read the template pack files. `ganda repo audit` exited 0. The `kanban-landed` warning is the same advisory as task 410 (eight other live claims have no worktree) and was left alone.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-25T09:38:43Z

## Results

Packed comments and docs now state the rule without naming a client. Sample Entra data is Contoso (`Contoso, Ltd`, `contoso.com`, `contoso.onmicrosoft.com`) and the made-up tenant id `00000000-0000-0000-0000-000000000001`. Test names describe the behavior. `wwwroot/auth.md` uses `contoso.com` and "for example, Azure Container Apps".

`dev check-client-names` reads `documentation/developer/client-names.txt` and searches tracked files with `git grep -n -z -i -I -F`. Comment lines and blank lines in that file are not patterns. Git pathspecs skip `documentation/`, the kanban tree, and `skills/*/analysis/`. A hit prints `path:line: matched <name>` and the command exits 1. The service does not read `template.json` or the template project. The name list stays under `documentation/`, so the search does not treat it as a hit and the pack does not ship it. `ganda repo audit` has no word-list check, so the gate is this verb. `dev build` runs it (CI `ci (fast)` calls that build through `dotnet run tools/dev-cli/dev.cs -- workflow`). `dev lint-skills` runs it so a skills-only PR is covered. `AGENTS.md` describes this search where `tw-pr` tells agents to read this repo's gates. `timewarp-flow`'s `tw-pr` skill was not edited.

The earlier pack-rule scanner is gone. Round 2's allow-list file set is not what the verb scans.

CI and this validation used `dotnet run tools/dev-cli/dev.cs`, which is what `workflow.yml` runs. `./bin/dev` is the gitignored AOT cache and does not gain the verb until `dev self-install`.

### How to validate

Smoke:

```bash
git grep -n -i -E 'crunchit|copic' -- source tests tools skills ':!skills/*/analysis/**'
dotnet run tools/dev-cli/dev.cs -- check-client-names
```

Expect: the search prints nothing. The verb prints `Client-name scan passed.` and exits 0. A planted token in a tracked file outside the excluded trees (this walk appended one to `readme.md`) prints `readme.md:54: matched copic` and exits 1. The same token under `documentation/`, `skills/*/analysis/`, or the kanban tree does not. Those cases are `ScanAsync_Given_` in `tests/tools/dev-cli-tests/client-name-scan-tests.cs` (fixture token `acmeclient`). The board-column directory name in that file is built from characters so the screenshot path guard stays green. `Union_Should_DedupeCaseInsensitivePreserveFirstCasing` expects the lower-case id `aaaaaaaa-0000-0000-0000-000000000001` when the second list has the upper-case form. `MatchById_DifferentGuidCasing_Should_Select` selects the tenant whose id is the lower-case form.

Rework gates:

- `cd tests/tools/dev-cli-tests && dotnet test -c Release` — 342 passed, 0 failed. `--filter-class ScanAsync_Given_` — 4 passed.
- `cd tests/container-apps/web/web-spa-playwright-tests && dotnet test -c Release -- --filter-method Not_Reference_Kanban_As_A_Path` — 1 passed.
- `dotnet run tools/dev-cli/dev.cs -- check-client-names` — `Client-name scan passed.`, exit 0.
- Planted `copic` on `readme.md`, then restored the file — `readme.md:54: matched copic`, exit 1. `git diff` on that file was empty afterward.
- `dotnet run tools/dev-cli/dev.cs -- build` — Build succeeded, 0 Warning(s), 0 Error(s), and the scan passed first.
- `ganda repo audit` — exit 0 (33 passed, 1 skipped, `kanban-landed` advisory only: the same eight other live claims).

`dev lint-skills` and `dev template-smoke` were not re-run. The skill lint calls this scan first, and the template content of the earlier smoke was not part of this rework.

### Review

Two rounds, roster general. Round 1 ran at effort 3 and round 2 at effort 2. Round 1 found 1 bug and 1 suggestion, and fix pass `70a5819ed` addressed both. Round 2 re-verified them on the fix delta and found nothing new. Round 2 re-ran `dev check-client-names` (exit 0) and the dev-cli tests (353/353).

Final counts: bug 1 fixed, suggestion 1 fixed, nit 0. Nothing is open and nothing is wontfix. **Disposition: clean.**

- M1 (bug): `Union_Should_DedupeCaseInsensitivePreserveFirstCasing` and `MatchById_DifferentGuidCasing_Should_Select` again use a GUID with hex letters. The union keeps the first casing.
- M2 (suggestion): the scan file set was the pack allow-list at round 2. The later plain-search rework replaced that set with `git grep` over tracked files. `PackedFiles_Given_` is gone.

Ledger: `review/round-2/merged.md` (round 1: `review/round-1/merged.md`). Framework: `review/review-framework.md`. Disposition: `review/disposition.md`.

## Review

- Round 1: fixed on this branch — M1, M2
- Round 2: Done
## Notes

- Found while verifying PR #576 (task 410). Steve 2026-10-25: "yes file the clean up task".
