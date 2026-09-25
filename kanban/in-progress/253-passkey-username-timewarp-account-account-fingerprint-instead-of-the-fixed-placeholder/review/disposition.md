# Disposition — task 253

**Date:** 2026-09-25
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (2026-09-24, effort-1 general review of commit 188466a1) found no bugs and one suggestion
(M1: AddPasskey does not refuse new-account challenges), initially accepted as wontfix. Steve reversed
that on 2026-09-25 — the template has no deployed pre-change clients, so the cached-bundle rationale
does not hold. Round 2 fixed M1: AddPasskey refuses a new-account challenge with 400 ChallengeInvalid
before writing a credential. A passkey's stored username now always names the account it is attached to.

## Exception log (if accepted-exceptions)

- None (M1 fixed in round 2).

## Escalations

- None.
