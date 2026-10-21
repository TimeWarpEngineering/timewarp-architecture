# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** branch vs master (f78d8b41e), all product paths in the framework

## Summary

The change makes `<AspireVersion>` in `Directory.Packages.props` the one Aspire version. The AppHost
SDK (`<Sdk Name="Aspire.AppHost.Sdk" Version="$(AspireVersion)" />`), every `Aspire.*` pin, and
the CI `Aspire.Cli` install read it. `dev run`, `dev publish`, `dev deploy`, and `dev db nuke` warn
when the CLI is older, and the hard 13.6 minimums still refuse. The goal and checklist are met.

Re-verified here:
- dev-cli-tests: 303 passed, 0 failed (Release).
- `dotnet msbuild aspire-app-host.csproj -getProperty`: `IsAspireHost=true`, `AspireVersion=13.6.2`,
  `AspireHostingSDKVersion=13.6.2`, so the SDK resolves from the property.
- dev-cli-tests is in the `fast` test group (outside `tests/container-apps/`), so CI runs
  `RepoFiles_Should_ShareOneAspireVersion`.
- `tests/tools/dev-cli-tests/**` is excluded from the template (`.template.config/template.json`), so the
  test's `workflow.yml` read does not ship to generated apps.
- No stray `13.6.2` literal is left in a pin, the SDK, or the workflow.

Risk is low. Two weaknesses are worth fixing on this change. Both are in the new pure helpers.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/services/aspire-run.cs:266
- Description: `WorkflowCliInstallError` passes whenever the text `<AspireVersion>` appears anywhere
  in `workflow.yml`. The new comment at `.github/workflows/workflow.yml:227` contains that text, so
  the second condition always holds. Example: replace the step with
  `dotnet tool install --global Aspire.Cli` (no `--version`), or with `--version ${{ vars.ASPIRE_CLI }}`.
  CI installs a CLI that does not come from the repo, and the agreement check still passes. The task
  asks for a check that fails "when … the workflow install stops agreeing with that property".
- Suggestion: Tie the check to the install command itself. Require the `Aspire.Cli` install to pass
  `--version "$version"` (or whatever variable is used), and require that variable to be assigned
  from the `sed` over `Directory.Packages.props`. Or skip comment lines (`#…`) before looking for
  `<AspireVersion>`. Add a test where the workflow mentions `<AspireVersion>` only in a comment and
  installs without `--version`. That test must fail.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/services/aspire-run.cs:158
- Description: `ParseRepoAspireVersion` uses `Version.TryParse` on the whole value, so a prerelease
  `<AspireVersion>` such as `14.0.0-preview.1.26600.1` returns null. MSBuild and NuGet accept that
  value for the SDK and the pins. `AspireCli.ValidateVersionAsync` then sets `RepoVersionError`,
  and `BlockingError` makes that fatal for every caller (aspire-cli.cs:26), including plain `dev run`
  with no `--launch-profile`. That `dev run` ran with any CLI before this change. The message says
  "Could not read <AspireVersion>", but the property is present. This template takes Aspire previews
  and RCs early (it already pins `-preview` Aspire packages), so moving to a preview train would
  block every Aspire verb, and the message would not say why.
- Suggestion: Parse the numeric core before the first `-`/`+`, the same way `ParseCliVersion` does,
  and compare on that. Or, when the value is present but does not parse, name the value in the
  message. Add a test with a prerelease `<AspireVersion>`.
- Status: open
