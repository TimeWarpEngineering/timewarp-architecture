# How to place progressive profile and agent–human links

Task **205** (former 104-024 / 104-025). Identity kernel (TimeWarp.Identity) stays principals,
credentials, sessions, and tokens. These two surfaces are **template product**, hung on domain
that already exists: `Features.Profiles` (personalization chrome) and a new `Features.AgentLinks`
slice (optional agent↔human relationship).

## Placement

| Concern | Lives in | Does not live in |
|---------|----------|------------------|
| Passkey / agent-key / session / token | TimeWarp.Identity + `web/features/identity/` | Profile or AgentLinks |
| Optional display name, email, language/region/theme | `web/features/profile/` (`Features.Profiles`) | TimeWarp.Identity (Principal.DisplayName remains an optional kernel alias only) |
| Optional Agent ↔ Human link + humanUx JSON | `web/features/agent-links/` (`Features.AgentLinks`) | TimeWarp.Identity |
| Marketplace / account-as-billing | Still a future slice (task 118). Do not fold these APIs there. | — |

Locked 104 decisions that this placement preserves:

1. Passkey/key first, profile later — `CompletePasskeyRegistration` / `CompleteAgentKeyRegistration`
   / token issuance do not take `IProfileStore`.
3. No human required if the agent pays — `InvokeMeteredCapability` does not take
   `IAgentHumanLinkStore`.

## Progressive profile

- **GET** `api/Users/Current/Profile` — create-if-missing defaults (`Member`, `en-US`, …).
- **PUT** `api/Users/Current/Profile` — `UpdateProfile` (`profile.write`, human session).
- Email is optional. Alias stays required so chrome always has a name.
- SPA: `/Profile` binds `IProfileDetails` (same shape as Get/Update).

## Agent–human link + humanUx

Minimal approve flow:

1. Agent `POST api/agent-links` `{ humanPrincipalId }` → Pending
2. Human `POST api/agent-links/{id}/approve` (or `/deny`)
3. Agent `GET api/agent-links/{id}/human-ux` → portable document (`timewarp.humanUx/v1`)

Schema and sample: `source/container-apps/web/features/agent-links/human-ux-contracts.cs`
Design region and `human-ux.sample.json`. Human chrome on `/AgentLinks`.
