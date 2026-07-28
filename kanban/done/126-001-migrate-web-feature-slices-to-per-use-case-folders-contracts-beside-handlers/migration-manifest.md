# Migration manifest — 126-001 + 126-002 combined pass

Produced 2026-07-26 by a read-only planning agent; executed verbatim by the implementation pass
(minus items resolved differently by the maintainer — see task Notes). Sections: features-tree
moves, layer-folder evacuations with namespace targets, hazards H1–H11, execution order, open
uncertainties U1–U4.

## 1. Features-tree moves (126-001)

Rule: operation-specific files (one Command/Query + its handler + any operation-only helpers) move into `<slice>/<use-case>/`; shared/multi-operation files stay at slice root; `commands/`/`queries/` folders are removed. Namespaces do NOT change in this pass.

### admin/modules (1 file — no operations, stays flat)
| Current | New |
|---|---|
| `admin/modules/module-ids-contracts.cs` | stays (shared const/enum shape, no operation) |

### admin/roles (13 files → 5 use-cases + 3 stayers)
| Current | New |
|---|---|
| `admin/roles/commands/create-role-contracts.cs` | `admin/roles/create-role/create-role-contracts.cs` |
| `admin/roles/create-role-handler-application.cs` | `admin/roles/create-role/create-role-handler-application.cs` |
| `admin/roles/commands/delete-role-contracts.cs` | `admin/roles/delete-role/delete-role-contracts.cs` |
| `admin/roles/delete-role-handler-application.cs` | `admin/roles/delete-role/delete-role-handler-application.cs` |
| `admin/roles/queries/get-role-contracts.cs` | `admin/roles/get-role/get-role-contracts.cs` |
| `admin/roles/get-role-handler-application.cs` | `admin/roles/get-role/get-role-handler-application.cs` |
| `admin/roles/queries/get-roles-contracts.cs` | `admin/roles/get-roles/get-roles-contracts.cs` |
| `admin/roles/get-roles-handler-application.cs` | `admin/roles/get-roles/get-roles-handler-application.cs` |
| `admin/roles/commands/update-role-contracts.cs` | `admin/roles/update-role/update-role-contracts.cs` |
| `admin/roles/update-role-handler-application.cs` | `admin/roles/update-role/update-role-handler-application.cs` |

Stays at `admin/roles/` root: `role-details-contracts.cs` (shared bindable shape used by create/update/get-role/get-roles), `role-store-application.cs` (shared store), `roles-feature-annotations-server.cs` (slice-wide feature annotations).

### analytics (3 files → 1 use-case + 1 stayer)
| Current | New |
|---|---|
| `analytics/track-event-contracts.cs` | `analytics/track-event/track-event-contracts.cs` |
| `analytics/track-event-handler-application.cs` | `analytics/track-event/track-event-handler-application.cs` |

Stays: `analytics/analytics-feature-annotations-server.cs`.

### auth (3 files → 1 use-case + 1 stayer)
| Current | New |
|---|---|
| `auth/queries/get-sign-in-token-contracts.cs` | `auth/get-sign-in-token/get-sign-in-token-contracts.cs` |
| `auth/get-sign-in-token-handler-application.cs` | `auth/get-sign-in-token/get-sign-in-token-handler-application.cs` |

Stays: `auth/auth-feature-annotations-server.cs`.

### authentication (1 file → 1 use-case)
| Current | New |
|---|---|
| `authentication/queries/get-current-user-contracts.cs` | `authentication/get-current-user/get-current-user-contracts.cs` |

Note: this contract carries `[ClientOnlyContract("Served by SPA mock mode; the template has no server-side auth slice.")]` — confirmed by design there is no application handler; not a gap.

### authorization (1 file — no operations, stays flat)
| Current | New |
|---|---|
| `authorization/role-ids-contracts.cs` | stays (shared const shape, no operation) |

### chat (5 files → 2 use-cases + 2 stayers)
| Current | New |
|---|---|
| `chat/client-to-server/send-message-contracts.cs` | `chat/send-message/send-message-contracts.cs` |
| `chat/send-message-handler-application.cs` | `chat/send-message/send-message-handler-application.cs` |
| `chat/server-to-client/receive-message-contracts.cs` | `chat/receive-message/receive-message-contracts.cs` |

Stays at `chat/` root: `chat-hub-constants-contracts.cs` (shared route/const shape), `signal-r-result-contracts.cs` (shared wire-result wrapper used by both directions).

Planner flag (U2): `client-to-server/`/`server-to-client/` are the same group-by-message-kind
instinct as `commands/`/`queries/` — recommended collapse into use-case folders.

### hello (3 files) — U1: operation name == slice name
| Current | New (planner recommendation) |
|---|---|
| `hello/hello-contracts.cs` | `hello/hello/hello-contracts.cs` |
| `hello/hello-handler-application.cs` | `hello/hello/hello-handler-application.cs` |

Stays: `hello/hello-feature-annotations-server.cs` (feature-annotations is a stay-at-root category regardless of operation count).

### identity (33 files → 14 use-cases + 5 stayers)
14 use-cases, each = one `commands/`/`queries/` contract + its flat `*-handler-application.cs`:

| Current (contract) | Current (handler) | New folder |
|---|---|---|
| `identity/commands/add-agent-key-contracts.cs` | `identity/add-agent-key-handler-application.cs` | `identity/add-agent-key/` |
| `identity/commands/add-passkey-contracts.cs` | `identity/add-passkey-handler-application.cs` | `identity/add-passkey/` |
| `identity/commands/complete-agent-key-registration-contracts.cs` | `identity/complete-agent-key-registration-handler-application.cs` | `identity/complete-agent-key-registration/` |
| `identity/commands/complete-agent-token-issuance-contracts.cs` | `identity/complete-agent-token-issuance-handler-application.cs` | `identity/complete-agent-token-issuance/` |
| `identity/commands/complete-passkey-authentication-contracts.cs` | `identity/complete-passkey-authentication-handler-application.cs` | `identity/complete-passkey-authentication/` |
| `identity/commands/complete-passkey-registration-contracts.cs` | `identity/complete-passkey-registration-handler-application.cs` | `identity/complete-passkey-registration/` |
| `identity/commands/revoke-credential-contracts.cs` | `identity/revoke-credential-handler-application.cs` | `identity/revoke-credential/` |
| `identity/commands/start-agent-key-registration-contracts.cs` | `identity/start-agent-key-registration-handler-application.cs` | `identity/start-agent-key-registration/` |
| `identity/commands/start-agent-token-issuance-contracts.cs` | `identity/start-agent-token-issuance-handler-application.cs` | `identity/start-agent-token-issuance/` |
| `identity/commands/start-passkey-authentication-contracts.cs` | `identity/start-passkey-authentication-handler-application.cs` | `identity/start-passkey-authentication/` |
| `identity/commands/start-passkey-registration-contracts.cs` | `identity/start-passkey-registration-handler-application.cs` | `identity/start-passkey-registration/` |
| `identity/queries/get-agent-identity-contracts.cs` | `identity/get-agent-identity-handler-application.cs` | `identity/get-agent-identity/` |
| `identity/queries/get-credentials-contracts.cs` | `identity/get-credentials-handler-application.cs` | `identity/get-credentials/` |
| `identity/queries/get-current-session-contracts.cs` | `identity/get-current-session-handler-application.cs` | `identity/get-current-session/` |

Each pair keeps its own two filenames unchanged, just relocated.

Stays at `identity/` root: `credential-entity-type-configuration-infrastructure.cs`, `principal-entity-type-configuration-infrastructure.cs` (entity type configs, shared category), `identity-feature-annotations-server.cs` (slice-wide annotations), `web-authn-payload-decoder-application.cs` and `web-authn-relying-party-selection-application.cs` — verified by grep: both consumed by 7+ different operation handlers, genuinely multi-operation shared helpers.

### profile (4 files → 1 use-case + 1 stayer)
| Current | New |
|---|---|
| `profile/queries/get-profile-contracts.cs` | `profile/get-profile/get-profile-contracts.cs` |
| `profile/get-profile-handler-application.cs` | `profile/get-profile/get-profile-handler-application.cs` |

Stays: `profile/profile-feature-annotations-server.cs`, `profile/profile-entity-type-configuration-infrastructure.cs` (shared entity config; see H2 for its `using` fixup once the Profile aggregate moves).

### todo-items (6 files → 5 use-cases + 1 stayer)
| Current | New |
|---|---|
| `todo-items/commands/create-todo-item-contracts.cs` | `todo-items/create-todo-item/create-todo-item-contracts.cs` |
| `todo-items/commands/delete-todo-item-contracts.cs` | `todo-items/delete-todo-item/delete-todo-item-contracts.cs` |
| `todo-items/commands/update-todo-item-contracts.cs` | `todo-items/update-todo-item/update-todo-item-contracts.cs` |
| `todo-items/queries/get-todo-item-by-id-contracts.cs` | `todo-items/get-todo-item-by-id/get-todo-item-by-id-contracts.cs` |
| `todo-items/queries/search-todo-items-contracts.cs` | `todo-items/search-todo-items/search-todo-items-contracts.cs` |

Stays: `todo-items/todo-item-dto-contracts.cs` (shared DTO shape). Slice is contracts-only by design (`[ClientOnlyContract]` demo).

## 2. Layer-folder evacuations (126-002)

Namespace rule: movers adopt `…Features.<Id>` namespaces. Target namespaces derived from actual
sibling files per layer (contracts/server → `Features.<Slice>`; application →
`Features.<Slice>.Application`; infrastructure → `Features.<Slice>.Infrastructure`; profile slice
namespace is plural **`Features.Profiles`** per its existing infrastructure sibling). Domain layer
has no precedent — `TimeWarp.Architecture.Features.Profiles.Domain` inferred by analogy (U3).

| Current path | New path (filename) | Target folder | Namespace change |
|---|---|---|---|
| `web-domain/aggregates/profile/profile.cs` | `profile-domain.cs` | `features/profile/` (slice root — shared aggregate) | `TimeWarp.Architecture.Aggregates.Profiles` → `TimeWarp.Architecture.Features.Profiles.Domain` (U3) |
| `web-domain/aggregates/profile/profile-id.cs` | `profile-id-domain.cs` | `features/profile/` | same |
| `web-infrastructure/persistence/ef-principal-store.cs` | `ef-principal-store-infrastructure.cs` | `features/identity/` (slice root — store spans many operations) | `TimeWarp.Architecture.Persistence` → `TimeWarp.Architecture.Features.Identity.Infrastructure` |
| `web-server/hubs/chat-hub.cs` | `chat-hub-server.cs` | `features/chat/` | `TimeWarp.Architecture.Hubs` → `TimeWarp.Architecture.Features.Chat` |
| `web-server/services/chat-hub-service.cs` | `chat-hub-service-server.cs` | `features/chat/` | `TimeWarp.Architecture.Services` → `TimeWarp.Architecture.Features.Chat` |
| `web-application/configuration/web-authn-options.cs` | `web-authn-options-application.cs` | `features/identity/` (slice root — used by 7+ operations) | `TimeWarp.Architecture.Configuration` → `TimeWarp.Architecture.Features.Identity.Application` |
| `web-application/configuration/web-authn-options-validator.cs` | `web-authn-options-validator-application.cs` | `features/identity/` | same |
| `web-application/configuration/agent-token-options.cs` | `agent-token-options-application.cs` | `features/identity/` | same |
| `web-application/configuration/agent-token-options-validator.cs` | `agent-token-options-validator-application.cs` | `features/identity/` | same |
| `web-server/services/agent-token-authentication-handler.cs` | `agent-token-authentication-handler-server.cs` | `features/identity/` | `TimeWarp.Architecture.Services` → `TimeWarp.Architecture.Features.Identity` |

All ten carry `#region Purpose` (TWA0004-clean). `WebAuthnOptions`/`AgentTokenOptions` Design
regions narrate the old folder homes — reconcile during move. `profile-id.cs` is a `[TypedId]`
partial (generator counterpart follows the attributed partial's namespace automatically; H8).

### Stayers (ambiguous category, defaulted STAY per 126-002) — rationale
| File | Rationale |
|---|---|
| `web-server/services/cookie-browser-session-service.cs` | Implements `IBrowserSessionService` seam; pure ASP.NET-Core host wiring — seam/impl split is the point, impl stays with host. |
| `web-server/services/http-current-principal-accessor.cs` | Implements `ICurrentPrincipalAccessor` seam; ASP.NET-Core-bound impl. |
| `web-server/services/http-request-host-accessor.cs` | Implements `IRequestHostAccessor` seam. |
| `web-server/services/agent-caller-context.cs` | Implements `IAgentCallerContext` seam; reads host-only defaults constants. |
| `web-server/configuration/identity-session-defaults.cs` | Referenced only from `program.cs` + staying service impls; host-registration constant stays at registration site. |
| `web-server/configuration/agent-token-defaults.cs` | Same pattern (planner-classified; not in task inventory); cross-namespace `using` from the moving auth handler is normal. |
| `web-server/configuration/credential-management-defaults.cs` | Referenced only from `program.cs` + staying accessor. |
| `web-server/configuration/sample-options.cs` / `sample-options-validator.cs` | Generic host-pattern exemplar; nothing outside web-server consumes it. |
| `web-server/configuration/environment-checks/*` | Host startup checks (category 2). |
| `web-server/hosted-services/postgres-db-context-startup-hosted-service.cs` | Host startup service (category 2). |
| `web-server/modules/postgres-db-module.cs`, `web-infrastructure/web-infrastructure-module.cs` | Host/DI modules (category 1/2). |
| `web-infrastructure/configuration/postgres-db-options.cs` | Connection binder for postgres module/check, not feature-specific. |
| `web-infrastructure/persistence/postgres-db-context.cs` | Explicitly stays per 126-002 Notes (platform infra aggregating slice entity configs). |
| `web-application/abstractions/i-*.cs` (4 files) | Category-3 platform seams per task description. |
| `web-contracts/mocks/mock-user-ids.cs` | Planner-classified (not in task inventory): shared across ≥2 slices (authentication contract + SPA mock auth provider) — platform seam, stays. |
| `web-contracts/extensions/assembly-extensions.cs` | Generic reflection helper — plumbing. |
| All `assembly-marker.cs` / `global-usings.cs` / `internals-visible-to-*` / `program.cs` | Category-1 plumbing / category-2 host. |

## 3. Hazards and required accompanying edits

**H1 — `.template.config/template.json:71` (hard path reference, MUST fix):** in the
`(!postgres)` exclude block, `source/container-apps/web/web-infrastructure/persistence/ef-principal-store.cs`
→ `source/container-apps/web/features/identity/ef-principal-store-infrastructure.cs`. Sole
hard-coded path hit across template.json/csproj/props/targets/slnx for any moving file.

**H2 — `TimeWarp.Architecture.Aggregates.Profiles` fully vacated (MUST fix; CS-error class):**
four sites need the using switched to the new domain namespace:
`web-infrastructure/persistence/postgres-db-context.cs:38`,
`features/profile/profile-entity-type-configuration-infrastructure.cs:25`,
`tests/container-apps/web/web-infrastructure-tests/global-usings.cs:5`,
`tests/container-apps/web/web-domain-tests/global-usings.cs:4`.

**H3 — `TimeWarp.Architecture.Persistence` survives** (`postgres-db-context.cs` stays) — keep
existing global usings; ADD `global using` for the identity infrastructure namespace to
`tests/container-apps/web/web-infrastructure-tests/global-usings.cs` (EfPrincipalStore tests).

**H4 — `TimeWarp.Architecture.Hubs` survives** via `web-spa/hubs/chat-hub-connection.cs`;
confirm web-server's global using stays genuinely used via clean build, don't assume.

**H5 — `TimeWarp.Architecture.Configuration` survives in web-server** (5 stayers); web-application's
`global using TimeWarp.Architecture.Configuration;` may go DEAD post-move (movers' consumers are
already in `Features.Identity.Application`) — remove only if the clean build flags it.

**H6 — `TimeWarp.Architecture.Services` survives** (4 of 6 members stay) — no changes.

**H7 — `documentation/developer/how-to-guides/HowToAddYourAggregate.md:7,28,57`** references old
`web-domain/aggregates/profile/` exemplar paths — update to new paths.

**H8 — TWA0001/generator pairing:** only `profile-id.cs` is generator-paired (`[TypedId]`);
verify generated members land in the new namespace post-move.

**H9 — Tests blast radius = H2/H3 only** (all test references resolve via the two global-usings
files; ingress-generator test "Profile" hit is a false positive).

**H10 — Glob mechanics CONFIRMED SAFE, no csproj edits:** no explicit Compile Include/Remove in
any layer csproj; SDK default globs are directory-scoped, and `feature-filename-grammar.g.props`
suffix globs are recursive and per-project-conditioned — no double-match possible; moved files
stay in the same compilation units.

**H11 — No path references to `commands/`/`queries/`/`client-to-server`/`server-to-client`**
outside the tree itself (template.json, build files, docs all clean).

## 4. Execution order recommendation

1. **126-002 first** (riskier: namespace changes) — roslynk-driven renames, H1/H2/H3 fixups,
   Design-region reconciliation, then `dev build` full/clean checkpoint; resolve H4/H5 from what
   the build flags.
2. **126-001 second** — pure `git mv`, batch all slices (U1/U2 resolved first).
3. `dev build` 0/0, `dev test`, `dev template-smoke` both matrices.
4. Update `skills/tw-feature-placement/SKILL.md` worked example + AGENTS.md Layout section (U4).
5. Update 126 RFC F4/D2 premise-correction checklist item on 126-002.

## 5. Open uncertainties

- **U1 — `hello` slice:** operation name == slice name → literal rule gives `hello/hello/…`.
- **U2 — chat `client-to-server`/`server-to-client` folders:** collapse into use-case folders
  (recommended) or literal-reading keep?
- **U3 — first-ever domain-layer namespace:** `TimeWarp.Architecture.Features.Profiles.Domain`
  inferred by analogy; no precedent exists — maintainer sign-off requested.
- **U4 — AGENTS.md Layout / skill worked-example updates** not verified by this manifest; are
  explicit checklist items on 126-001.
