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

- [x] Answer the mechanism question with a falsifiable test (same rigor as 158's investigation)
- [x] Audit test coverage of agent-token + credential-management auth paths
- [x] Risk analysis of endpoint-level vs policy-level scheme lists
- [x] Written recommendation with options; maintainer decision recorded
- [x] Results (research task: findings + decision are the deliverable; follow-up implement task
      only if the decision requires code)
- [x] Implementation review disposition (effort 1, same id; no sibling apply-review task)

## Notes

- Origin: task 158 Results ("Out of scope" section) and its Grok root-cause investigation.
- The `AuthenticationSchemeNames` constants class
  (`source/container-apps/web/features/identity/authentication-scheme-names-contracts.cs`)
  already includes `AgentToken` for use if the answer is "declare everywhere".
- Task 182-006 (permission-centric auth, ADR-0010) already moved web credential + agent-only
  contracts onto `[EndpointAuthorize(AuthenticationSchemes)]` and **removed**
  `AddAuthenticationSchemes` from permission policies. This research confirms that was required,
  not merely consistent. The last Policies-only hosted contract was api-server
  `GetAgentBearerIdentity` (policy still lists `agent-token`).

## Session

- Created: Claude (2026-08-05), spun out of task 158 per maintainer direction (answer unknown —
  research first, don't assume).
- Implementer: Grok (2026-09-04) — mechanism test, coverage audit, hybrid litmus, fold-in.
- Review oracle: Grok (2026-09-04) — effort 1 general; round 1 + round 2 (M1 coverage-label fix).

## Results

### Mechanism

FastEndpoints 8.3 does **not** run its own auth middleware. `Policies("X")` and `AuthSchemes(...)`
become `IAuthorizeData` via `EndpointSecurityPolicies.BuildAuthorizeAttributes`:

- one `AuthorizeAttribute` per policy name (user policies, then `epPolicy:<EndpointType>`)
- `AuthenticationSchemes` is copied onto those attributes **only** from `AuthSchemes()`
- `epPolicy` is always `RequireAuthenticatedUser()` with **empty** schemes

ASP.NET Core 10 `AuthorizationPolicy.CombineAsync` **unions** named-policy
`AddAuthenticationSchemes` with any `IAuthorizeData.AuthenticationSchemes`.
`PolicyEvaluator.AuthenticateAsync` then:

- if the combined list is **non-empty**: `AuthenticateAsync` each listed scheme (merge successes)
- if the combined list is **empty**: **no-op** — only `UseAuthentication`'s **default** scheme
  result is used (`identity-session` on web)

Falsifiable TestServer proof (`ProbeScheme_Given_` in
`tests/container-apps/web/web-server-integration-tests/features/identity/fast-endpoint-auth-schemes-tests.cs`),
same rigor as 158's throw-in-handler instrumentation (counter at the top of the probe handler):

| Shape | Probe handler ran? | HTTP |
|-------|--------------------|------|
| Policies-only + policy `AddAuthenticationSchemes("probe")` | yes | 2xx |
| Policies-only + policy with **no** schemes (PermissionIds) | **no** | 401 |
| `AuthSchemes("probe")` + policy with no schemes | yes | 2xx |
| both lists present | yes | 2xx |

So: **not every Policies-only endpoint skips non-default schemes.** Policy-level
`AddAuthenticationSchemes` **is** sufficient when the named policy actually lists them (Combine
copies them). Task 158's mock miss is **not** "FE always drops policy schemes"; it matches the
**empty combined scheme list** path. After 182, product permission policies register
`PermissionRequirement` only — that empty-list path is the live web shape.

Credential-management "worked" because 182-006 already put
`AuthenticationSchemes = identity-session,agent-token` on those four contracts. Api-server
`GetAgentBearerIdentity` worked Policies-only because its policy still lists `agent-token` (and
api has no default scheme).

### Coverage audit

In-proc HostGraph **does** exercise agent-token through credential-management
(`web-server-integration-tests`; InvokeMeteredCapability anonymous 401 is the co-located
`invoke-metered-capability-tests.cs` `Unauthorized_Given_No_Bearer`, not that suite):

| Endpoint | cookie | bearer | anonymous | wrong scheme |
|----------|--------|--------|-----------|--------------|
| GetCredentials | yes | yes (`credential:manage` 200; `identity:read` 403) | 401 | n/a (dual) |
| RevokeCredential | yes | yes | 401 | n/a |
| AddPasskey | yes | **no dedicated bearer test** | 401 | — |
| AddAgentKey | **no dedicated cookie test** | yes | **no anonymous test** | — |
| GetAgentIdentity | cookie → 401 (isolation) | yes | 401 | covered |
| InvokeMeteredCapability | **no cookie isolation test** | yes | 401 | gap vs GetAgentIdentity |

Closed-box `aspire-tests`: **zero** hits on credential or agent-me routes (only GetRoles mock 403 /
anonymous 401). Mock header is **not** listed on credential/agent contracts (deliberate — no mock
scheme on those surfaces). SPA suite: none of these six.

The "works today" assumption for **bearer on GetCredentials/RevokeCredential** is tested in-proc.
It is **not** tested closed-box. Asymmetric happy paths (AddPasskey cookie-only, AddAgentKey
bearer-only) remain.

### Risk analysis (endpoint-level vs policy-level lists)

Same `PolicyEvaluator` merge either way. Combine **unions** the two lists (`Distinct` at
`Build()`). Listing the same names on both is a no-op for authenticate/challenge/forbid.

| Concern | Effect |
|---------|--------|
| Order / challenge scheme | First name in the **combined** list. Policy schemes are added first, then `IAuthorizeData`. Identical lists → identical first scheme. Credential contracts list `identity-session` then `agent-token` → anonymous `/api` challenges as cookie 401. |
| Forbid | `ForbidAsync` for every combined scheme. Cookie `OnRedirectToAccessDenied` = 403; agent-token handler Forbid = 403 + `WWW-Authenticate`. |
| Narrowing | You **cannot** drop a scheme by omitting it on the contract if the named policy still lists it (union). After 182, permission policies list **none**, so the contract **is** the list — agent-only routes isolate by listing only `agent-token`. |
| Dual-scheme both-succeed | Identities merge onto `HttpContext.User` (documented on `ICurrentPrincipalAccessor`). Unchanged by which SSOT named the schemes. |
| Putting schemes back on permission policies | Dual SSOT; a drift 158-class bug. ADR-0010 forbids it. |

### Recommendation (hybrid, option c) — decision

**Litmus (product truth, already shipped on web by 182 / ADR-0010):**

1. Hosted `[EndpointAuthorize]` **must** set `AuthenticationSchemes` (`AuthenticationSchemeNames`
   on web). PermissionIds policies have no `AddAuthenticationSchemes`; omitting the attribute
   silently authenticates only `identity-session`.
2. Named-policy `AddAuthenticationSchemes` remains sufficient **when the policy actually lists
   schemes** (api-server agent-scope policies, `IdentitySessionDefaults.AuthenticatedPolicy`).
   Still declare on the contract so a policy-registration change cannot drop non-default schemes.
3. Do **not** put scheme lists back on permission policies.
4. Analyzer (not in this task): a TWA that hosted `[EndpointAuthorize]` has non-empty
   `AuthenticationSchemes` is the prefer-analyzers follow-up. Distinct from task 111 (policy
   **name** agreement). Do not build a contract-vs-policy scheme-agreement analyzer — permission
   policies have no scheme list to agree with.

**Maintainer decision recorded:** ADR-0010 already chose "scheme lists stay on
`[EndpointAuthorize(AuthenticationSchemes)]`." This research confirms that is **required** for
permission policies, not optional consistency. Fold-in on this id: convention in
`tw-web-api-contracts`, Design regions, ADR-0010 sentence, api-server `GetAgentBearerIdentity`
now declares `AuthenticationSchemes = "agent-token"`.

### What changed

| Path | Change |
|------|--------|
| `tests/.../fast-endpoint-auth-schemes-tests.cs` | New isolated FastEndpoints TestServer mechanism suite (`ProbeScheme_Given_`, 4/4) |
| `get-agent-bearer-identity-contracts.cs` | `AuthenticationSchemes = "agent-token"` on the last Policies-only hosted contract |
| `authentication-scheme-names-contracts.cs`, `endpoint-authorize-attribute.cs`, identity-host accessor Design regions, `get-roles-contracts.cs`, `credential-list-tests.cs` | Reconciled with permission-policy empty scheme lists |
| `skills/tw-web-api-contracts/SKILL.md` | Required `AuthenticationSchemes` on hosted `[EndpointAuthorize]` + pitfall |
| ADR-0010, `how-to-agent-identity-host-split-web-vs-api.md`, generator markdown | Litmus documented |

No TWA analyzer in this task.

### Review disposition

**Outcome:** clean. **Rounds:** 2. **Effort:** 1 (general only). **Final open count:** 0.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

M1 (nit): coverage-audit heading attributed InvokeMeteredCapability anonymous 401 to
`web-server-integration-tests`; that case is co-located `Unauthorized_Given_No_Bearer`. Fixed
on this id (coverage intro wording). No product-code findings. No wontfix / escalations.

Paths: `review/review-framework.md`, `review/round-1/{general,merged}.md`,
`review/round-2/{general,merged}.md`, `review/disposition.md`.

### Test outcomes

- `ProbeScheme_Given_` 4/4
- `get-agent-bearer-identity-tests.cs` 4/4 (still 200 with bearer after declaring schemes)

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class ProbeScheme_Given_
dotnet run source/container-apps/api/features/agent-bearer-sample/get-agent-bearer-identity/get-agent-bearer-identity-tests.cs
```

**Expect**

- Probe suite: 4 passed. Policies-only + no policy schemes → 401 and probe invoke count 0.
  The other three shapes invoke the probe handler and return 2xx.
- Api GetAgentBearerIdentity: 4 passed (`Ok_With_String_Enums_Given_Valid_IdentityRead_Token` →
  200; anonymous → 401 `WWW-Authenticate: Bearer`).

**Automated gate**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class ProbeScheme_Given_
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release -- --filter-method Should_Emit_Both_AuthSchemes_And_Policies_When_Both_Set
```

**Not in scope:** closed-box credential/agent-me routes; AddPasskey-via-bearer / AddAgentKey-via-cookie
symmetric happy paths; TWA analyzer for missing `AuthenticationSchemes`.
