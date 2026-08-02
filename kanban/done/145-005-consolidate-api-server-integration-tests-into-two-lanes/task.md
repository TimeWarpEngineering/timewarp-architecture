# Consolidate api-server-integration-tests into two lanes

## Description

Stop paying both boot paths in one assembly (parent 145; findings §4). Split in-proc vs closed-box.

## Checklist

- [x] Lane triage documented (`triage.md`); weather Fixie twins deleted
- [x] In-proc co-located (5 tests); closed-box OpenAPI Jaribu+Aspire (1 test)
- [x] Fixie wiring removed; wall-clock before/after recorded
- [x] dev build 0/0; audit pass; review clean (template-smoke count bumped; full smoke not re-run)

## Session

- 2026-08-02 orchestration: implement + verify + disposition clean

## Results

### Summary

Assembly is **closed-box only** (OpenAPI process-isolation). In-proc weather endpoint/handler/validator
live solely in co-located `get-weather-forecasts-tests.cs` (no Fixie duplicate).

### Wall-clock (145-008)

| | Before (mixed Fixie) | After |
|--|----------------------|--------|
| Suite contents | 7 pass + 1 skip (in-proc + Aspire) | 1 pass (OpenAPI only) |
| Wall | ~**56s** | ~**32s** |
| Co-located weather | (partial twin) | **5/5** standalone / api-jaribu |

### Verification

| Gate | Result |
|------|--------|
| weather runfile | 5/5 |
| api-jaribu-tests | 5/5 |
| api-server-integration-tests MTP | 1/1 |
| solution build | 0/0 |
| ganda repo audit | PASS |

### Review

Effort 1, **clean** — `review/`
