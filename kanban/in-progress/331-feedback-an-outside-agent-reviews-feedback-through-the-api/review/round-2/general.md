# Round 2 — general
**Date:** 2026-11-02
**Scope reviewed:** the fix delta 24a113195..37f0bb8e5 (round-1 base to HEAD). Product files:
`source/container-apps/web/platform/identity-host/open-api-agent-reviewer-guide-server.cs` and
`tests/container-apps/web/web-server-integration-tests/open-api-agent-reviewer-guide-tests.cs`.
Kitchen: `task.md` Results and Session.

## Summary

The fix pass resolves both round-1 findings. M1 needed no product change: `./bin/dev check-version`
reports source `2.0.0-beta.30` is one prerelease ahead of NuGet `2.0.0-beta.29`, so beta.30 is
unpublished and the release cut from master after this merge carries the generator that reads
contract remarks. Results record that outcome. M2: each ceremony step in the `JWTBearerAuth` guide
now reads `<kebab id> (<METHOD> <path>)`. The method comes from `GetHttpVerb()` and the path from
`GetRoute()` on the contract, which the `[ApiRoute]` source generator emits as `RouteTemplate`
for these four static routes. The document test asserts that each path is a key under `paths`
with that method.

Re-verified in this worktree:
- `./bin/dev check-version`: exit 0, source beta.30, NuGet beta.29, "safe to release".
- `./bin/dev build`: 0 warnings, 0 errors.
- `dotnet test -c Release -- --filter-class OpenApiAgentReviewerGuide_` from
  `tests/container-apps/web/web-server-integration-tests`: 3/3 passed.

Checked and fine:
- The four routes (`api/identity/agent/register/options`, `api/identity/agent/register`,
  `api/identity/agent/token/options`, `api/identity/agent/token`) are static, so the
  parameterless `GetRoute()` returns the template with no placeholder or query string.
- Adding the leading `/` matches how the document keys `paths`; the test's `paths` lookup proves it.
- `new()` on the command types compiles, so none of them has `required` members.
- The Design regions in both files are updated to say where method and path come from.
- The test builds the expected token with the same helper shape as production, but the `paths`
  and method assertions check it against the document independently, so the test still catches
  a route the endpoint does not serve.

## Issues

None in the delta.
