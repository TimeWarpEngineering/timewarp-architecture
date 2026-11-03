# Round 2 — general
**Date:** 2026-11-04
**Scope reviewed:** fix delta `8edf0ea9a..406fad697` (review round 1 fixes): 14 product files,
comments and whitespace only, plus round-1 kitchen artifacts. Prior IDs M1–M5 re-verified against
the post-fix tree.

## Summary

The fix commit changes only comments and blank lines. It re-points Design/Purpose text from the
removed `Program.ConfigureSettings` / `Program.ConfigureAuthentication` / `AddStateDevToolsExport`
names to the new concern types, and it adds three blank lines after `namespace`. No code changed,
so there is no behaviour risk. `./bin/dev build` in this worktree: 0 warnings, 0 errors.

## Re-verification of prior findings

- M1 (fixed, confirmed): a grep of source, tests and skills finds no `Program.ConfigureSettings` or
  `Program.ConfigureAuthentication`. The 8 files plus sign-in-options-validator-application.cs now
  name `WebServerSettings.ConfigureServices` or `WebServerAuthentication`. Both members exist.
  The remaining `ConfigureSettings` hits are web-spa's and api-server's own private methods, which
  are correct.
- M2 (fixed, confirmed): web-server-state-dev-tools.cs Design says `AddExport`, and
  web-server-authentication.cs:177 says `AgentTokenOptions (WebServerSettings)`.
- M3 (fixed, confirmed): postgres-resources.cs:107 and :130 name PublishTarget's Design region in
  publish-target.cs. Two other "see the Design region" pointers remain (postgres-resources.cs:257,
  ingress-resource.cs:325). Both point at their own file's Design region, and the reasoning is there
  (task 270 wait-edge notes at :28–36; WebServerApiRoutePrefixes at :9).
- M4 (wontfix, rationale accepted): the combination (api, grpc and web all off) is not a smoke-matrix
  variant. The yarp-on form already failed on master for an unrelated reason (IDE0005). Supporting
  that combination is a template flag-set decision that needs its own matrix variant. It is not
  part of a behaviour-preserving refactor. The rationale is recorded in round-1/merged.md.
- M5 (fixed, confirmed): blank line after `namespace …;` in all three files.

Other surviving pointers checked: web-spa program.cs:45 names `Web.Server.Program.ConfigureEndpoints`
and mock-authentication-registration.cs:21 names `ResolveRealEnvironmentName`. Both still exist on
web-server `Program` (program.cs:41, :52, :101), so neither is stale.

## Issues

None. One observation that is not a finding: web-authn-options-application.cs:10 is now a
142-character comment line, where the rest of the file wraps near 100. No rule in .editorconfig
or the analyzers covers comment width, so it is left to the implementer's discretion.
