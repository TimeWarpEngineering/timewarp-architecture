# Round 2 — general
**Date:** 2026-08-06
**Scope reviewed:** post-fix Home CTA (M1) after c4c90779 + uncommitted HomePage fix

## Summary

Re-verified M1: Home anonymous CTA is a single `FluentButton` with `OnClick="GoToLoginAsync"`; code-behind uses `NoSubRouteState.ChangeRoute(LoginPage.GetPageUrl())` and `[CrossSliceReference(typeof(LoginPage), …)]` so TWA0009 is opt-in, not silent. `./bin/dev build` 0/0. No new defects on the fix delta.

## Issues

None.
