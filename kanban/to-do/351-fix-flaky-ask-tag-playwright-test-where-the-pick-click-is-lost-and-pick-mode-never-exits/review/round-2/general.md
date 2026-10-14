# Round 2 — general
**Date:** 2026-10-14
**Scope reviewed:** the fix delta for M1 and M2 (ask-tag.ts refusal set, AgentAsk.razor comment wrap).

## Summary

M1 is fixed. A cancel that reaches the browser before its Start is remembered per token, so any number
of interleaved cancels cannot un-refuse an earlier pick. The set is bounded: a refused Start deletes its
entry, and a cancel for a started pick never adds one. Cancelling an armed pick still finishes it.
Cancelling an orphan from `PickAsync`'s stale `armed()` path finds it `active` and adds nothing.
Behavior is unchanged for the single-pick path the tests cover (`dev build` 0/0, Playwright 3/3).
M2 is fixed. No new issues.

## Issues

<!-- none -->
