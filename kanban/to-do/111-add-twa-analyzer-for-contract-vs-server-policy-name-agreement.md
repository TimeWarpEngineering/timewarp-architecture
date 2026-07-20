# Add TWA analyzer for contract-vs-server policy-name agreement

## Description

Origin: 110 review round-1 M2 (general). Policy name `"identity-session-authenticated"` now lives
as six comment-coordinated string literals across two assemblies — the `[EndpointAuthorize(Policy =
"identity-session-authenticated")]` literal repeated on five web-contracts contracts
(create/update/delete/get-role, get-roles) plus the `IdentitySessionDefaults.AuthenticatedPolicy`
constant in web-server — agreeing only by convention (a `// matches
IdentitySessionDefaults.AuthenticatedPolicy` comment, not a compiler-checked reference). web-contracts
cannot reference web-server's constant directly (wrong dependency direction), which is why this
pattern exists at all — it also already applies to `get-agent-identity.cs`'s
`[EndpointAuthorize(Policy = "agent-scope:identity:read")] // matches
AgentTokenDefaults.IdentityReadPolicy`.

Fail-closed + the current test suite catch a *drifted* literal today (a policy name that stops
matching means the endpoint 401s where it shouldn't, or vice versa — visible), but nothing catches
it at build time, and the coupling is easy to miss when adding new policies. A prefer-analyzers
directive candidate (see AGENTS.md "Prefer analyzers/source generators over convention-by-memory"):
generate one side from the other, or add a build-time agreement check between the contract-side
string literals and the server-side policy-name constants.

## Checklist

- [ ] Decide the mechanism: new TWA analyzer cross-referencing contract `[EndpointAuthorize(Policy =
      "...")]` literals against known server policy-name constants, vs. a source-generation approach
      that derives one side from the other
- [ ] Cover all three known instances: the five roles contracts'
      `"identity-session-authenticated"` vs. `IdentitySessionDefaults.AuthenticatedPolicy`,
      `get-agent-identity.cs`'s `"agent-scope:identity:read"` vs.
      `AgentTokenDefaults.IdentityReadPolicy`, and (104-005 review round-1 M2) the four
      credential-management contracts' (`get-credentials.cs`, `revoke-credential.cs`,
      `add-passkey.cs`, `add-agent-key.cs`) `"credential-management"` literal vs.
      `CredentialManagementDefaults.Policy`
- [ ] Tests (analyzer positive/negative, or generator round-trip)
- [ ] Docs: note the new check in AGENTS.md's TWA table and the web-api-contracts skill if the
      convention changes
- [ ] dev build 0/0; full dev test

## Notes

Do not expand task 110's scope to cover this — 110 left the coupling documented via comments only
(`// matches IdentitySessionDefaults.AuthenticatedPolicy`) and deliberately did not build a new
analyzer for it.

104-005 review round-1 (M2) is a THIRD motivating instance: `CredentialManagementDefaults.Policy`
("credential-management") duplicated as a string literal across `get-credentials.cs`,
`revoke-credential.cs`, `add-passkey.cs`, and `add-agent-key.cs`. Same coupling, same interim
mitigation (fail-closed + `// matches CredentialManagementDefaults.Policy` comment) — not fixed
per-task, tracked here.
