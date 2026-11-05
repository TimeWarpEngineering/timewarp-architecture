# Bump Scalar.AspNetCore to 2.19.0 and fix the FastEndpoints pin comment

## Description

Scalar.AspNetCore 2.19.0 reached nuget.org on 2026-11-05 after task 480's check. Bump the pin in
`Directory.Packages.props`. While there, correct the comment above the FastEndpoints pin: it says
FastEndpoints depends on Microsoft.AspNetCore.OpenApi, but that dependency belongs to
FastEndpoints.OpenApi (task 480 review nit M1). Either name the right package or drop the comment
if it no longer earns its lines.

## Requirements

- Scalar.AspNetCore 2.18.7 -> 2.19.0. Read the 2.19.0 notes; the API reference page is wired in
  web-server, so load it in the web-spa integration or Playwright suite if one covers it.
- Fix or remove the FastEndpoints comment. No other pin changes.
- `dev build`, `dev test`, `dev template-smoke`, `dev check-version` handled as in task 480.

## Goal (implementer restatement)

Move the Scalar.AspNetCore pin in `Directory.Packages.props` from 2.18.7 to 2.19.0, and make the
comment above the FastEndpoints pins name the package that really depends on
Microsoft.AspNetCore.OpenApi (FastEndpoints.OpenApi, not FastEndpoints). Change no other pins.

Acceptance:

- `Scalar.AspNetCore` is pinned at 2.19.0. The 2.19.0 notes were read for breaking changes.
- The FastEndpoints comment is correct or gone.
- `dev build` has 0 warnings. `dev test` passes, including the web-server integration tests that
  load `/scalar/v1`. `dev template-smoke` passes. `dev check-version` was run and its result
  handled the way task 480 handled it.
- `ganda repo audit` passes.

## Checklist

- [x] Scalar pin bumped
- [x] FastEndpoints comment fixed or removed
- [x] Gates green; check-version handled

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 26df18c6-b179-46fc-93eb-bcc49325293c, max-turns 500, Done — 2026-11-05T13:56:06Z

## Results

- `Directory.Packages.props`: `Scalar.AspNetCore` 2.18.7 -> 2.19.0. The 2.19.0 changelog lists one
  minor, additive change: per-document authentication overrides and an optional security-scheme
  allowlist (scalar#10728). It also bundles `@scalar/api-reference` 1.77.0. Nothing is breaking, and
  `CommonServerModule.UseScalarApiReference` needs no change. The 2.19.0 nuspec has no package
  dependencies.
- FastEndpoints comment fixed. It now names FastEndpoints.OpenApi as the package that depends on
  Microsoft.AspNetCore.OpenApi >= 11.0.0-rc.3.26522.148. I checked both 9.0.0-beta.7 nuspecs:
  FastEndpoints' net11.0 group depends only on FastEndpoints.Attributes, JobQueues, Messaging, and
  FluentValidation. The Microsoft.OpenApi floor note is still accurate, so it stays. No other pins
  changed.
- `dev check-version`: source is 2.0.0-beta.32 and nuget.org has 2.0.0-beta.31. Not yet published,
  so no `<Version>` bump (same as task 480).
- `dev build`: 0 warnings, 0 errors.
- `dev test`: 23 of 23 projects passed. That includes web-server-integration-tests, which holds the
  `/scalar/v1` page tests (`ScalarPage_Should_Load_The_Document`,
  `ScalarPage_Should_NotEmbedALoopbackServer`), and web-spa-playwright-tests (37/37).
- `dev template-smoke`: succeeded (default variant). No template symbol or `#if` change, so no
  `--matrix` run.
- `ganda repo audit`: passes (exit 0), with the same 2 advisory warnings as task 480, neither
  caused by this task. kanban-landed lists other tasks' claims that have no worktree.
  source-file-line-cap lists EF migration designers and a done-task research runfile.

### How to validate

Smoke: `./bin/dev build && ./bin/dev test`. Or run the web-server integration tests alone and
request `/scalar/v1` on a running web-server.

Expect: build has 0 warnings. `ScalarPage_Should_Load_The_Document` and
`ScalarPage_Should_NotEmbedALoopbackServer` pass. `/scalar/v1` returns 200 with the Scalar page
pointing at `openapi/v1.json`. `grep Scalar.AspNetCore Directory.Packages.props` shows
`Version="2.19.0"`.

## Notes

Steve 2026-11-05: "bump it and fix/remove the FastEndpoints comment while at it".
