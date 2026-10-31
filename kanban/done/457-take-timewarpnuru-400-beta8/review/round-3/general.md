# Round 3 — general
**Date:** 2026-10-30
**Scope reviewed:** fix-pass delta `9e44b197e..018ef652d` (round-2 `base.sha` to HEAD). Product paths:
`Directory.Packages.props`, `tools/dev-cli/Directory.Build.props`, `tools/dev-cli/Directory.Build.targets` (new),
`tools/dev-cli/services/repo-config-service.cs` (new). Kitchen: `task.md`.

## Summary

The fix pass moves both Nuru pins to `4.0.0-beta.9` and removes `RCS1194` and its comment from the dev
CLI `NoWarn`. That resolves M1 and M2. Goal, Results, and Checklist now describe beta.9, which resolves M3.
DevCli beta.9 content adds a call to `JsonSerializerOptions.GetTypeInfo(Type)` in
`content/any/services/repo-config-service.cs:60`. beta.8 did not have that call, and with warnings as
errors CA2263 now fails the runfile. The implementer did not add a `NoWarn`. Instead they copied the whole packaged
`RepoConfigService` into `tools/dev-cli/services/`, changed it to `GetTypeInfo<RepoConfig>()`, and wrote an
MSBuild target that removes the packaged Compile item. I diffed the copy against the beta.9 package
source: apart from the header regions and that one line, it is identical. `dotnet build tools/dev-cli/dev.cs
--no-incremental` succeeds with no warnings or errors. The fix is real, not a suppression. Its
problem is that nothing ties it to the pin. The local Nuru master (`edaa9982`) still has the
non-generic call, and no Nuru task for it was found.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/Directory.Build.targets:7
- Description: `OmitPackagedRepoConfigService` removes the packaged `repo-config-service.cs` for **every**
  DevCli version. The rule to delete the copy "when the pin uses the generic overload" lives only in
  comments, in the targets file and in the Design region. That is agreement by memory, which the AGENTS.md
  hard rule "When two things must agree, generate one from the other or add a build check" forbids.
  When the pin moves to beta.10 or later, any upstream change to `RepoConfigService` behavior (new
  config keys, parse rules, file location) is dropped without a warning. Only a change to the
  `IRepoConfigService` signature would break the build.
- Suggestion: Make the target apply only to the version it was written for, for example by also requiring
  `%(Identity)` to contain `4.0.0-beta.9`, or by comparing against the `TimeWarp.Nuru.DevCli` `PackageVersion`.
  On any other pin, both types then compile, and CS0101 (duplicate `DevCli.RepoConfigService`) forces someone
  to revisit the copy. Alternatively, add a build check that fails when the pin differs from the version the copy targets.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/services/repo-config-service.cs:1
- Description: The fix keeps a local copy of first-party package internals to work around a defect in
  TimeWarp.Nuru.DevCli, a package this org owns and releases. In round 1 the operator turned down a local
  workaround (the RCS1194 `NoWarn`) and waited for the upstream Nuru fix, which shipped as beta.9 the same day.
  This is the same kind of decision. Nuru master still has the CA2263 call, and no upstream task was found.
  A worker spec cannot authorize cross-repo work, so the implementer was right not to open one.
- Suggestion: Operator decision. Either (a) accept the copy as a temporary documented exception, with
  Issue 1's version guard and an upstream Nuru task to switch to `GetTypeInfo<RepoConfig>()`, or
  (b) wait for a Nuru beta.10 that uses the generic overload, then remove the copy and the target.
- Status: open
