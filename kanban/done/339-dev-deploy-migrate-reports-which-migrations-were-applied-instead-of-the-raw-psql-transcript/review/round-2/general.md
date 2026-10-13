# Round 2 — general
**Date:** 2026-10-13
**Scope reviewed:** fix delta for M1 (BuildBundleReport failure branch + test)

## Summary

M1 re-verified fixed: the failure branch returns the applied ids (`applying[..^1]`), then the failing id. With no output
it returns "Failed before any migration was applied". Three test cases cover it, and dev-cli-tests passes 218/0. The fix
delta has no new defects.

## Resolved prior

- M1 — suggestion — fixed
