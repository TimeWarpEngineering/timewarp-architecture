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

- [ ] M11: delete `tests/foundation/foundation-domain-jaribu-tests/`
- [ ] M16: add `web-spa` and `agent-identity-cli-tests` to `timewarp-architecture.slnx`, or
      document the exclusion
- [ ] M31: delete `my-behavior.cs` and its `global-suppressions.cs` entry
- [ ] M35: decide and act on `UserClaimsBase` TODO (delete / integrate / leave + task link)
- [ ] M37: delete the two stale TODO lines in `SideNavigationLink.razor`
- [ ] M38: move `i-current-user-service.cs` TODO into `#region Open Questions` (or answer it)
- [ ] M39: add Purpose regions and tighten the IDE0052 justification in `timewarp-testing`
- [ ] M40: remove dead `Directory.Version.props` filter entries; delete `version.json`
- [ ] M41: delete redundant pragmas in `string-extensions.cs` and `i-aggregate-root.cs`
- [ ] `dev build` 0/0
- [ ] `ganda repo audit`
- [ ] `dev test`

## Notes

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.

## Session

- Created: 220514 (2026-09-12)
