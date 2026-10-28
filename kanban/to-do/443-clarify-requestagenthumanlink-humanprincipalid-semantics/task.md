# Clarify RequestAgentHumanLink humanPrincipalId semantics

## Description

`RequestAgentHumanLink_Command.humanPrincipalId` is optional (non-required
uuid). Callers cannot tell from the spec alone what omitting it means
(self-link? error?) vs the successful humanPrincipalId-pinned flow used on
2026-10-28.

## Checklist

- [ ] Document omitting vs providing humanPrincipalId
- [ ] Mark required if pinning is the contract
- [ ] Add description + example; align auth.md/llms.txt wording

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
