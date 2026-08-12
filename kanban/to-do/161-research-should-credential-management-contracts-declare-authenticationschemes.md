# Research: should credential-management contracts declare AuthenticationSchemes

## Description

Spun out of task 158. That task fixed the missing-`AuthSchemes` bug by adding
`[EndpointAuthorize(AuthenticationSchemes = …)]` to the 7 admin roles/principals contracts whose
server policies declare `identity-session` + `mock-identity-session` (commit `6442b605`). The
**credential-management** contracts (add-agent-key, add-passkey, get-credentials,
revoke-credential — policy `CredentialManagementDefaults.Policy`, dual
`identity-session`/`agent-token` schemes, no mock scheme) and the **agent-token-only** endpoints
were deliberately left WITHOUT scheme declarations: no proven failure, and their dual/restricted
scheme lists are load-bearing security design (see
`source/container-apps/web/platform/identity-host/credential-management-defaults-server.cs` and
`agent-token-defaults-server.cs` Design regions).

Open question (maintainer does not know the answer yet — that's what this task determines):
should ALL hosted `[EndpointAuthorize]` contracts declare their schemes explicitly for
consistency, or is policy-level `AddAuthenticationSchemes` sufficient (and safer) for the
non-mock endpoints?

## Requirements — questions to answer

- **Mechanism:** when a policy carries `AddAuthenticationSchemes(...)`, does the FastEndpoints
  `Policies(...)`-only emission invoke those handlers correctly at request time (i.e. was the 158
  bug specific to how `CanViewRolesPage`/`CanViewPrincipalsPage` were defined, or does EVERY
  Policies-only endpoint silently skip non-default schemes)? Determine precisely why the roles
  endpoints failed while (apparently) credential-management works — or prove credential-management
  is broken too and nobody noticed (test coverage gap?).
- **Coverage check:** do integration tests actually exercise agent-token auth through the
  credential-management endpoints closed-box/in-proc? If none do, the "works today" assumption is
  untested.
- **Risk analysis:** does adding endpoint-level `AuthSchemes` to dual-scheme endpoints change any
  behavior vs the policy-level list (ordering, challenge scheme selection, forbid behavior)?
- **Recommendation:** one of (a) declare schemes on all hosted contracts + consider a TWA analyzer
  enforcing endpoint/policy scheme agreement (prefer-analyzers-over-convention rule), (b) leave
  policy-level as the SSOT and REMOVE the redundancy risk by documenting the convention, or
  (c) hybrid with a documented litmus. Maintainer decides on the findings.

## Checklist

- [ ] Answer the mechanism question with a falsifiable test (same rigor as 158's investigation)
- [ ] Audit test coverage of agent-token + credential-management auth paths
- [ ] Risk analysis of endpoint-level vs policy-level scheme lists
- [ ] Written recommendation with options; maintainer decision recorded
- [ ] Results (research task: findings + decision are the deliverable; follow-up implement task
      only if the decision requires code)

## Notes

- Origin: task 158 Results ("Out of scope" section) and its Grok root-cause investigation.
- The `AuthenticationSchemeNames` constants class
  (`source/container-apps/web/features/identity/authentication-scheme-names-contracts.cs`)
  already includes `AgentToken` for use if the answer is "declare everywhere".

## Session

- Created: Claude (2026-08-05), spun out of task 158 per maintainer direction (answer unknown —
  research first, don't assume).
