# Take TimeWarp.Nuru 4.0.0-beta.8

## Description

TimeWarp.Nuru 4.0.0-beta.8 was released on 2026-10-30 (release run 41041359868, success). Bump
`TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` from `4.0.0-beta.7` to `4.0.0-beta.8` in
`Directory.Packages.props`. Fix anything that breaks in `tools/dev-cli/` or other consumers.

The `TimeWarp.Nuru` beta.8 nupkg is on the nuget.org flat container (HTTP 200), but the search
index still listed beta.7 when this task was filed. That is why the `nuru` audit check reported
"up to date". Restore uses the flat container, so it works.

## Requirements

- Bump both pins to `4.0.0-beta.10` (operator decisions 2026-10-30 and 2026-10-31, see Session). No
  `VersionOverride`, no backward pins.
- Remove `RCS1194` from the `NoWarn` list in `tools/dev-cli/Directory.Build.props`, along with its
  comment. DevCli 4.0.0-beta.9 adds the `HttpRequestError` constructor to
  `PackageFeedUnreadableException`, so the warning no longer fires.
- Delete the local copy `tools/dev-cli/services/repo-config-service.cs` and the
  `Directory.Build.targets` step that drops the package's copy. DevCli 4.0.0-beta.10
  (timewarp-nuru task 562) deserializes with `JsonContext.RepoConfig`, so CA2263 no longer fires.
  No NoWarn and no copied package files remain.
- Fix any API breaks in the dev CLI and other consumers. Do not suppress warnings.
- After the bump, rebuild the dev CLI (`dev self-install`, or run it from source) so the gates
  run the new Nuru, not the stale `bin/dev`.
- Run `dev check-version`. `2.0.0-beta.30` is not published yet (task 456), so no further
  `<Version>` bump should be needed; take one only if check-version asks for it.
- Gates: `ganda repo audit`, `dev build` (0 warnings, 0 errors), `dev test`,
  `dev template-smoke`, `dev check-version`.

## Checklist

- [x] Pins bumped to 4.0.0-beta.9
- [x] `RCS1194` NoWarn and its comment removed
- [x] CA2263 fixed without a new suppression
- [x] `bin/dev` rebuilt from beta.9
- [x] Gates green on beta.9

## Session

- Created: 8d5a7906-5253-4dc5-b6d4-729819f6f6b0 (2026-10-30)
- Implementation: 01a18c6e-5da6-72f2-b81b-b1eb58f0500b (2026-10-30)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a18c6e-5da6-72f2-b81b-b1eb58f0500b, max-turns 500, Done — 2026-10-30T11:16:51Z

- Review oracle: claude (claude-opus-5-5), effort 1, roster general, round 1 — 2026-10-30. Blocked: operator decision needed on the RCS1194 NoWarn.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 998e140e-6f24-473e-acc3-6ed85f62ee16, max-turns 80 — 2026-10-30T11:20:03Z
- Review oracle: claude (claude-opus-5-5), effort 2, roster general, round 2, turn cap 120 — 2026-10-30. Blocked: fix pass needed for open findings M1, M2, M3.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session f75ae191-a44c-49ad-a9a9-928c665369c9, max-turns 120 — 2026-10-30T15:16:09Z
- Review oracle: claude (claude-opus-5-5), effort 2, roster general, round 3, turn cap 120 — 2026-10-30. Blocked: stalemate, operator decision needed on the local copy of RepoConfigService.
- Fix pass: 01a18d61-f891-7273-9b4f-db6312f1c44b (2026-10-30). Pins at beta.9, RCS1194 removed, CA2263 fixed in the runfile without a new NoWarn entry.
- Operator decision (Steve, 2026-10-31) on M4/M5 (CA2263 in DevCli beta.9 RepoConfigService):
  file the bug with Nuru. Filed as timewarp-nuru task 562 (generic `GetTypeInfo<RepoConfig>()`).
  Wait for the Nuru beta that ships it, then take that pin and delete the local
  `tools/dev-cli/services/repo-config-service.cs` copy and its Directory.Build.targets removal step.
- Nuru 4.0.0-beta.10 shipped 2026-10-31 with task 562: `RepoConfigService.Parse` uses
  `JsonSerializer.Deserialize(json, JsonContext.RepoConfig)`. Fix pass: pins to beta.10, delete the
  local RepoConfigService copy and its targets step, re-run every gate. This resolves M4 and M5.

## Goal (implementer restatement)

Take the published TimeWarp.Nuru 4.0.0-beta.9 pin. Both central package pins, `TimeWarp.Nuru` and
`TimeWarp.Nuru.DevCli`, move from `4.0.0-beta.8` to `4.0.0-beta.9` in `Directory.Packages.props`.
No other package stays on beta.8, and no project uses `VersionOverride` or a backward pin.

DevCli 4.0.0-beta.9 adds
`PackageFeedUnreadableException(HttpRequestError, string?, Exception?, HttpStatusCode?)`. Remove
`RCS1194` from the `NoWarn` list in `tools/dev-cli/Directory.Build.props` and delete the comment
that justified that suppression. Do not add a replacement suppression. If beta.9 changes a public
API the dev CLI or another consumer calls, update the call site so the solution still builds with
warnings as errors.

Rebuild `bin/dev` from this pin so the gates run beta.9, not the installed beta.8 binary.

`dev check-version` should accept the current `<Version>` (`2.0.0-beta.30`). That version is not
on NuGet yet (task 456), so leave `<Version>` alone unless check-version says it is already
published. Then run `ganda repo audit`, `dev build`, `dev test`, `dev template-smoke`, and
`dev check-version`, and record how to validate.

## Review

- Round 2: Blocked — open M1, M2, M3
- Round 3: Blocked. M1, M2, and M3 are fixed. M4 (an unguarded target) and M5 (the local copy of Nuru
  internals) are open. Operator decision needed: accept the copy or wait for Nuru.

## Notes

- Nuru releases itself; this task only takes the pin.
- beta.8 DevCli reads nuget.org and the org GitHub Packages feed in `check-version` and
  `dev release` (Nuru 536). A version missing from nuget.org is unknown when that feed cannot be
  read.

## Results

Pinned `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` from 4.0.0-beta.8 to 4.0.0-beta.9 in
`Directory.Packages.props`. No `VersionOverride`. `<Version>` stays `2.0.0-beta.30`.

Removed `RCS1194` and its comment from `tools/dev-cli/Directory.Build.props`. DevCli
4.0.0-beta.9 declares
`PackageFeedUnreadableException(HttpRequestError, string?, Exception?, HttpStatusCode?)`.

That same package content calls `JsonSerializerOptions.GetTypeInfo(Type)` in
`repo-config-service.cs`. Warnings are errors, so CA2263 fails the runfile. The type is
not partial and the source lives in the package, so this repo does not add a suppression.
`tools/dev-cli/services/repo-config-service.cs` is that type with
`GetTypeInfo<RepoConfig>()`. `tools/dev-cli/Directory.Build.targets` drops the packaged
Compile item (`OmitPackagedRepoConfigService`). Delete both when a later pin uses the
generic overload. `source/container-apps` still has its own `RCS1194` NoWarn. That line
predates this task.

`dotnet build tools/dev-cli/dev.cs`: 0 warnings, 0 errors. `bin/dev` was self-installed
from this pin. It is gitignored.

`dev check-version` read both feeds. Source `2.0.0-beta.30` is one prerelease increment
ahead of published `2.0.0-beta.29`, so no version bump.

`dev build`: 0 warnings, 0 errors.

`dev template-smoke` succeeded. SmokeDefault built with 0 warnings. Generated Jaribu
aggregators: web 442/442, api 9/9, timewarp-testing 14/14. Standalone co-located tests
passed (5/5, 5/5, 2/2).

`dev test`: 22 of 23 projects passed. `web-spa-playwright-tests` failed 26 of 33 while
`template-smoke` ran on the same machine. The browser could not load
`_framework/dotnet.js`. A later `./bin/dev test --group playwright`, alone, passed 33/33
in 5m 09s.

`ganda repo audit` exited 0 after `ganda repo audit --fix` landed this kitchen:
Passed 34, Failed 1 advisory. `nuru` says TimeWarp.Nuru is up to date.
`kanban-landed` still warns for eight other claims with no worktree (070-001,
104-024, 104-025, 104-026, 123, 141, 198, 244). Those claims are outside this
id. `kanban-landed --fix` does not remove them.

The release job in `.github/workflows/workflow.yml` does not run `check-version` or
`dev release`. It still has `contents: read` and `id-token: write` only. A later step that
calls those gates in that job needs `packages: read` (Nuru 536).

### How to validate

**Smoke**

```bash
grep -n 'PackageVersion Include="TimeWarp.Nuru' Directory.Packages.props
grep -n RCS1194 tools/dev-cli/Directory.Build.props || echo 'RCS1194 absent'
dotnet build tools/dev-cli/dev.cs
./bin/dev check-version
./bin/dev build
```

**Expect**

- Both `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` are `4.0.0-beta.9`.
- `tools/dev-cli/Directory.Build.props` has no `RCS1194`.
- `dotnet build tools/dev-cli/dev.cs` ends with `0 Warning(s)` and `0 Error(s)`.
- `./bin/dev check-version` prints `Version in source: 2.0.0-beta.30`, `Latest NuGet version: 2.0.0-beta.29`, and `safe to release`.
- `./bin/dev build` ends with `0 Warning(s)` and `0 Error(s)`.

**Automated gate**

```bash
ganda repo audit
./bin/dev build
./bin/dev test
./bin/dev template-smoke
./bin/dev check-version
```

### Review

- Round 1, effort 1, roster `general`. The restatement matches the task, so there is no misread.
- Counts: bug 1 open; suggestion 0; nit 0.
- Disposition: none yet. M1 is still open: the dev CLI `NoWarn` adds `RCS1194` for the sealed
  `PackageFeedUnreadableException` in DevCli 4.0.0-beta.8 package content. That conflicts with
  "Do not suppress warnings". The fix is upstream in Nuru. The operator chooses between accepting
  it as a documented exception and taking an upstream Nuru fix.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Operator decision (Steve, 2026-10-30): do not accept the RCS1194 suppression; wait for the Nuru fix.
  Nuru shipped 4.0.0-beta.9 the same day, and its DevCli `package-feed-lookup.cs` now has
  `PackageFeedUnreadableException(HttpRequestError, string?, Exception?, HttpStatusCode?)`. Fix pass:
  move both pins to beta.9, drop `RCS1194` from the dev CLI `NoWarn`, and re-run the gates. This
  resolves M1.
- Round 2, effort 2, roster `general`. No fix pass ran between rounds. The product diff still pins
  beta.8 and keeps `RCS1194` in `NoWarn`.
- Counts: bug 2 open (M1 carried, M2 pins at beta.8); suggestion 1 open (M3: Goal, Results, and
  Checklist still describe beta.8); nit 0.
- Disposition: none yet. The fix pass is needed for M1, M2, and M3. The local cache has DevCli
  beta.9 with the constructor at `content/any/services/package-feed-lookup.cs:142`.
- No misread. The Goal restatement matched the task as written; the task was retargeted after it.
- Paths: `review/review-framework.md`, `review/round-2/general.md`, `review/round-2/merged.md`.
- Fix pass after round 2: both pins are `4.0.0-beta.9`, `RCS1194` is out of the dev CLI `NoWarn`, and Goal, Results, and Checklist describe beta.9. CA2263 is a local `RepoConfigService`, not a suppression. See ## Results.
- Round 3, effort 2, roster `general`, turn cap 120. This round reviewed the fix-pass delta
  `9e44b197e..018ef652d`. `dotnet build tools/dev-cli/dev.cs --no-incremental` succeeds clean. The
  local `RepoConfigService` matches the beta.9 package source except for its header regions and the
  `GetTypeInfo<RepoConfig>()` line.
- Counts: bug 1 open, 2 fixed (M1, M2); suggestion 1 open, 1 fixed (M3); nit 0.
- M4 (bug, open): `OmitPackagedRepoConfigService` in `tools/dev-cli/Directory.Build.targets` removes the
  packaged file for every DevCli version. The rule to delete the copy is only a comment, which breaks the
  AGENTS.md "add a build check" rule. Fix: guard the removal to `4.0.0-beta.9` so another pin hits CS0101.
- M5 (suggestion, open, operator decision): the copy works around a CA2263 defect in DevCli beta.9, a
  package this org owns. Nuru master `edaa9982` still has `GetTypeInfo(typeof(RepoConfig))`, and no Nuru task
  for it was found. Either (a) accept the copy as a temporary exception, with M4's guard and an upstream Nuru
  task, or (b) wait for a Nuru release with the generic overload, as round 1 did for RCS1194.
- Disposition: none yet. Blocked on that operator decision. M4 is moot under (b).
- No misread. The Goal restatement matches the beta.9 task.
- Paths: `review/review-framework.md`, `review/round-3/general.md`, `review/round-3/merged.md`.

## Blocked

2026-10-30: stalemate — operator decision needed: accept the local copy of the Nuru DevCli RepoConfigService (CA2263 workaround) as a temporary exception with a Nuru upstream task, or wait for a Nuru release that uses GetTypeInfo<RepoConfig>()
