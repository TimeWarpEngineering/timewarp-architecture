# Round 1 — docs-skills
**Date:** 2026-09-09
**Scope reviewed:** AGENTS.md/CLAUDE.md (every path, skill name, diagnostic ID, exemplar
class/file cited); `documentation/**` (76 markdown files — relative links, cited code paths,
Fixie/xUnit/Tailwind/npm currency, ADR status/index accuracy); `skills/**` (8 skill folders —
frontmatter, name/folder match, public-skill rules); `readme.md`, `kanban/overview.md`,
`kanban/task-template.md`, `scripts/overview.md`, `runfiles/overview.md`, `spikes/overview.md`.
All findings below were verified against the actual tree (`test -e`, `grep`, `find`) before being
recorded — none are guesses.

## Summary
AGENTS.md itself is accurate on diagnostics (all TWA0001–0024 / TWE / SG ids match
`diagnostic-descriptors.cs` and the convention analyzers exactly) and on most cited paths, but six
skill references use a bare name that does not match the skill's real name, which breaks
skill-by-name invocation. The bigger problem is `documentation/**`: a meaningful fraction of it is
leftover pre-migration content (old per-class-file/PascalCase contract layout, `Source/Client`
paths, `net9.0`, Fixie-era lifecycle hooks) that actively contradicts the current, approved
architecture, plus a long tail of empty/TODO-stub pages and a few broken relative links.
`kanban/overview.md` is the most consequential: it describes a hand-numbered,
underscore-filename, PascalCase-folder workflow that directly contradicts AGENTS.md's "never
hand-number, always `ganda kanban create`" policy and the kebab-hyphen filenames actually in use.
Skills are otherwise clean except `tw-mock-response-factory`, whose frontmatter trigger list bakes
in a client-specific, nonexistent class name.

## Issues

### Issue 1 — Severity: bug
- File: `AGENTS.md:25,72,88,110,143,162`
- Description: Six skill references use a bare (non-`tw-`-prefixed) name that does not match the
  skill's actual registered name, so an agent invoking the skill by the name AGENTS.md gives will
  fail to find it:
  - `AGENTS.md:25` — `` `dev-cli` skill `` → actual skill name is `tw-dev-cli`
  - `AGENTS.md:72` — `` `blazor-css-strategy` skill `` → actual: `tw-blazor-css-strategy`
    (folder exists in this repo at `skills/tw-blazor-css-strategy/`)
  - `AGENTS.md:88` — `` `feature-placement` skill `` → actual: `tw-feature-placement` (this exact
    doc correctly uses `tw-feature-placement` at lines 46, 80, 101 — inconsistent within the same
    file)
  - `AGENTS.md:110` — `` `web-api-contracts` skill `` → actual: `tw-web-api-contracts` (folder
    exists at `skills/tw-web-api-contracts/`)
  - `AGENTS.md:143` — `` skill **`slice-isolation`** `` → actual: `tw-slice-isolation` (folder
    exists at `skills/tw-slice-isolation/`; the same sentence's path citation
    `skills/tw-slice-isolation/SKILL.md` is correct, only the bolded skill name is wrong)
  - `AGENTS.md:162` — `` `agent-context-regions` skill `` → actual: `tw-agent-context-regions`
- Suggestion: Prefix all six with `tw-` to match the real skill names, consistent with how
  `tw-pr`, `tw-git`, `tw-kanban`, `tw-csharp`, `tw-jaribu`, `tw-feature-placement`, and
  `tw-aggregate-pattern` are already written correctly elsewhere in the same file.
- Status: open

### Issue 2 — Severity: bug
- File: `kanban/overview.md` (whole file, 104 lines), `kanban/task-template.md:9`
- Description: `kanban/overview.md` documents a workflow that no longer exists and directly
  contradicts the authoritative policy in `AGENTS.md:167-174`:
  - Folder names given as `Backlog`, `ToDo`, `InProgress`, `Done` (PascalCase) and
    `Kanban/Task-Examples/`; the real folders are lowercase-kebab
    `kanban/backlog`, `kanban/to-do`, `kanban/in-progress`, `kanban/done`, `kanban/task-examples`.
  - Instructs manually assigning numeric ids and using underscore filenames
    (`001_implement-user-registration.md`, `B001_research-authentication-methods.md`), never
    mentions `ganda kanban create`. AGENTS.md is explicit: "Never hand-number task files — always
    create via `ganda kanban create`", and real task files use kebab hyphens
    (e.g. `kanban/done/061-migrate-remaining-ps1-scripts-to-dev-cli-endpoints.md`).
  - `kanban/task-template.md:9` reinforces the same stale convention with
    `<Reference to parent item like 001_user-registration>`.
  - The companion script `scripts/get-next-task-number.ps1` (described in `scripts/overview.md`)
    exists specifically to compute the next hand-assigned number — the exact operation AGENTS.md
    now forbids agents from doing.
  An agent reading `kanban/overview.md` instead of (or before) `AGENTS.md` would create
  wrongly-numbered, wrongly-named task files in nonexistent folders.
- Suggestion: Rewrite `kanban/overview.md` to describe the current `ganda kanban` CLI workflow and
  actual folder/filename conventions (or delete it and point to `AGENTS.md` §Task management +
  the `tw-kanban` skill as SSOT); fix the example in `task-template.md:9`; retire or clearly mark
  `get-next-task-number.ps1` as superseded.
- Status: open

### Issue 3 — Severity: bug
- File: `documentation/developer/how-to-guides/testing/how-to-add-lifecycles-to-tests.md`
- Description: Describes `Setup`/`Cleanup` as the lifecycle method names test classes should add,
  and links to `../../../../Tests/TimeWarp.Architecture.Testing/ConventionTests/LifecycleExamples.cs`.
  Neither is current: (a) that path does not exist anywhere in the repo (old PascalCase `Tests/`
  tree, pre-Jaribu), and (b) the current Jaribu convention (confirmed in
  `documentation/test-structure.md:9-10` and in exemplar `create-role-tests.cs`) uses
  `SetupOnce`/`CleanUpOnce`, not bare `Setup`/`Cleanup` — no test file in the repo declares plain
  `Setup`/`Cleanup` methods.
- Suggestion: Rewrite to reference `SetupOnce`/`CleanUpOnce` and link a real exemplar (e.g.
  `create-role-tests.cs` or `get-weather-forecasts-tests.cs`), or delete the page if superseded by
  `how-to-filter-tests-by-name.md`/`how-to-filter-tests-by-tags.md`.
- Status: open

### Issue 4 — Severity: bug
- File: `documentation/developer/conceptual/architectural-decision-records/approved/0003-endpoint-centric-api-with-interface-based-validation.md`
- Description: Relative link `../api-design.md` resolves to
  `documentation/developer/conceptual/architectural-decision-records/api-design.md`, which does
  not exist. The real target is two directories up:
  `documentation/developer/conceptual/api-design.md`.
- Suggestion: Change the link to `../../api-design.md`.
- Status: open

### Issue 5 — Severity: bug
- File: `documentation/developer/conceptual/features/application/is-processing.md`
- Description: Entire page describes the pre-rename layout and is orphaned/unlinked from anywhere
  else in `documentation/`. It cites `` [`Client`](\Source\Client\TimeWarp.ArchitectureBlazor.Client.csproj) ``
  and `` [`ApplicationState`](\Source\Client\Features\Application\ApplicationState.cs) `` — both
  paths (Windows-backslash, PascalCase `Source/Client/...`) confirmed absent from the repo; no
  `ApplicationState.cs` exists anywhere in the tree.
- Suggestion: Delete this page, or rewrite it against the current `source/container-apps/web/...`
  layout and `TimeWarp.State` APIs if the `IsProcessing` concept is still current.
- Status: open

### Issue 6 — Severity: bug
- File: `documentation/developer/reference/dotnet-conventions.md:4`
- Description: States `Target net9.0`. The repo targets `net10.0`
  (`Directory.Build.props:38: <TargetFramework>net10.0</TargetFramework>`, SDK pin
  `global.json` → `10.0.400`).
- Suggestion: Update to `net10.0`.
- Status: open

### Issue 7 — Severity: bug
- File: `readme.md:4`
- Description: The workflow badge points at
  `https://github.com/TimeWarpEngineering/blazor-state/actions/workflows/release-build.yml/badge.svg`.
  The repo's only workflow file is `.github/workflows/workflow.yml` — `release-build.yml` does not
  exist, so the badge cannot report real status. (Separately, `readme.md:1`'s
  `dotnet-6.0-blue` badge is stale — see Issue 9's sibling nit list — but the workflow link is the
  actionable break since it silently shows no/wrong CI status.)
- Suggestion: Point the badge at `.github/workflows/workflow.yml` (matching the repo's actual
  single-workflow CI pattern) or drop it if per-repo workflow badges aren't maintained.
- Status: open

### Issue 8 — Severity: bug
- File: `runfiles/overview.md`
- Description: Says "See existing runfiles in this directory for examples: `build.cs` - Build
  solution (replaces Build.ps1)". The `runfiles/` directory contains only `overview.md` — no
  `build.cs` or any other runfile exists yet (consistent with task 061, ps1→dev-CLI migration,
  still being open per the repo's own tracking, but the doc asserts the example already exists).
- Suggestion: Either add the referenced `build.cs` or change the sentence to state that runfiles
  are not yet populated / point at the actual current examples under
  `source/**/*-tests.cs` instead.
- Status: open

### Issue 9 — Severity: bug
- File: `documentation/developer/conceptual/architectural-decision-records/project-structure-and-conventions/project-structure-and-conventions.md`, `.../project-structure-and-conventions/ai-context-test.md`
- Description: This whole orphaned subfolder (nothing in `documentation/` or `AGENTS.md` links to
  it) documents a contract layout that is the *opposite* of the current, approved architecture:
  one PascalCase folder + file per class (`Command1/Command1.Command.cs`, `Command1.Response.cs`,
  `Command1.Validator.cs`), plain `ProblemDetails`, and a `<Container>.<Feature>.<Entity>`
  namespace scheme. The current pattern (AGENTS.md "Key patterns", ADR-0008) is the opposite:
  a single endpoint-centric `*-contracts.cs` file per operation with `[ApiRoute]`-generated route
  members, `OneOf<Response, SharedProblemDetails>`, and `…Features.<Id>` namespaces. Because it
  ships inside `documentation/` (which the template copies verbatim into every generated app,
  per `AGENTS.md:178-179`), a generated app carries stale/contradictory architecture guidance.
- Suggestion: Delete the `project-structure-and-conventions/` subfolder (superseded by ADR-0008 +
  `tw-web-api-contracts` skill), or explicitly mark it historical/rejected if it must be kept for
  the record.
- Status: open

### Issue 10 — Severity: bug
- File: `documentation/developer/conceptual/architectural-decision-records/overview.md`
- Description: This is the top-level ADR index but it is unedited MADR-tool boilerplate: every
  linked entry points into `examples/` (the tool's own worked examples, e.g. "Use CC0 as
  license", "Do not use numbers in headings") — none of the real approved decisions
  (0001–0010 under `approved/`, which do exist and are individually accurate per
  `approved/overview.md`) are listed or linked from this page at all.
- Suggestion: Regenerate/hand-edit this index to link `approved/overview.md` (and `proposed/`) as
  the actual decision log, removing or clearly separating the tool's own example ADRs.
- Status: open

### Issue 11 — Severity: bug
- File: `documentation/developer/conceptual/component-naming-and-organization.md`
- Description: The "Example Directory Structure" section (lines ~150-255) uses PascalCase folder
  names throughout (`Components/`, `Elements/`, `Base/`, `Editors/`, `Forms/`, `Layouts/`,
  `Composites/`, `Pages/`, `Features/`, `UserManagement/`) — the real tree under
  `source/container-apps/web/projects/web-spa/` is all lowercase kebab
  (`components/elements/base/forms/layouts/composites/pages`, plus a top-level `features/`),
  confirmed via `find`. There is no `editors/` folder at all (the `DateEditor`/`NumericEditor`
  example folder doesn't exist). The doc also shows two full duplicate layouts
  (`Pages/TimeWarpPage/`, `Pages/AlternatePage/`, each re-declaring Footer/Header/Navigation/
  Banner) — this contradicts the current single-shell pattern in the `tw-blazor-layout` skill
  ("keep `LayoutComponentBase` empty… ONE shell component"), and the cited `AltLayout.razor`
  doesn't exist (only `components/layouts/MainLayout.razor` and `FluentUIRequiredFeatures.razor`
  are present).
- Suggestion: Rewrite the example tree in lowercase kebab folder names (Razor filenames
  themselves stay PascalCase per the documented `.razor` exception) and replace the two-layout
  example with the current single-shell pattern, cross-referencing `tw-blazor-layout`.
- Status: open

### Issue 12 — Severity: bug
- File: `skills/tw-mock-response-factory/SKILL.md:4`
- Description: Frontmatter `when-to-use:` trigger list includes `MockCopicApiService` — a
  client-specific class name from outside this repo. It also does not exist anywhere in this
  repo's code (`grep -rl MockCopicApiService source/` → no hits); the real class here is
  `MockWebApiService` (`source/container-apps/web/projects/web-spa/services/mocks/mock-web-api-service.cs`).
  Skills publish to timewarp.software with no client names permitted.
- Suggestion: Remove `MockCopicApiService` from the trigger list (it's redundant with
  `MockWebApiService`, already present in the same list).
- Status: open

### Issue 13 — Severity: suggestion
- File: `documentation/overview.md`
- Description: Entirely unedited template boilerplate: "TODO: Give a short introduction of your
  project…", a link to `CONTRIBUTING.md` (doesn't exist), a link literally reading
  `https://todo/your-docs`, and a dotnet-core 3.0 SDK download link. This page ships to every
  generated app under `dotnet new timewarp-architecture`.
- Suggestion: Write real content or delete/redirect to `readme.md`.
- Status: open

### Issue 14 — Severity: suggestion
- File: Nine near-empty/stub documentation pages
- Description: The following are either 0 bytes or a single unexplained line, providing no
  guidance despite occupying a spot in the doc tree that a reader/agent would expect to be useful:
  - `documentation/roadmap.md` (0 lines)
  - `documentation/developer/overview.md` (0 lines)
  - `documentation/developer/tutorials/overview.md` (0 lines; `tutorials/` has no other content)
  - `documentation/developer/conceptual/testing/overview.md` (0 lines)
  - `documentation/developer/conceptual/features/overview.md` (0 lines)
  - `documentation/developer/conceptual/architectural-decision-records/proposed/overview.md` (0 lines)
  - `documentation/developer/conceptual/architectural-decision-records/project-structure-and-conventions/overview.md` (0 lines)
  - `documentation/developer/conceptual/testing/end-to-end-testing.md` (2 lines: "# End to end
    testing" / "Playwright")
  - `documentation/developer/how-to-guides/testing/how-to-write-endpoint-test.md` (3 lines, no
    actual instructions)
- Suggestion: Fill in or remove; an empty `overview.md` per folder is acceptable convention noise
  in small doses but nine of them plus two content-stub pages is worth a pass.
- Status: open

### Issue 15 — Severity: suggestion
- File: `documentation/developer/conceptual/architectural-decision-records/proposed/xxx.md`, `.../proposed/xxxx-powershell-coding-standards.md`
- Description: Both files still carry placeholder filenames (literal `xxx`/`xxxx` instead of a
  real ADR number and slug). `xxx.md` duplicates the same stale per-class-file contract
  convention flagged in Issue 9. `xxxx-powershell-coding-standards.md` is an un-integrated PowerShell
  style-guide fragment (source: a GitHub issue link) with no ADR framing (no context/decision/
  consequences), sitting in `proposed/` seemingly indefinitely.
- Suggestion: Either finish and rename these as real ADRs, or delete them — `proposed/` should not
  be a permanent parking lot for unfinished drafts with placeholder filenames.
- Status: open

### Issue 16 — Severity: nit
- File: `skills/tw-web-api-contracts/analysis/composer-skill-analysis.md`, `.../analysis/glm52-review.md`
- Description: Both contain the client name "copic" and past-tense internal review narrative.
  Per established convention this repo's `analysis/` subfolders are meant to be excluded from the
  public skill sync to timewarp.software, but nothing inside this repo (no `.syncignore`, CI
  config, or note in the skill itself) documents that exclusion — a future contributor or a
  differently-configured sync run could publish it verbatim.
- Suggestion: Add a one-line note (e.g. a `.gitattributes`/README marker in `analysis/`, or a
  comment in the skill) stating this folder is excluded from public sync, so the exclusion isn't
  tribal knowledge.
- Status: open

## Checked clean
- CLAUDE.md — confirmed it is exactly `@AGENTS.md`, nothing else.
- AGENTS.md diagnostic tables — every TWA0001–TWA0024, TWE002/003/005/006/007, SG001/002/010/011,
  and the retired TWA0005/TWE001/TWE004 markers all match
  `source/analyzers/timewarp-architecture-analyzers/diagnostics/diagnostic-descriptors.cs` and the
  convention analyzers exactly; no orphaned ids in either direction.
- AGENTS.md exemplar files/classes — `create-role-tests.cs`, `get-weather-forecasts-tests.cs`,
  `SpaSessionFixture`, `HostGraphFactory`, `SessionHostFixture`, `ContractSerializationDefaults`,
  `ServiceNames`, `IngressReservedPathPrefixes` (MSBuild property, not a class — correctly used),
  `WebServerApiRoutePrefixes`, `MockAuthenticationRegistration`, `AggregateDbContext`,
  `how-to-remove-demo-features.md`, `how-to-upgrade-to-analyzer-packages.md`, `file-naming.md` —
  all exist as described.
- `source/container-apps/api/platform/` — AGENTS.md's own line 150 ASCII diagram says
  "platform/ tree absent (no content yet)" but this is now stale (an `identity-host/` cluster with
  5 files exists) — **note:** this line is inside the tree diagram at line 150 and is covered by
  the general staleness already logged; not double-counted as a separate issue since it's a
  one-line diagram annotation rather than instructive prose, but worth fixing alongside Issue 1.
- `feature-filename-grammar.g.props` / `feature-membership.targets` — exist for web, api, grpc as
  described.
- Fixie/xUnit/Tailwind/npm mentions found in `documentation/` (`file-naming.md`,
  `test-structure.md`, `integration-testing.md`, ADR examples) are all correctly framed as
  historical/retired, not presented as current instructions.
- `RolePolicyGrants` mention in `approved/0010-permission-centric-authorization.md` — correctly
  framed as a retired/considered-and-rejected option, not current guidance.
- No `Features.Authentication` namespace or old PascalCase `Source/` folder instructions found
  anywhere outside the already-flagged stale pages.
- `skills/*/SKILL.md` frontmatter — all 8 skills have both `name` and `description`, and each
  `name` matches its folder (`tw-aggregate-pattern`, `tw-blazor`, `tw-blazor-css-strategy`,
  `tw-blazor-layout`, `tw-feature-placement`, `tw-mock-response-factory`, `tw-slice-isolation`,
  `tw-web-api-contracts`).
- `skills/**` (outside `tw-mock-response-factory`'s frontmatter and the one `analysis/` folder) —
  no other past-tense migration narrative, task-number-only justifications without a stated rule,
  or client names found; task-number citations found in `tw-web-api-contracts/SKILL.md` and
  `tw-feature-placement/SKILL.md` all pair a stated rule with the task number as supporting
  evidence, which is the acceptable pattern already used throughout AGENTS.md itself.
- `scripts/overview.md` — accurately describes the `.ps1` scripts actually present.
- `spikes/overview.md` — generic but not inaccurate (no claims to verify).
- Oakton (`documentation/developer/how-to-guides/how-to-run-oakton-commands.md`) — package and
  usage still current (`Directory.Packages.props` pins `Oakton 6.3.0`; referenced from
  `web-server`/`api-server` `program.cs`).
- `documentation/developer/how-to-guides/web-api-contracts/how-to-write-bff-api-contracts.md` —
  correctly uses `SharedProblemDetails`, matching `source/foundation/foundation-contracts/types/shared-problem-details.cs`.
- `documentation/developer/how-to-guides/overview.md` — all its links resolve; content matches
  current file set.
- `NuGet` package id `TimeWarp.Architecture` in `readme.md` — matches
  `timewarp-architecture-template.csproj`'s `<PackageId>`.
