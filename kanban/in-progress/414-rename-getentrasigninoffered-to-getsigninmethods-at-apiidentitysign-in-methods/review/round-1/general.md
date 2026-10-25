# Round 1 — general
**Date:** 2026-10-25
**Scope reviewed:** branch `task/414-rename-getentrasigninoffered-to-getsigninmethods-a` vs `master` (merge base a1e95c0ec), product paths outside `kanban/`. That is 20 files, 53 lines added and 53 removed.

## Summary

The change renames the anonymous read `GetEntraSignInOffered` to `GetSignInMethods`. Its folder becomes `features/identity/get-sign-in-methods/` (contracts, handler and co-located tests, all kebab file names). The route moves from `api/identity/entra/offered` to `api/identity/sign-in-methods`, with no alias. The SPA fetch, the Design comments, the `dev deploy` smoke paths and their tests, the `tw-deploy` skill and reference, and the server and SPA tests all use the new name and route. `EntraSignInOffer` keeps its name. The contract has no `[Operation]`, so there is no tool id to rename. The old `GetEntraSignInOffered` comment in `get-site-settings-contracts.cs` said "boolean only", which was out of date. It now says "the method list". The ingress golden is unchanged. That is correct, because `IngressRoutePrefixGenerator` keeps only the top-level `api/identity` prefix. Risk is low.

The implementer's restatement under `## Goal` matches the task (rename, no alias, `EntraSignInOffer` unchanged, golden checked, gates listed). There is no misread.

Checked again in this worktree:
- `rg -n 'GetEntraSignInOffered|entra/offered|get-entra-sign-in-offered|EntraSignInOffered' --glob '!kanban/**' .` returns nothing. That covers the whole tree, not only the five listed roots.
- `dotnet run --file …/get-sign-in-methods/get-sign-in-methods-tests.cs` passed 5/5.
- `./bin/dev build` gave 0 warnings and 0 errors. The worktree was clean afterwards, and `goldens/` has no diff against master.

Not re-run here: integration suites, template-smoke, lint-skills and repo audit. The implementer reported those in Results.

## Issues

None.
