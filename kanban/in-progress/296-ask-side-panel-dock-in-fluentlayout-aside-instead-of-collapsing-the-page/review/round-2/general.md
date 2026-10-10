# Round 2 — general
**Date:** 2026-10-10
**Scope reviewed:** fix delta for M1 (AgentAsk.razor.css, AskAnswerBar.razor.css, ask-surface-playwright-tests.cs) and refreshed screenshots.

## Summary

M1 re-verified: header and message icons render at 20px in `ask-docked-1280.png`, and the new
icon-size assertion passes in the Ask surface Playwright class. The rule is scoped to the Ask
action containers and does not touch the global reset. M2 remains wontfix. No new findings.

## Issues

<!-- none -->
