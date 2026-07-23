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
