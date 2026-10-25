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

## Checklist

- [ ] All matches in `source/` and `tests/` reworded or replaced
- [ ] Real tenant name, domain and GUID replaced with Contoso examples and a made-up GUID
- [ ] `wwwroot/auth.md` examples neutral
- [ ] `skills/tw-blazor/SKILL.md` and `skills/tw-blazor-css-strategy/SKILL.md` cleaned
- [ ] `tools/` matches cleaned
- [ ] Gate added (names list unpacked, runs in CI on PRs, scans packed content); `tw-pr` lists it
- [ ] `git grep -n -i -E 'crunchit|copic' -- source tests tools skills ':!skills/*/analysis/**'` is empty
- [ ] `./bin/dev build` 0/0, tests pass, `./bin/dev template-smoke`, `./bin/dev lint-skills`, `ganda repo audit`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-25)

## Notes

- Found while verifying PR #576 (task 410). Steve 2026-10-25: "yes file the clean up task".
