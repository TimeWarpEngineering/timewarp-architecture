# Disposition — task 466

**Date:** 2026-11-02
**Outcome:** clean
**Rounds:** 3
**Final open count:** 0

## Summary

Round 1 (effort 3, general) raised five findings: 1 bug, 3 suggestions, and 1 nit. The fix in
5a60e94c5 fixed all five, and round 2 (effort 2, general) verified them. Steve then decided that
agents are not Members. Commit d0a9d35d9 implemented that in `EffectiveRolesResolver`: a roleless
human or a missing row is Member, and a roleless agent or service has no role. Round 3 (effort 3,
general) reviewed that delta and found nothing new. The reviewer re-ran `dev build` (0/0), the
resolver runfile (17/17), set-role-permissions (15/15), get-principal (10/10), list-principals
(13/13), the issuance class `AgentScopeApproval_` (5/5), and the full web-server integration
suite (435 passed, 1 skipped, 0 failed).

Final counts: bug 1 fixed, suggestion 3 fixed, nit 1 fixed, 0 wontfix.

Ledger: `review/round-3/merged.md`. Earlier rounds: `review/round-2/merged.md` and
`review/round-1/merged.md`.

## Exception log (if accepted-exceptions)

None.

## Escalations

- Round 2 left Steve one call under M3: whether Member's agent scopes should reach agents. He
  decided on 2026-11-02 that agents are not Members. d0a9d35d9 implements that, and round 3
  verified it. Nothing is left open.
