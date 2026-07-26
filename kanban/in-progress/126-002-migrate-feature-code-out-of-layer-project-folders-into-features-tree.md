# Migrate feature code out of layer project folders into features tree

## Description

Maintainer decision on task 126 (2026-07-26): feature-owned code currently living inside the web
layer project folders (**category 4** in the 126 folder-taxonomy discussion) belongs in
`source/container-apps/web/features/` under the filename grammar. Migrate it.

**The four-way sort this task implements one leg of** (from the 126 conversation):

1. Assembly/project plumbing (csproj, assembly-marker, global-usings, InternalsVisibleTo) — stays
   with the artifact.
2. True host/deployable code (program.cs, appsettings, launchSettings, environment checks,
   host modules) — stays in the layer folder.
3. Platform seams (`web-application/abstractions/i-*.cs` etc.) — stays put for now; a possible
   `platform/` home is a separate, undecided conversation on 126.
4. **Feature code → migrates to `features/` (this task).**

**Known category-4 inventory (verify + complete during execution):**

| Current location | Feature | Target shape (grammar name) |
|---|---|---|
| `web-domain/aggregates/profile/profile.cs` | profile | `features/profile/…profile-domain.cs` — first real `-domain.cs` files in the tree |
| `web-domain/aggregates/profile/profile-id.cs` | profile | `features/profile/…profile-id-domain.cs` |
| `web-infrastructure/persistence/ef-principal-store.cs` | identity | `features/identity/…ef-principal-store-infrastructure.cs` |
| `web-server/hubs/chat-hub.cs` | chat | `features/chat/…chat-hub-server.cs` (reunites the split chat feature — its constants contract already lives in `features/chat/`) |
| `web-server/services/chat-hub-service.cs` | chat | `features/chat/…-server.cs` |
| `web-application/configuration/web-authn-options.cs` + validator | identity | `features/identity/…-application.cs` |
| `web-application/configuration/agent-token-options.cs` + validator | identity (agent) | `features/identity/…-application.cs` |
| `web-server/services/agent-token-authentication-handler.cs` | identity (agent) | `features/identity/…-server.cs` |

Ambiguous candidates (classify during execution; default ambiguous host-wiring to **stay**):
`web-server/services/cookie-browser-session-service.cs`, `identity-session-defaults.cs`,
`credential-management-defaults.cs`, `http-current-principal-accessor.cs`,
`http-request-host-accessor.cs`, `agent-caller-context.cs`.

**Mechanics:** filename suffix must match the owning project's layer (the suffix globs are what
keep each file in the same compilation unit — project membership must not change). Grammar and
membership guard key on filename only; globs are recursive. Files land per the **use-case-folder
rule from 126-001** (operation-specific → `<use-case>/`, shared/multi-operation → slice root).

## Checklist

- [ ] Apply the resolved namespace rule: migrated files adopt `…Features.<Id>` namespaces
      (use roslynk rename_symbol so all references update; expect TWA0009 to begin governing
      these files — any cross-slice reference it surfaces is a finding, not a nuisance)
- [ ] Complete the category-4 inventory (sweep all five layer folders; classify ambiguous items)
- [ ] `git mv` + rename to grammar names; verify each file stays in its original project
      (compare `dotnet build` project outputs / use roslynk before-and-after)
- [ ] Coordinate with 126-001 (may execute as one pass; migrated files land in use-case folders)
- [ ] Re-check TWA0004 Purpose regions still honest after moves; reconcile Design regions
- [ ] Verify: `dev build` 0/0, `dev test`, `dev template-smoke` (both matrices)
- [ ] Update 126 RFC/record: the "empty domain layer" finding (F4/D2) premise is corrected —
      domain files existed in `web-domain/aggregates/`, not zero; skill headroom note stays valid

## Notes

- Parent: 126. Origin: folder-taxonomy conversation on 126 (see 126 `rfc/rfc.md` post-tally
  notes); maintainer decided category 4 migrates, categories 1–3 unchanged for now.
- **Namespace question RESOLVED (Steve, 2026-07-26): migrated files adopt `…Features.<Id>`
  namespaces — namespace declares slice membership.** Rationale: TWA0009 keys off the
  `…Features.<Id>` namespace, so a pure disk move would leave files in `features/` that the
  slice-isolation analyzer silently ignores (looks-governed-but-isn't). This is not a cosmetic
  folder-tracking rename (repo policy against those stands); it is a semantic membership
  declaration. Consequence: TWA0009 starts governing these files — surface any violations it
  finds as findings to fix or consciously `[CrossSliceReference]`, not as noise.
- Dependency note: `postgres-db-context.cs` (stays — platform infrastructure aggregating slice
  entity configs) references migrated types; no reference breakage expected since project
  membership is unchanged, but verify.

## Session

- Created: 2026-07-26 — filed from 126 maintainer decision (category-4 migration).
