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

The template is copied into every generated app, so packed files must not name a client.
Reword comments and docs so the rule remains and the source credit goes away. Replace the
real Entra sample (legal name, domains, tenant id) with Contoso and a made-up GUID, and
rename tests so they describe the behavior. `wwwroot/auth.md` uses `contoso.com`, and the
Azure Container Apps sentence no longer names a client. `kanban/`, `documentation/`, and
`skills/*/analysis/` stay as they are.

Add a gate that scans the packed trees (`source/`, `tests/`, `skills/` except `analysis/`,
and `tools/` only if the template pack includes it), not only the PR diff. The name list
lives in a file both `.template.config/template.json` and the `timewarp-templates` pack
leave out. `ganda repo audit` has no word-list check, so the gate is `dev check-client-names`.
`dev build` runs it, which is what CI's `ci (fast)` job already calls, and `dev lint-skills`
runs it so a skills-only PR is covered too. The repo gate list that `tw-pr` tells agents to
read (`AGENTS.md`) names the verb.

Done means the packed-tree search for those names is empty, the gate fails on a planted
name, and build, tests, template smoke, skill lint, and `ganda repo audit` pass.

## Checklist

- [x] All matches in `source/` and `tests/` reworded or replaced
- [x] Real tenant name, domain and GUID replaced with Contoso examples and a made-up GUID
- [x] `wwwroot/auth.md` examples neutral
- [x] `skills/tw-blazor/SKILL.md` and `skills/tw-blazor-css-strategy/SKILL.md` cleaned
- [x] `tools/` matches cleaned
- [x] Gate added (names list unpacked, runs in CI on PRs, scans packed content); `tw-pr` lists it
- [x] `git grep -n -i -E 'crunchit|copic' -- source tests tools skills ':!skills/*/analysis/**'` is empty
- [x] `./bin/dev build` 0/0, tests pass, `./bin/dev template-smoke`, `./bin/dev lint-skills`, `ganda repo audit`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-25)
- Implementation: 01a170b0-8b68-7c61-9c79-7b9fa8da6a91 (2026-10-25). Reworded packed comments, replaced the sample tenant with Contoso and `00000000-0000-0000-0000-000000000001`, and added `dev check-client-names`. `ganda repo audit` exited 0. The `kanban-landed` warning is the same advisory as task 410 (eight other live claims have no worktree) and was left alone.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-25T02:08:21Z
- Review round 1 (2026-10-25): review oracle (claude-opus-5-5), effort 3, roster general. Wrote `review/round-1/general.md` and `review/round-1/merged.md`. 2 open findings (M1 bug, M2 suggestion), so a fix pass is needed.

## Results

Packed comments and docs now state the rule without naming a client. Sample Entra data is Contoso (`Contoso, Ltd`, `contoso.com`, `contoso.onmicrosoft.com`) and the made-up tenant id `00000000-0000-0000-0000-000000000001`. Test names describe the behavior. `wwwroot/auth.md` uses `contoso.com` and "for example, Azure Container Apps".

`dev check-client-names` reads `documentation/developer/client-names.txt` and scans `source/`, `tests/`, and `skills/` except `skills/**/analysis/`. `tools/` is scanned only when the template pack includes `tools/dev-cli/dev.cs`. This repo's allow-list does not, so `tools/` is cleaned for the acceptance search and is not part of the packed scan. The name list is outside those trees. The scan fails if `documentation/**` stops being an unconditional exclude or the pack allow-list starts including the file. `ganda repo audit --list-checks` has no word-list check, so the gate is this verb. `dev build` runs it (CI `ci (fast)` calls that build through `dotnet run tools/dev-cli/dev.cs -- workflow`). `dev lint-skills` runs it so a skills-only PR is covered. `AGENTS.md` lists the verb where `tw-pr` tells agents to read this repo's gates. `timewarp-flow`'s `tw-pr` skill was not edited.

The packed nupkg `TimeWarp.Architecture.2.0.0-beta.25` from template-smoke contains neither the name list nor those client tokens, and it does not contain `tools/dev-cli/dev.cs`.

CI and this validation used `dotnet run tools/dev-cli/dev.cs`, which is what `workflow.yml` runs. `./bin/dev` is the gitignored AOT cache and does not gain the verb until `dev self-install`.

### How to validate

Smoke:

```bash
git grep -n -i -E 'crunchit|copic' -- source tests tools skills ':!skills/*/analysis/**'
dotnet run tools/dev-cli/dev.cs -- check-client-names
```

Expect: the search prints nothing. The verb prints `Client-name scan passed.` and exits 0. A planted token under `source/` or `skills/*/SKILL.md` fails the scan; the same token under `skills/*/analysis/` does not. Those cases are `ClientNameScan_` in `tests/tools/dev-cli-tests` (fixture token `acmeclient`, so the denylist words stay out of packed tests).

Gates run on this walk (all exit 0):

- `dotnet run tools/dev-cli/dev.cs -- build` — Build succeeded, 0 Warning(s), 0 Error(s), and the scan passed first.
- `dotnet run tools/dev-cli/dev.cs -- test --no-build --group fast` — 14/14 projects passed.
- `dotnet run tools/dev-cli/dev.cs -- test --no-build --group integration` — 8/8 projects passed (includes the Contoso tenant tests and web-jaribu co-located tests).
- `dotnet run tools/dev-cli/dev.cs -- test --no-build --group playwright` — 1/1 passed (27/27).
- `dotnet run tools/dev-cli/dev.cs -- template-smoke` — Template smoke SUCCEEDED (SmokeDefault build 0/0).
- `dotnet run tools/dev-cli/dev.cs -- lint-skills` — 9/9 skills, eval.yaml valid, scan passed first.
- `ganda repo audit` — exit 0 (33 passed, 1 skipped, `kanban-landed` advisory only).

### Review

Round 1 (effort 3, general) is not dispositioned yet. It has 1 open bug and 1 open suggestion.

- M1 (bug): `entra-tenant-tests.cs` lost its case-variant ids, because the all-digit GUID has no hex letters.
- M2 (suggestion): the scan roots are hardcoded and skip the packed `msbuild/`, `.template.config/`, root files, kanban skeleton, and `readme.md`.

Ledger: `review/round-1/merged.md`. Framework: `review/review-framework.md`.

## Notes

- Found while verifying PR #576 (task 410). Steve 2026-10-25: "yes file the clean up task".
