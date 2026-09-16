# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** branch `task/226-template-smoke-assert-zero-failures-and-a-minimum` vs `origin/master` — `tools/dev-cli` (helper + harness + surrounding `template-smoke-command` Design), `tests/tools/dev-cli-tests`, `skills/tw-feature-placement`; confirmed no `documentation/` tree / `releasing.md`

## Summary

Product commit `f47dfca5` correctly replaces tier-3 exact `ExpectedSucceeded` equality with an exit-0 precondition plus a Terminal-free parse/decide helper: `failed:` required and must be 0, `succeeded >= MinimumSucceeded` (web 180 / api 9 / common 3), `total == succeeded + skipped` (missing `skipped:` → 0), unparsable summary fails, and succeeded above 2× floor warns without failing. Required Decide/TryParse cases are covered and pass locally (9 + 5); skills that used to tell authors to bump exact counts now describe the floor; no `ExpectedSucceeded` call sites remain. Low risk; no defects found against Requirements.

## Issues

