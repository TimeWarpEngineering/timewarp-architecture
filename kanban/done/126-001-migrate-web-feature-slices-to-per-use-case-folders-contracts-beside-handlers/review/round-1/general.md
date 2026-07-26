# Round 1 — general
**Date:** 2026-07-26
**Scope reviewed:** 257d0ad1..40409ed7 (three commits) + repo-state spot checks

## Summary

The combined 126-001/126-002 pass does exactly what the manifest describes: 10 category-4 files
evacuated out of the layer folders into `features/` with `…Features.<Id>` namespaces (4442ca65),
then all operation-specific files across every slice regrouped into `<use-case>/` folders with
`commands/`/`queries`/`client-to-server`/`server-to-client` folders dissolved (5fff1e27), plus
skill-doc and how-to updates (40409ed7). I walked the full resulting `features/` tree against
every manifest table (§1 moves, §2 evacuations, U1–U3 resolutions), re-verified all eleven hazard
closures against the actual repo state (H1–H11), confirmed the filename-grammar props glob is
suffix-only and recursive (so use-case-folder nesting cannot cause project-membership drift), and
grep-swept the repo for every stale old-path/old-namespace pattern the framework called out. The
work is careful and the manifest was followed faithfully, including the `agent-token-authentication-
scheme-server.cs` escape-hatch rename, whose Purpose/Design regions read clean with no stale
references. One issue: a Design-region comment in the chat feature still narrates a folder
structure (`server-to-client`) that this exact migration renamed away.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/chat/receive-message/receive-message-contracts.cs:8
- Description: The Design region reads `"Mirrors SendMessage's shape; the folder (server-to-client)
  encodes direction because the type alone cannot."` The file used to live at
  `chat/server-to-client/receive-message-contracts.cs`; this migration (U2, the maintainer's own
  resolution) collapsed that direction-named folder into `chat/receive-message/`. The comment is a
  leftover pointer to the folder name this migration deliberately removed — per AGENTS.md's own
  rule ("A Design region describing the old approach is a bug you just introduced"), this qualifies
  even though it does not affect behavior. `send-message-contracts.cs`'s Design region has no
  equivalent stale reference; this is isolated to the one file (confirmed via repo-wide grep for
  "the folder" under `features/`).
- Suggestion: Reword to something like: `"Mirrors SendMessage's shape; the folder name
  (receive-message) states the use case, not the direction — Design records the direction instead."`
  or simply drop the parenthetical since the use-case folder name no longer encodes direction at all.
- Status: open

## Verification notes (no issues found)

- **Manifest fidelity:** every §1 move and §2 evacuation landed at its documented target
  path/filename; U1 (`hello/hello/`), U2 (chat use-case collapse), U3
  (`TimeWarp.Architecture.Features.Profiles.Domain`) all present exactly as resolved. All stayers
  (role-details, role-store, feature-annotations, entity-type-configs, web-authn/agent-token
  payload-decoder & RP-selection helpers, todo-item-dto) remain at slice root. Every emptied
  `commands/`/`queries/`/`client-to-server`/`server-to-client` folder is gone from the tree.
- **Executor deviation** (`agent-token-authentication-handler.cs` →
  `agent-token-authentication-scheme-server.cs`): sound use of the documented `<name>-<layer>`
  escape hatch (`handler` is reserved for the application layer per the registry; this is an
  ASP.NET Core `AuthenticationHandler`, not a mediator handler). No remaining reference anywhere in
  source/tests/docs to the old filename; the type name `AgentTokenAuthenticationHandler` itself is
  unchanged (correctly — only the filename needed to change) and its Purpose/Design regions contain
  nothing stale.
- **Hazards H1–H11:** all verified closed by direct inspection, not just the diff:
  - H1: `.template.config/template.json:71` points at the new `ef-principal-store-infrastructure.cs` path.
  - H2: all four `Aggregates.Profiles` using-sites (`postgres-db-context.cs:38`,
    `profile-entity-type-configuration-infrastructure.cs:25`, both test `global-usings.cs`) now
    reference `Features.Profiles.Domain`; zero remaining references to
    `TimeWarp.Architecture.Aggregates.Profiles` anywhere in source or tests.
  - H3: `web-infrastructure-tests/global-usings.cs` gained
    `global using TimeWarp.Architecture.Features.Identity.Infrastructure;`.
  - H5: `web-application/global-usings.cs` dropped the dead
    `global using TimeWarp.Architecture.Configuration;` line.
  - H7 + the three extra stale references the executor found: `HowToAddYourAggregate.md` (3 spots),
    `missing-invariants-validator-exception.cs` (message string + Design-region comment), and
    `aggregate-db-context.cs` (exception message) all now point at
    `web/features/profile/profile-domain.cs` + `web-domain/aggregates/overview.md` (the overview doc
    itself correctly stays at its old path — only the aggregate exemplar moved).
  - Repo-wide grep for `Aggregates.Profiles`, `web-domain/aggregates/profile` (as opposed to the
    still-valid `aggregates/overview.md`), `persistence/ef-principal-store`, `hubs/chat-hub`,
    `configuration/web-authn-options`, `features/**/commands/`, `features/**/queries/`,
    `client-to-server`, `server-to-client` (outside kanban and template-smoke artifacts): the only
    hits are historical kanban records (expected/excluded), the `tw-feature-placement` skill's
    intentional present-tense description of the anti-pattern it tells agents to avoid, and the one
    stale chat comment above.
- **No project-membership drift:** `feature-filename-grammar.g.props` composes each layer project's
  `<Compile Include>` as `$(WebFeatureTreeRoot)/**/*-<layer>.cs` — a suffix-only, fully recursive
  glob with no folder-depth dependency. Adding a use-case subfolder cannot move a file into a
  different project's compilation unit; this is a structural guarantee, not a per-file coincidence,
  and it is consistent with the `dev build`/`dev test`/`dev template-smoke` gates already green at
  three checkpoints per the review framework.
- **Purpose/Design regions:** spot-checked every moved file with a nontrivial Design region
  (`web-authn-options-application.cs`, `agent-token-options-application.cs`,
  `agent-token-authentication-scheme-server.cs`, `chat-hub-server.cs`,
  `chat-hub-service-server.cs`, `ef-principal-store-infrastructure.cs`, `profile-domain.cs`,
  `profile-id-domain.cs`) — all read correctly post-move, with `web-authn-options-application.cs`
  and `agent-token-options-application.cs` explicitly reconciled to describe "compiles into
  web-application" / the new namespace rather than the old "lives in web-application" phrasing.
  Only the one chat-feature Design region above is stale (Issue 1).
- **Docs quality:** both skill updates (`tw-feature-placement`, `tw-web-api-contracts`) are
  present-tense, carry no task numbers/dates, and accurately describe the new shape — including the
  shared-at-root rule and an explicit worked mention of the `hello/hello` single-operation literal
  case and the direction-folder-collapse rule for hubs (`client-to-server`/`server-to-client`
  named only as the anti-pattern being dissolved, which is appropriate skill content, not stale
  narration).
- **126-002's RFC checklist item:** `kanban/done/126-review-.../rfc/rfc.md` already contains the
  "F4/D2 premise correction" paragraph (post-tally section) recording that the domain layer was
  never actually empty — the Profile aggregate lived in the layer folder — and that D2's
  resolution stands. Nothing further needed there.
