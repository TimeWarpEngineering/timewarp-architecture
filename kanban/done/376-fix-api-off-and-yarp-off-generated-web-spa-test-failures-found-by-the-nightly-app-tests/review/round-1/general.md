# Round 1 — general
**Date:** 2026-10-16
**Scope reviewed:** branch vs master: `tests/container-apps/web/web-spa-integration-tests/` (global-usings.cs, infrastructure/aspire-spa-test-application.cs, infrastructure/base-test.cs, csproj)

## Summary

The yarp-off fix puts the `ingress` health wait inside `#if(yarp)` and picks the api-server client base URL from a flag-selected resource name (ingress / api-server / web-server). The csproj defines `yarp` (TWA0010). The api-off fix deletes the test-only `PermittedAuthenticationStateProvider` and uses the product `MockAuthenticationRegistration` in every variant. That is the right side to fix: the product mock auth ships whatever the flags, so the test assumption was the defect. Flag regions are intact. The implementer reports that a nightly dispatched on this branch passed. Risk is low. One maintainability issue: comments still describe the yarp-only wiring.

Goal restatement vs original task: consistent; no misread.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/infrastructure/aspire-spa-test-application.cs:2
- Description: The Purpose region, Design region, class XML summary, and the AddHttpClient comment still say the base address is always the Aspire ingress and that requests go "through YARP". After this change that is false in the yarp-off variant. Generated apps would ship misleading agent context.
- Suggestion: Reword them to name the flag-selected resource (BaseUrlResourceName). Point at the Design region and do not restate the ingress assumption.
- Status: open
