# Round 2 — general
**Date:** 2026-10-10
**Scope reviewed:** round-1 fix delta (listener order, ChatProbeCompleted removal, Playwright re-probe wait), plus re-verification of M1–M4

## Summary

M1: `LoadChatConfiguration` is now the listener's last await. Verified that the identity branch runs first on both sign-in and sign-out, and that the Design region matches the code. M3: the Playwright wait is bounded (30 s), polls the recorded configuration responses and does not throw, so the old code's failure is still reported. 3 consecutive runs: 3/3 passed. M4: no reference to `ChatProbeCompleted` remains in source or tests. `dev build` 0/0, `AgentAskReadiness_Should_` 9/9, `ganda repo audit` passes. No new defects in the delta.

## Issues

<!-- none -->
