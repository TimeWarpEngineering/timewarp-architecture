# Update Aspire 13.5.4 to 13.6 and adopt what fits

## Description

Move the template off Aspire 13.5.4 and onto 13.6, the latest 13.6.x on nuget.org. Then adopt
the 13.6 features that improve the generated app's developer loop. Release notes:
https://devblogs.microsoft.com/aspire/whats-new-aspire-13-6/

Current pins, all 13.5.4 unless noted:

- `Aspire.AppHost.Sdk/13.5.4` in `aspire-app-host.csproj`
- `Directory.Packages.props`:
  - `Aspire.Hosting.Yarp`
  - `Aspire.Hosting.PostgreSQL`
  - `Aspire.Hosting.EntityFrameworkCore` (13.5.4-preview.1.26464.4)
  - `Aspire.Hosting.Testing`
  - the other `Aspire.*` entries in the "Aspire Packages" group

Also check the CLI version, the dashboard SDK and the service-defaults packages.

## Requirements

1. **Bump every Aspire pin together** to the latest 13.6.x, or the matching 13.6 preview for
   packages that only ship previews (EntityFrameworkCore). Pins move forward only, never
   backward or split. If a package was renamed or split, migrate to the new package.
2. **`aspire update` gotchas** (these bit us before):
   - It reflows csproj attributes. Revert any formatting-only churn.
   - It skips `Aspire.Hosting.Testing`. Bump that one by hand.
   - `aspire agent init` writes a telemetry hook into `~/.claude/settings.json`. Do not run it.
   - Prefer editing the pins directly and running `aspire update` only to cross-check.
3. **Adopt, in this PR, when low-risk:**
   - **`WithRepl()`** on the Postgres resource, so the dashboard gets an authenticated psql
     REPL command. The REPL is read-only in historical runs. Development only.
   - **Dashboard persistence** (resource snapshots and telemetry kept after the AppHost stops,
     10 runs, pinning). Confirm the default behavior, and note it in the owning skill. It helps
     postmortems such as today's WASM assertion.
   - **`--launch-profile`** support on `aspire run` / `aspire start`. Check whether `dev run`
     should pass it through.
4. **Evaluate and record, don't adopt:**
   - Prerelease `Aspire.Hosting.Dotnet` / `AddDotnetProject()`: coordinated restore, and
     multi-threaded MSBuild on the .NET 11 SDK. This repo is on .NET 10, and the package is
     prerelease.
   - `WithVolume(env:)` path standardization, for the postgres data volume.
   - `aspire stop --force --volumes`: does it fit a `dev db` flow?

   For each item, write an adopt, skip or later recommendation in Notes and leave it as an open
   question for Steve.
5. **Coordinate with task 261** (browser logs to the dashboard, investigating `WithBrowserLogs`).
   Rebase onto it if it merged first, and re-check that its wiring still works on 13.6. Do not
   duplicate its work.
6. **Keep every regression gate green.** That covers the in-proc lane, the closed-box
   `aspire-tests` (Aspire.Hosting.Testing) and `dev template-smoke`. The template ships the
   AppHost. Keep the template flag regions intact.

## Checklist

- [ ] All Aspire pins on 13.6.x (SDK, hosting packages, Testing, EF preview, service defaults)
- [ ] No csproj churn from `aspire update`; `aspire agent init` not run
- [ ] `WithRepl()` on Postgres (Development); dashboard persistence noted in the skill
- [ ] `--launch-profile` / `dev run` pass-through evaluated
- [ ] Evaluate-only items recorded with recommendations (open questions for Steve)
- [ ] Task 261 wiring re-checked on 13.6 (if merged)
- [ ] Purpose/Design regions reconciled where AppHost code changes
- [ ] Gates: `dev build` 0/0, `dev test` (including aspire-tests), `dev template-smoke`,
      `ganda repo audit`, `dev check-version` if the template version must bump for release
- [ ] Do **not** start the AppHost manually (`dev run`, `aspire run`): it shares the
      maintainer's user secrets. The closed-box test suites are fine. Record the dashboard and
      REPL check as not performed.
- [ ] Implementation review; host `open-pr`

## Session

- Created: 585494 (2026-10-01)

## Notes

- Memory discipline: other workers may be building at the same time. Run builds and tests
  serially and call `dotnet build-server shutdown` before finishing.
- Maintainer check after merge:
  1. Run `dev clean`, then `dev run`.
  2. The dashboard shows the Postgres REPL command.
  3. Stop the AppHost, reopen the dashboard, and confirm the previous run is selectable.

## Results

*(fill when done)*

### How to validate

*(required before done)*
