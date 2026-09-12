# Delete dead files and stale TODOs left by the migrations

## Description

Mechanical cleanup surfaced by the 210 round-1 code review of the architecture template
(`kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`,
findings M11, M16, M31, M35, M37, M38, M39, M40, M41). All items are deletes, renames, or
small edits with no design decisions required. One PR.

## Requirements

- **M11** — `tests/foundation/foundation-domain-jaribu-tests/` (`enumeration.cs`,
  `Directory.Build.props`): dead, never-executed runfile. No csproj, not under `source/`,
  filename does not match `*-tests.cs`, so neither `dev test` nor any aggregator touches it.
  Its header claims to be "a Jaribu duplicate of the Fixie suite" but the sibling
  `foundation-domain-tests` is itself Jaribu since 145-007 and the cases are equivalent.
  Delete the folder.

- **M16** — `timewarp-architecture.slnx`; `tests/tools/agent-identity-cli-tests/agent-identity-cli-tests.csproj`;
  `source/container-apps/web/projects/web-spa/web-spa.csproj`: both projects are absent from
  the solution with no documented rationale, unlike the JARIBU_MULTI aggregators and
  `timewarp-testing-tests`, which carry "intentionally not in .slnx" comments. `dev build`
  (slnx) never compiles `agent-identity-cli-tests` under the 0/0 gate; IDEs never show
  web-spa. Add `web-spa` under the `(web)` block and `agent-identity-cli-tests` under a new
  `/tests/tools/` folder in the slnx; or, if either is deliberately excluded, add the same
  one-line "intentionally not in .slnx" comment the aggregators carry.

- **M31** — `source/container-apps/web/projects/web-spa/pipeline/my-behavior.cs`;
  `source/container-apps/web/projects/web-spa/global-suppressions.cs:10`: `MyBehavior<,>`
  is a placeholder-named, unregistered pipeline behavior (program.cs wires only
  `ActiveActionBehavior` and `EventStreamBehavior`) — dead code shipped to every generated
  app, plus a suppression that exists only for it. Delete both; the two live behaviors
  already teach the pattern.

- **M35** — `source/container-apps/web/projects/web-spa/features/developer/components/user-claims-base.cs:5`:
  `TODO [2026-06]: Reassess UserClaimsBase …` is past its own checkpoint date with the
  decision (delete / integrate / leave) still "pending"; the file is an `#if false` sketch.
  Decide now — most likely delete — or open a kanban task and reference it from the file.

- **M37** — `source/container-apps/web/projects/web-spa/components/pages/SideNavigationLink.razor:5,9`:
  two stale TODOs — one naming a person and a source generator that has since shipped, one
  "Add Bootstrap classes" (Bootstrap and Tailwind both retired). Delete both lines.

- **M38** — `source/foundation/foundation-application/abstractions/i-current-user-service.cs:9`:
  `TODO: Should this be a strongly typed UserId?` is a design question, not a work item.
  Move into `#region Open Questions` (or answer it — the repo already has TypedId
  infrastructure).

- **M39** — `tests/common/timewarp-testing/scoped-sender.cs:1`;
  `tests/common/timewarp-testing/global-suppressions.cs:1,6-7`: no `#region Purpose` (TWA0004
  is off for `tests/`, but the rest of this project carries them); the IDE0052
  justification "Construction the item will start it" is typo'd and terse. Add Purpose
  lines; tighten the justification.

- **M40** — `.github/workflows/workflow.yml:25,51,110`; `timewarp-templates/version.json`:
  path filters name a `Directory.Version.props` that does not exist; `version.json` is an
  unused Nerdbank.GitVersioning file stuck at `1.0-beta` while the real version is
  `2.0.0-beta.17`. Remove the dead filter entries; delete `version.json`.

- **M41** — `source/analyzers/timewarp-architecture-analyzers/helpers/string-extensions.cs:24`;
  `source/foundation/foundation-domain/entities/base/i-aggregate-root.cs:48-50`: two
  `#pragma warning disable` lines are redundant — CA1308 is already `none` repo-wide in
  `.editorconfig:298`, and CA1040 is already in `source/foundation/Directory.Build.props`
  `<NoWarn>`. Delete both pragmas (or, if the project-wide suppression is trimmed under
  210-006/M32, keep the local one and drop the global instead).

## Checklist

- [x] M11: delete `tests/foundation/foundation-domain-jaribu-tests/`
- [x] M16: add `web-spa` and `agent-identity-cli-tests` to `timewarp-architecture.slnx`, or
      document the exclusion
- [x] M31: delete `my-behavior.cs` and its `global-suppressions.cs` entry
- [x] M35: decide and act on `UserClaimsBase` TODO (delete / integrate / leave + task link)
- [x] M37: delete the two stale TODO lines in `SideNavigationLink.razor`
- [x] M38: move `i-current-user-service.cs` TODO into `#region Open Questions` (or answer it)
- [x] M39: add Purpose regions and tighten the IDE0052 justification in `timewarp-testing`
- [x] M40: remove dead `Directory.Version.props` filter entries; delete `version.json`
- [x] M41: delete redundant pragmas in `string-extensions.cs` and `i-aggregate-root.cs`
- [x] `dev build` 0/0
- [x] `ganda repo audit`
- [x] `dev test`

## Notes

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.

## Session

- Created: 220514 (2026-09-12)
- Implementer: grok session 01a095bd-1f54-7142-aebe-9c1aaeb49ccd (2026-09-12)

## Results

Mechanical cleanup of 210 round-1 findings M11, M16, M31, M35, M37, M38, M39, M40, M41. One PR.

### What was implemented

- **M11** — Deleted dead `tests/foundation/foundation-domain-jaribu-tests/` (never executed; sibling `foundation-domain-tests` is the live Jaribu suite).
- **M16** — Added `web-spa` under the `(web)` block in `timewarp-architecture.slnx`. Added `/tests/tools/` with `agent-identity-cli-tests`, gated by `#if (false)` so generated apps do not list a project `template.json` excludes.
- **M31** — Deleted unregistered `MyBehavior<,>` and its CA1720 suppression. Live pipeline behaviors stay.
- **M35** — Deleted `#if false` `UserClaimsBase` sketch. Nothing inherited it; `UserClaims.razor` owns the live claims display.
- **M37** — Removed the two stale TODOs in `SideNavigationLink.razor` (person-named source-gen note + Bootstrap classes) and the unused attribute comment stubs.
- **M38** — Moved the typed-`UserId` question into `#region Open Questions` (Q1). Left `Guid?` in place; folding TypedId across CurrentUserService / IAuthApiRequest / the contracts generator is out of this cleanup's scope.
- **M39** — Added Purpose regions on `scoped-sender.cs` and `global-suppressions.cs`. Tightened the IDE0052 justification.
- **M40** — Removed three dead `Directory.Version.props` path-filter entries from `.github/workflows/workflow.yml`; deleted unused `timewarp-templates/version.json`.
- **M41** — Deleted redundant CA1308 / CA1040 pragmas. Project-wide suppressions still cover them; 210-006/M32 owns any later `<NoWarn>` trim.

Parent ledger statuses for those M-ids set to **fixed** (counts: bug 13/2, suggestion 14/3, nit 4/5).

### Files changed

Deletes: `tests/foundation/foundation-domain-jaribu-tests/`, `pipeline/my-behavior.cs`, `user-claims-base.cs`, `timewarp-templates/version.json`.

Edits: `timewarp-architecture.slnx`, `web-spa/global-suppressions.cs`, `SideNavigationLink.razor`, `i-current-user-service.cs`, `scoped-sender.cs`, `timewarp-testing/global-suppressions.cs`, `.github/workflows/workflow.yml`, `string-extensions.cs`, `i-aggregate-root.cs`, parent `review/round-1/merged.md`.

### Key decisions

- M16: include both projects (do not document exclusion). Gate `agent-identity-cli-tests` with `#if (false)` because it is monorepo-only.
- M35: delete, not integrate or leave + task.
- M38: record the question, do not change the type.

### Test outcomes

- `./bin/dev build` — 0 Warning(s) / 0 Error(s)
- `ganda repo audit` — pass (2 pre-existing advisory warnings: memsearch-scaffold, vscode-window-icon)
- `./bin/dev test` — pass, including `agent-identity-cli-tests` 11/11 (now compiled under the slnx 0/0 gate)

### How to validate

**Smoke**

```bash
test ! -d tests/foundation/foundation-domain-jaribu-tests
test ! -f source/container-apps/web/projects/web-spa/pipeline/my-behavior.cs
test ! -f source/container-apps/web/projects/web-spa/features/developer/components/user-claims-base.cs
test ! -f timewarp-templates/version.json
grep -n 'web-spa.csproj' timewarp-architecture.slnx
grep -n 'agent-identity-cli-tests.csproj' timewarp-architecture.slnx
grep -n 'Directory.Version.props' .github/workflows/workflow.yml || echo 'no Directory.Version.props matches'
grep -n 'TODO' source/container-apps/web/projects/web-spa/components/pages/SideNavigationLink.razor || echo 'no TODOs in SideNavigationLink'
grep -n 'Open Questions' source/foundation/foundation-application/abstractions/i-current-user-service.cs
```

**Expect**

- First four `test` commands succeed (paths gone).
- slnx lists `web-spa.csproj` inside the `(web)` block and `agent-identity-cli-tests.csproj` under `/tests/tools/` inside `#if (false)`.
- `grep Directory.Version.props` prints `no Directory.Version.props matches`.
- `SideNavigationLink.razor` has no TODO lines.
- `i-current-user-service.cs` has `#region Open Questions` and no `TODO:` line.
- `dev build` reports `0 Warning(s)` / `0 Error(s)` and compiles both `web-spa` and `agent-identity-cli-tests`.

**Automated gate**

```bash
./bin/dev build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

ganda repo audit
# expect: Repository passes (advisory warnings only)

./bin/dev test
# expect: Tests completed successfully; agent-identity-cli-tests 11 passed
```

**Not in scope:** folding `ICurrentUserService.UserId` onto TypedId; 210-006/M32 `<NoWarn>` audit.
