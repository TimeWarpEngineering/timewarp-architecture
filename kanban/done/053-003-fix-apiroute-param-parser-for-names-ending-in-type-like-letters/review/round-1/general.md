# Round 1 — general
**Date:** 2026-09-09
**Scope reviewed:** branch `task/053-003-fix-apiroute-param-parser-for-names-ending-in-type` vs `origin/master` (product commit ea744171 plus Results 91fb3f9f)

## Summary

`ContractsMixinGenerator` now requires an explicit colon before a route constraint, so bare
`{Date}` / `{LocationId}` (and siblings) keep the full identifier and default to `string`. Type
mapping parity is preserved via `MapConstraintToClrType`; skill, how-to, and 2.0.0-beta.17 release
note document the same grammar. Risk is low: focused tokenizer fix with host-free generator
regression coverage. Claims re-checked — regex, tests (6/6), docs/code agreement, version pins
already at 2.0.0-beta.17 vs published `v2.0.0-beta.16`, and PageSourceGenerator sibling left in
`TimeWarp.Architecture.Generators` as stated out of scope.

## Issues
