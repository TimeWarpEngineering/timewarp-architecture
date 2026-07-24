# Fix Release-mode SPA-shell 500 flake in GetRootThroughIngressReturnsSpaShell

## Description

Found 2026-07-23 during 107's full-dev-test gate: `GetRootThroughIngressReturnsSpaShell`
(aspire-tests, task 117) fails with a 500 under RELEASE-mode full `dev test` — and the build
agent proved it PRE-EXISTING by stashing all 107 changes and reproducing identically on clean
master. Debug-mode runs and the targeted aspire-tests suite pass. Suspects to investigate:
Release-mode static web assets / compressed asset resolution for the SPA shell through the
ingress, or Release DCP/proxy timing. Diagnose with the web-server console logs at failure
(the 500 body/stack), then fix root cause — do not weaken the smoke assert.

## Checklist

- [ ] Reproduce under Release full dev test; capture web-server logs for the 500
- [ ] Root-cause and fix (asset pipeline vs timing)
- [ ] 3 consecutive Release-mode full dev test runs green

## Notes

Related: 116 (Rebuild StaticWebAssets/TS ordering) may share a root cause in the wwwroot/js pipeline; check first.

## Results (2026-07-24)

Root cause found — **deterministic Release failure, not a flake** (the "flake" impression came
from targeted runs defaulting to Debug while CI/full dev test run Release; unrelated to 116's
asset pipeline):

- web-server prerenders the SPA shell via `Web.Spa.Program.ConfigureServices`; web-spa
  registered `ReduxDevToolsInterop` (UseReduxDevTools) under `#if DEBUG`, but Routes.razor
  rendered `<ReduxDevTools/>` (which injects that service) unconditionally. Debug: both on.
  Release: component present, service missing → EVERY SPA-shell request 500
  (`InvalidOperationException: Cannot provide a value for property 'ReduxDevToolsInterop'`).
  Any Release deployment shipped a dead shell — worse than a test problem.
- Fix: the previously-defined-but-never-used `ReduxDevToolsEnabled` symbol is now the single
  truth: defined only in web-spa's Debug PropertyGroup; all four sites (registration, markup
  via `@if`, inject, InitAsync) compile under it. `#if DEBUG` no longer appears in the
  ReduxDevTools path.

Verification: dev build 0/0; GetRootThroughIngressReturnsSpaShell 3 consecutive Release runs
green; full aspire-tests 7/7 in BOTH Release and Debug; dev template-smoke SUCCEEDED
(SmokeDefault + SmokeNoPostgres).
