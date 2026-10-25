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

## Goal (implementer restatement)

Packed template files are copied into every generated app, so they must not name a client.
Comments and docs keep the rule and drop the source credit. Sample Entra data uses Contoso
(`Contoso, Ltd`, `contoso.com`, `contoso.onmicrosoft.com`) and a made-up tenant id.
`wwwroot/auth.md` uses `contoso.com`. Task history under `kanban/`, `documentation/`, and
`skills/*/analysis/` stay as they are, because the pack does not ship those trees.

`dev check-client-names` is the gate. It reads `documentation/developer/client-names.txt`,
which both `.template.config/template.json` and the `timewarp-templates` pack leave out.
`ganda repo audit` has no word-list check. `dev build` runs the scan, and that is what
CI's `ci (fast)` job calls. `dev lint-skills` runs it for a skills-only PR. `AGENTS.md`
names the verb where `tw-pr` tells agents to read this repo's gates.

The scan file set is the pack allow-list (`Content`, and `None` with `Pack=true`), minus
each item's `Exclude`, and also minus `skills/**/analysis/**`. That set includes `msbuild/`,
`.template.config/`, the root build and solution files, the kanban skeleton, and `readme.md`.
A new allow-list include is scanned without a second hardcoded root list. An include or
exclude that uses an MSBuild property fails the scan. The two case-variant Entra tests use
a made-up GUID whose upper and lower forms differ (`aaaaaaaa-0000-0000-0000-000000000001`
and `AAAAAAAA-0000-0000-0000-000000000001`). The union assertion keeps the first casing.

Done means the denylist search of packed content is empty, the gate fails on a planted
name, those previously skipped packed paths are in the scan set, the case-variant tests
would fail an ordinal compare, and build, tests, template smoke, skill lint, and
`ganda repo audit` pass.

## Checklist

- [x] All matches in `source/` and `tests/` reworded or replaced
- [x] Real tenant name, domain and GUID replaced with Contoso examples and a made-up GUID
- [x] `wwwroot/auth.md` examples neutral
- [x] `skills/tw-blazor/SKILL.md` and `skills/tw-blazor-css-strategy/SKILL.md` cleaned
- [x] `tools/` matches cleaned
- [x] Gate added (names list unpacked, runs in CI on PRs, scans packed content); `tw-pr` lists it
- [x] `git grep -n -i -E 'crunchit|copic' -- source tests tools skills ':!skills/*/analysis/**'` is empty
- [x] `./bin/dev build` 0/0, tests pass, `./bin/dev template-smoke`, `./bin/dev lint-skills`, `ganda repo audit`
- [x] Case-variant Entra tests use a GUID with hex letters and keep the first casing
- [x] Scan set is the pack allow-list, including msbuild, .template.config, root files, the kanban skeleton, and readme.md
- [x] A test lists a new allow-list include and leaves an unpacked file out

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-25)
- Implementation: 01a170b0-8b68-7c61-9c79-7b9fa8da6a91 (2026-10-25). Reworded packed comments, replaced the sample tenant with Contoso and `00000000-0000-0000-0000-000000000001`, and added `dev check-client-names`. `ganda repo audit` exited 0. The `kanban-landed` warning is the same advisory as task 410 (eight other live claims have no worktree) and was left alone.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-25T02:08:21Z
- Review round 1 (2026-10-25): review oracle (claude-opus-5-5), effort 3, roster general. Wrote `review/round-1/general.md` and `review/round-1/merged.md`. 2 open findings (M1 bug, M2 suggestion), so a fix pass is needed.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-25T02:11:54Z
- Fix pass (2026-10-25): implement-fix-2. The two Entra case tests use `aaaaaaaa-0000-0000-0000-000000000001` against its upper-case form, and the union keeps the first casing. `dev check-client-names` lists files from the pack allow-list (`Content`, and `None` with `Pack=true`) minus each item's `Exclude` and `skills/**/analysis/**`. An MSBuild property in an include or exclude fails the scan. `tests/tools/dev-cli-tests` Release: 353 passed. `dotnet run tools/dev-cli/dev.cs -- check-client-names` printed `Client-name scan passed.` Full solution build, template-smoke, and lint-skills were not re-run; the pack contents are unchanged and the scan verb is what `dev build` and `dev lint-skills` call first.

## Results

Packed comments and docs now state the rule without naming a client. Sample Entra data is Contoso (`Contoso, Ltd`, `contoso.com`, `contoso.onmicrosoft.com`) and the made-up tenant id `00000000-0000-0000-0000-000000000001`. Test names describe the behavior. `wwwroot/auth.md` uses `contoso.com` and "for example, Azure Container Apps".

`dev check-client-names` reads `documentation/developer/client-names.txt`. The file set is the `timewarp-templates` allow-list: `Content` items, and `None` items with `Pack=true`, minus each item's `Exclude`. `skills/**/analysis/**`, `bin`, `obj`, `artifacts`, and `node_modules` are skipped as well. That set includes `msbuild/`, `.template.config/`, the root build and solution files, the kanban skeleton (column `.gitkeep` files, `overview.md`, `task-template.md`), and `readme.md`. `tools/` is included when an allow-list item covers it. This repo's allow-list does not, so `tools/` stays out of the packed scan. An include or exclude that uses an MSBuild property fails the scan. The name list stays unpacked: `documentation/**` is an unconditional exclude, and the pack does not include it. The scan fails if that exclude disappears or the pack starts including the file. `ganda repo audit --list-checks` has no word-list check, so the gate is this verb. `dev build` runs it (CI `ci (fast)` calls that build through `dotnet run tools/dev-cli/dev.cs -- workflow`). `dev lint-skills` runs it so a skills-only PR is covered. `AGENTS.md` lists the verb where `tw-pr` tells agents to read this repo's gates. `timewarp-flow`'s `tw-pr` skill was not edited.

The packed nupkg `TimeWarp.Architecture.2.0.0-beta.25` from template-smoke contains neither the name list nor those client tokens, and it does not contain `tools/dev-cli/dev.cs`.

CI and this validation used `dotnet run tools/dev-cli/dev.cs`, which is what `workflow.yml` runs. `./bin/dev` is the gitignored AOT cache and does not gain the verb until `dev self-install`.

### How to validate

Smoke:

```bash
git grep -n -i -E 'crunchit|copic' -- source tests tools skills ':!skills/*/analysis/**'
dotnet run tools/dev-cli/dev.cs -- check-client-names
```

Expect: the search prints nothing. The verb prints `Client-name scan passed.` and exits 0. A planted token in an allow-listed file (for example `msbuild/` or `readme.md`) fails the scan. The same token under `skills/*/analysis/`, in a `None` item without `Pack=true`, or in `kanban/done/*.md` does not. Those cases are `ClientNameScan_` in `tests/tools/dev-cli-tests` (fixture token `acmeclient`, so the denylist words stay out of packed tests). `Union_Should_DedupeCaseInsensitivePreserveFirstCasing` expects the lower-case id `aaaaaaaa-0000-0000-0000-000000000001` when the second list has the upper-case form. `MatchById_DifferentGuidCasing_Should_Select` selects the tenant whose id is the lower-case form.

Fix-pass gates:

- `cd tests/tools/dev-cli-tests && dotnet test -c Release` — 353 passed, 0 failed.
- `dotnet run tools/dev-cli/dev.cs -- check-client-names` — `Client-name scan passed.`, exit 0.
- `ganda repo audit --fix` landed kitchen 413, then the audit exited 0. The only `kanban-landed` row is the same advisory as task 410: eight other live claims have no worktree.

Earlier walk (still the pack-content evidence; not re-run on this fix pass):

- `dotnet run tools/dev-cli/dev.cs -- build` — Build succeeded, 0 Warning(s), 0 Error(s), and the scan passed first.
- `dotnet run tools/dev-cli/dev.cs -- test --no-build --group fast` — 14/14 projects passed.
- `dotnet run tools/dev-cli/dev.cs -- test --no-build --group integration` — 8/8 projects passed (includes the Contoso tenant tests and web-jaribu co-located tests).
- `dotnet run tools/dev-cli/dev.cs -- test --no-build --group playwright` — 1/1 passed (27/27).
- `dotnet run tools/dev-cli/dev.cs -- template-smoke` — Template smoke SUCCEEDED (SmokeDefault build 0/0).
- `dotnet run tools/dev-cli/dev.cs -- lint-skills` — 9/9 skills, eval.yaml valid, scan passed first.
- `ganda repo audit` — exit 0 (33 passed, 1 skipped, `kanban-landed` advisory only).

### Review

Round 1 (effort 3, general) found 1 bug and 1 suggestion. This fix pass addresses both. Round 2 has not run.

- M1 (bug): `Union_Should_DedupeCaseInsensitivePreserveFirstCasing` and `MatchById_DifferentGuidCasing_Should_Select` again use a GUID with hex letters. The union keeps the first casing.
- M2 (suggestion): the scan file set is the pack allow-list, so `msbuild/`, `.template.config/`, the root build and solution files, the kanban skeleton, and `readme.md` are scanned. `PackedFiles_Given_` and `RealPack_Should_CoverAllowListPaths` pin that set.

Ledger: `review/round-1/merged.md`. Framework: `review/review-framework.md`.

## Review

- Round 1: fixed on this branch — M1, M2 (round 2 not run)

## Notes

- Found while verifying PR #576 (task 410). Steve 2026-10-25: "yes file the clean up task".
