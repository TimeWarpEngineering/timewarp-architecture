# Round 1 — general
**Date:** 2026-09-11
**Scope reviewed:** branch task/053-006-emit-less-per-contract-mixin-members vs origin/master (product commit 79f0ec11)

## Summary

`ContractsMixinGenerator` now emits leaner per-contract mixins: static routes return `RouteTemplate`, parameterized routes forward `GetRoute()` to `GetRoute(...)` without duplicating the format string, and `GetAuthQueryParameters` is gated to query-string contracts (`IQueryStringRouteProvider` and/or `[OpenDataQueryParameters]`) while `[AuthApiRequest]` still always emits `UserId`. Hosted call sites re-verify cleanly — GetRoles / ListPrincipals / GetCredentials keep the helper; CreateRole and GetRole stay on the manual `IAuthApiRequest` form — and skill/how-to text matches the new emit rules. Risk is low; coverage of the shrink behaviors is adequate.

## Issues

