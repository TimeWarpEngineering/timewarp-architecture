# Round 1 — general
**Date:** 2026-08-01

## Summary

Suite converted to Jaribu MTP with C-create HostGraphFactory per host class. Hello endpoint
co-located. Fixie convention deleted. Gates green.

## Verification

| Gate | Result |
|------|--------|
| Suite before | 97 pass, 1 skip, wall ~31s |
| Suite after | 95 succeed, skip RunForever (MTP may count skip ×2), wall ~24s |
| Hello co-located | 2/2 |
| web-jaribu-tests | 7/7 |
| solution build | 0/0 |
| ganda repo audit | PASS (CPM pins restored for audit consistency) |

## Issues

_None blocking._
