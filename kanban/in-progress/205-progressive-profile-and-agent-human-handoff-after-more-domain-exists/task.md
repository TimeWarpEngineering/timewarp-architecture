# Progressive profile and agent-human handoff after more domain exists

## Description

Deferred product work pulled off epic **104** (Agent-ready Identity and x402).

**104** shipped principals, credentials, sessions/tokens, TimeWarp.402, and the agent-ready
template surface (Waves 1–4 + 023). These two items are **higher-level product** than that
kernel. Placement (which slice, which package, which host) is not honest until more of the
**domain** exists — marketplace, human account chrome beyond Settings passkeys, and any
real agent↔human workflow.

Do **not** treat this as 104 leftover polish. Do **not** implement until domain surfaces
exist to hang them on.

Folded from:

| Old child | Topic |
|-----------|--------|
| **104-024** (done — superseded stub) | Optional progressive profile after the principal exists |
| **104-025** (done — superseded stub) | Optional Agent ↔ Human link and portable humanUx handoff payload |

## Requirements

### Progressive profile (was 104-024)

- Optional display name / email / etc. **after** principal exists
- Contract/endpoint style of the template
- **Never** a gate on passkey register, agent-key register, session, or token
- Locked 104 decision 1: passkey/key first, profile later

### Agent–human link + humanUx (was 104-025)

- Optional link Agent ↔ Human and a portable humanUx JSON an agent can show its human
- Minimal link/approve mechanism
- humanUx schema in a Design region / sample JSON
- **Not** required for paid service (locked 104 decision 3: no human required if the agent pays)

## Checklist

- [x] Enough domain exists to place profile vs identity vs a future account slice
- [x] Progressive profile: model fields, update API, tests — still never a register/session gate
- [x] Agent–human link: link model, minimal approve flow
- [x] Sample humanUx payload + Design region
- [x] Document where this lives (Identity vs template Features vs new slice) — decide then, not now

## Notes

Hold until demanded **and** until domain placement is obvious. A2A-shaped handoff.

Former Wave 5 on **104**. Cloudflare operator notes (**104-023**) stayed on 104 and are **done**.

Soft predecessors (not `## Depends on` merge-wait): 104-002 (principal model), 104-004 (agent keys),
104-016 (human passkey demo). Those are already merged.

- Overnight 2026-09-04: first implementer judged the existing **Profiles** slice enough to hang this on, started product, then **hit max-turns** with **uncommitted** work. Resume walk continued that tree (tests + EF migration + Results).

## Session

- Created: 528392 (2026-08-26)
- Cockpit: Grok — pulled 104-024 / 104-025 off epic 104 into this independent to-do
- Overnight: Grok implementer max-turns (uncommitted); resume on this same claim worktree
- Implementer: Grok (2026-09-04) — finished product, tests, EF migration, Results

## Results

Continued the uncommitted overnight tree. Placement is now decided and documented:

| Concern | Lives in |
|---------|----------|
| Passkey / agent-key / session / token | TimeWarp.Identity + `web/features/identity/` |
| Optional display name, email, language/region/theme | `web/features/profile/` (`Features.Profiles`) |
| Optional Agent ↔ Human link + humanUx JSON | `web/features/agent-links/` (`Features.AgentLinks`) |
| Marketplace / account-as-billing | Still a future slice (task 118) |

**Progressive profile (104-024)**
- `Profile.Email` optional (`SetEmail`); never required on Create
- `PUT api/Users/Current/Profile` (`UpdateProfile`, `profile.write`, human session)
- GET stays dual-mode anonymous demo vs store-backed create-if-missing
- SPA `/Profile` binds `IProfileDetails` (same shape as Get/Update)
- Passkey register, agent-key register, token issuance, and metered capability do **not** take `IProfileStore`

**Agent–human link + humanUx (104-025)**
- `AgentHumanLink` aggregate: Create → Pending; human Approve/Deny
- Agent `POST api/agent-links`; human `POST …/approve` or `…/deny`; list `GET api/agent-links`
- Agent `GET api/agent-links/{id}/human-ux` → `timewarp.humanUx/v1` (Approved + owning agent only)
- Sample: `source/container-apps/web/features/agent-links/human-ux.sample.json`
- SPA `/AgentLinks` approve/deny chrome
- `InvokeMeteredCapability` does **not** take `IAgentHumanLinkStore`

**Key decisions**
- Identity kernel stays credentials/trust. `Principal.DisplayName` remains an optional kernel alias; product email/prefs stay on Profiles.
- GetHumanUx fills `human.displayName` from `Principal`, not Profiles (TWA0009). Sample JSON still shows optional `email`.
- Humans get `profile.write` + `agent-link.manage.self` via `RolePermissionSeed.SelfServicePermissions`. Agents get `agent-link.manage.self` from `identity:read`.
- Page gate on `/Profile` stays `profile.read`; save is authorized at the PUT.

**Files (high level)**
- Profile: domain Email, `IProfileDetails`, `update-profile/`, SPA form + `UpdateProfileActionSet`
- AgentLinks slice (contracts/handlers/store/EF) + SPA state/page
- EF migration `20260904134226_AddProfileEmailAndAgentHumanLinks` (Email column, `agent_links` table, self-service grant rows)
- Placement guide: `documentation/developer/how-to-guides/how-to-progressive-profile-and-agent-human-link.md`
- Template smoke web aggregator expected count 104 → 125

**Tests**
- `update-profile-tests.cs` — 8 passed
- `agent-human-link-tests.cs` — 9 passed (incl. humanUx JSON round-trip)
- `identity-progressive-profile-gate-tests.cs` — 4 passed
- `permission-evaluator-tests.cs` — 19 passed
- `get-profile-tests.cs` — 10 passed
- `permission-ids-tests.cs` — 8 passed
- `cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release` — 125 passed
- `cd tests/container-apps/web/web-infrastructure-tests && dotnet test -c Release -- --filter-class Profile_Model_Mapping` — 4 passed
- `dotnet run tools/dev-cli/dev.cs -- build` — 0 Warning(s), 0 Error(s)

Browser click-through of `/Profile` and `/AgentLinks` was not run in this session (no AppHost). SPA compiles in the 0/0 build; handler/contract tests cover the APIs.

### How to validate

**Automated**

```bash
dotnet run source/container-apps/web/features/profile/update-profile/update-profile-tests.cs
# expect: Total: 8, Passed: 8

dotnet run source/container-apps/web/features/agent-links/agent-human-link-tests.cs
# expect: Total: 9, Passed: 9; humanUx JSON contains timewarp.humanUx/v1

dotnet run source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs
# expect: Total: 4, Passed: 4 (passkey/agent-key/token/metered handlers do not take IProfileStore or IAgentHumanLinkStore)

cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release
# expect: succeeded: 125
```

**Smoke**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: 0 Warning(s), 0 Error(s)

# After `dev run` (or equivalent Aspire AppHost) and a signed-in human session:
# 1. Open /Profile — display name required, email optional; Save persists via PUT api/Users/Current/Profile
# 2. Open /AgentLinks — empty list copy "No agent links yet." Paid-service copy visible.
# 3. As an agent token with identity:read, POST api/agent-links { "humanPrincipalId": "<human guid>" }
#    expect: 200, status Pending
# 4. Human Approve on /AgentLinks
#    expect: Status Approved
# 5. Agent GET api/agent-links/{linkId}/human-ux
#    expect: 200, spec "timewarp.humanUx/v1", kind "handoff"
```

**Expect**
- Registering a passkey or agent key still works with no profile row and no email.
- Metered/paid invoke does not require an approved human link.
- `profiles.profiles.Email` is nullable varchar(254); schema `agent_links.agent_links` exists after migration.

**Depends on:** signed-in identity-session (or mock session) for human chrome; agent token for request/humanUx. Postgres volumes pick up the new migration via AppHost `web-migrations`.

**Not in scope:** live WebAuthn hardware; marketplace/billing (task 118); folding these APIs into TimeWarp.Identity.
