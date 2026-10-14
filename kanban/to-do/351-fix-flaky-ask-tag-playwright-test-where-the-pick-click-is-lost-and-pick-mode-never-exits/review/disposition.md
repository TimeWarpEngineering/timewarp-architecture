# Disposition — task 351

**Date:** 2026-10-14
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

General review at effort 2 found that the product fix is sound: picks arm before "picking" shows,
cancels are token-named, and state is cleared in order. Two findings were raised and both are fixed on
this task. M1 (suggestion): the JS refusal memory was one slot, so interleaved cancels could let a
late Start arm and end a newer pick; it is now a bounded refused-token set. M2 (nit): rewrapped an
overlong comment.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None.
