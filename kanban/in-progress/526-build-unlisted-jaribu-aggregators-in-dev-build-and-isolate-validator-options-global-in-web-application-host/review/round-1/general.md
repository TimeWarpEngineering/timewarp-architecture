# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/526-build-unlisted-jaribu-aggregators-in-dev-build-and` vs `master` (`master...HEAD`), checked against the original task and `## Goal (implementer restatement)`.

## Summary

`dev build` now builds the `.slnx` and then each `tests/**/*.csproj` the solution does not list, one at a time, using the same glob and path diff as `dev test`. `--clean` cleans those projects too. `WebApplicationHost<TProgram>` enters a ref-counted `ValidatorOptionsGuard` before `CreateBuilder` and exits it from `DisposeAsyncCore` and from the constructor's failure path, so FluentValidation's static property-name and display-name resolvers return to their pre-host delegates. The restatement matches the task. The recorded `## Results` cover the build, the new unit and regression tests, both Jaribu aggregators, template-smoke, and the audit rerun after the `NoWarn` and executable-bit fixes. No product defect turned up in the diff or the call sites around it.

## Issues

<!-- None. -->
