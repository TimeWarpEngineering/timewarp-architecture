# Bump outdated third-party packages: FastEndpoints beta.7, Blazilla 2.4.3, Markdig 1.6.0, simple-icons 16.36.0

## Description

`ganda nuget outdated` on 2026-11-05 lists five outdated pins, all third party. Bump them in
`Directory.Packages.props` and prove the build, tests, and template smoke still pass.

| Package | Current | Target |
|---|---|---|
| FastEndpoints, FastEndpoints.OpenApi | 9.0.0-beta.6 | 9.0.0-beta.7 |
| Blazilla | 2.4.2 | 2.4.3 |
| Markdig | 1.5.1 | 1.6.0 |
| timewarp-simple-icons | 16.35.0 | 16.36.0 |

## Requirements

- Bump only these pins. Leave TimeWarp.* and Microsoft rc pins alone.
- FastEndpoints: read the beta.7 release notes. The generated endpoints come from
  `fast-endpoint-emitter.cs` and the documentation resolver (task 472); if beta.7 changes any API the
  emitter targets, fix the emitter and its tests, not the generated output.
- Blazilla: the four forms (`RoleForm`, `TodoItemForm`, `ProfilePage`, admin `AuthenticationPage`)
  bind `EditForm` to a details interface with an explicit validator instance. Confirm 2.4.3 keeps
  that; run the web-spa integration and Playwright suites.
- No behavior changes, no refactors riding along. `ganda repo audit --fix` output is policy.
- Version: run `dev check-version`. Package pins in the template ship to generated apps; bump
  `<Version>` and the platform pins only if check-version says the current version is published.

## Goal (implementer restatement)

Move five third-party pins in `Directory.Packages.props` to their latest releases
(FastEndpoints + FastEndpoints.OpenApi 9.0.0-beta.7, Blazilla 2.4.3, Markdig 1.6.0,
timewarp-simple-icons 16.36.0) and nothing else.

Acceptance:
- Only those pins (and their comments) change; TimeWarp.* and Microsoft pins untouched.
- FastEndpoints beta.7 changes checked against what `fast-endpoint-emitter.cs` emits; emitter fixed
  with tests only if an emitted API changed.
- Blazilla 2.4.3 still takes an explicit validator instance on the four I*Details forms.
- `dev build` 0 warnings / 0 errors; `dev test` (fast + integration, including web-spa Playwright)
  green; `dev template-smoke` green; `ganda repo audit` clean.
- `dev check-version` run; `<Version>` bumped only if the current one is already published.

## Checklist

- [x] Pins bumped in Directory.Packages.props
- [x] FastEndpoints beta.7 notes read; emitter unaffected or fixed with tests
- [x] dev build 0/0, dev test fast + integration, Playwright, dev template-smoke
- [x] check-version handled

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)

## Results

`Directory.Packages.props` changes:

- FastEndpoints and FastEndpoints.OpenApi: 9.0.0-beta.6 to 9.0.0-beta.7. The pin comment is updated:
  beta.7's net11.0 group depends on Microsoft.AspNetCore.OpenApi >= 11.0.0-rc.3.26522.148, the same
  as this repo's ASP.NET Core train. The direct foundation-server reference stays (out of scope).
- Blazilla: 2.4.2 to 2.4.3.
- Markdig: 1.5.1 to 1.6.0.
- timewarp-simple-icons: 16.35.0 to 16.36.0.

Release-note review:

- FastEndpoints v9.0.0-beta.6...v9.0.0-beta.7 has 11 commits. Its changelog diff adds two things:
  the net11 dependency moves to RC3, and an `Idempotency()` endpoint now completes its 400 response.
  The other commits are financial idempotency middleware and MCP/A2A agent internals. The emitter
  (`fast-endpoint-emitter.cs`) and the repo never call `Idempotency()`, financial idempotency, or the
  agent packages. The emitter needs no change.
- Blazilla v2.4.2...v2.4.3 has no `src/` code change (dependabot bumps and NuGet publishing only).
  The explicit validator instance on the four I*Details forms is unchanged.
- Markdig 1.6.0 adds a Table of Contents extension and fixes `Markdown.Normalize`. SafeMarkdown
  uses neither.

Gates (all run 2026-11-05 in this worktree):

- `dev build`: 0 warnings, 0 errors.
- `dev test --no-build`: 23 of 23 projects passed (fast, integration, web-spa-integration-tests,
  web-spa-playwright-tests 37/37).
- `dev template-smoke`: SUCCEEDED.
- `dev check-version`: source is 2.0.0-beta.32 and nuget.org has 2.0.0-beta.31. Not yet published,
  so no `<Version>` bump.
- `ganda repo audit`: passes, with 2 advisory warnings that this task did not cause. kanban-landed
  lists other tasks' claims without worktrees. source-file-line-cap lists EF migration designers and
  a done-task research file; that check is warning-only by policy (TWA0038 enforces). Nothing for
  `--fix` to change.

### How to validate

Smoke: `./bin/dev build && ./bin/dev test && ./bin/dev template-smoke`

Expect: the build reports 0 Warning(s) and 0 Error(s), the test summary reports "23 of 23 projects
passed", and template-smoke prints "Template smoke SUCCEEDED". `grep -nE
'FastEndpoints|Blazilla|Markdig|simple-icons' Directory.Packages.props` shows 9.0.0-beta.7, 2.4.3,
1.6.0, and 16.36.0.

## Notes

Steve 2026-11-05: "create small task to update all and launch it". Blazilla is kept on purpose:
it accepts an explicit validator instance, which the shared I*Details validator pattern needs
(`skills/tw-web-api-contracts/references/mutability.md`).
