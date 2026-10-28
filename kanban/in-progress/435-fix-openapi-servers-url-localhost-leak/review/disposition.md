# Disposition — task 435

**Date:** 2026-10-29
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 raised M1 (bug: platform package fix without a version bump) and M2 (suggestion: a third
copy of the forwarded-origin rule). The fix pass bumped every version and pin to `2.0.0-beta.29` and
added one `PublicRequestOrigin` resolver in `foundation-server`, which OpenAPI, mail, link previews,
and identity cookie forwarding now share. Round 2 re-ran the affected test classes (4 + 8 + 9 + 9
passed) and `dev check-version` (pass), and found no new defects. Every raised finding is `fixed`.

## Exception log (if accepted-exceptions)

None.

## Escalations

None.
