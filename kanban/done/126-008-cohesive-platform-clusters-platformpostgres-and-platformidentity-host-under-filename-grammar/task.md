# Cohesive platform clusters: platform/postgres and platform/identity-host under filename grammar

## Description

From the 126 platform/projects folder review (maintainer + orchestrator conversation,
2026-07-27): two platform concerns are currently split across layer folders exactly the way
product features were before use-case folders. Give them cohesive homes under a new
`source/container-apps/web/platform/` tree using the same filename-grammar mechanics (suffix
keeps each file in its compilation unit; folder gives cohesion).

**Cluster 1 — `platform/postgres/` (5 files, currently split across two folders):**

| Current | New |
|---|---|
| `web-infrastructure/configuration/postgres-db-options.cs` | `platform/postgres/postgres-db-options-infrastructure.cs` |
| `web-infrastructure/persistence/postgres-db-context.cs` | `platform/postgres/postgres-db-context-infrastructure.cs` |
| `web-server/modules/postgres-db-module.cs` | `platform/postgres/postgres-db-module-server.cs` |
| `web-server/configuration/environment-checks/postgres-db-environment-check.cs` | `platform/postgres/postgres-db-environment-check-server.cs` |
| `web-server/hosted-services/postgres-db-context-startup-hosted-service.cs` | `platform/postgres/postgres-db-context-startup-hosted-service-server.cs` |

**Cluster 2 — `platform/identity-host/` (7 files — the seam impls + their constants):**

| Current | New |
|---|---|
| `web-server/services/cookie-browser-session-service.cs` | `platform/identity-host/cookie-browser-session-service-server.cs` |
| `web-server/services/http-current-principal-accessor.cs` | `platform/identity-host/http-current-principal-accessor-server.cs` |
| `web-server/services/http-request-host-accessor.cs` | `platform/identity-host/http-request-host-accessor-server.cs` |
| `web-server/services/agent-caller-context.cs` | `platform/identity-host/agent-caller-context-server.cs` |
| `web-server/configuration/identity-session-defaults.cs` | `platform/identity-host/identity-session-defaults-server.cs` |
| `web-server/configuration/agent-token-defaults.cs` | `platform/identity-host/agent-token-defaults-server.cs` |
| `web-server/configuration/credential-management-defaults.cs` | `platform/identity-host/credential-management-defaults-server.cs` |

**Stays put (explicitly out of scope):** the four seam interfaces in
`web-application/abstractions/` (interface-in-application/impl-in-server is the point of the
seam; foundation promotion is a separate future decision), `web-infrastructure-module.cs`
(assembly-level DI hook), sample-options/sample-environment-check (host exemplars), program.cs
and appsettings (the host), `mock-user-ids.cs` / `assembly-extensions.cs` (dispositions
deferred).

**CRITICAL MECHANIC — glob coverage:** the grammar's suffix globs in
`source/container-apps/web/msbuild/feature-filename-grammar.g.props` are rooted at the
FEATURES tree only. Files moved to `web/platform/**` with layer suffixes will silently FALL OUT
of compilation unless coverage is added. Required approach: extend the generated props (via its
generator/source in the convention-analyzers project — g.props is generated, do not hand-edit)
or the msbuild seam so a second root (`platform/`) participates in the same per-project suffix
globs and the membership guard. If that extension turns out to be non-trivial in the generator,
STOP and report (design issue) rather than hand-editing generated files or adding ad-hoc csproj
includes.

**Namespaces do NOT change** (repo rule: namespaces don't track folders; these are platform
files, not slice-membership declarations — `…Features.<Id>` adoption is exactly what these
should NOT get; TWA0009 must keep seeing them as platform). Existing namespaces
(`TimeWarp.Architecture.Persistence`, `.Services`, `.Configuration`) stay.

**Known hazards (from prior migrations of these same files' neighbors):**

- `.template.config/template.json` `(!postgres)` exclude block lists ALL FIVE postgres file
  paths — every one must be updated to its new path (the H1 class from 126-001/002).
- `#if postgres` regions inside moved files: unaffected (same csproj, same DefineConstants) —
  verify TWA0010 stays quiet.
- Purpose/Design regions: reconcile any that narrate old folder homes (the M1 class).
- Grep for path-based references to the old locations in docs/tests/workflows before and after.

## Checklist

- [x] Resolve glob coverage for `platform/` in the g.props generator + membership guard
      (STOP if non-trivial — design issue, not workaround)
- [x] `git mv` + grammar-rename the 12 files per the tables; delete emptied subfolders
- [x] Update the five postgres paths in `.template.config/template.json` (!postgres) excludes
- [x] Reconcile Purpose/Design regions narrating old homes
- [x] Sweep for stale path references (docs, tests, workflows, skills)
- [x] Update AGENTS.md Layout (add `platform/` line) + `skills/tw-feature-placement/SKILL.md`
      (platform tree: same grammar, NOT slice-namespaced, when to use platform/ vs features/ vs
      host — present tense, no history)
- [x] Gates: `dev build` 0/0 (FULL rebuild — glob/generator surface changed), `dev test`,
      `dev template-smoke` both matrices via current-code path (stale `./bin/dev` footgun);
      SmokeNoPostgres proves the updated excludes still fully strip postgres content
- [x] Runtime spot check: postgres profile boots (aspire or web-server with connection) —
      module/hosted-service/environment-check all still registered

## Notes

- Parent: 126. Origin: platform/projects folder review (2026-07-27) — the postgres cluster was
  the standout "more cohesion in a shared folder" finding (5 files, 1 concern, 2 folders); the
  identity-host cluster is the same shape (cohesive with each other, not with program.cs).
  Maintainer approved both clusters as one task.
- Deliberately NOT moving these into `features/identity` — slices don't own host wiring; the
  seam/impl split stays.
- Related: 126-007 (marker generation) drains the same folders; either order works.
- After both land, the layer folders approach "csproj + host files only" — re-examine whether
  anything further is wanted for the projects/-as-pure-selector idea then.


## Implementation Plan (2026-07-27)

### Goal
Cohesive `web/platform/{postgres,identity-host}/` clusters using filename-grammar layer suffixes; namespaces unchanged; features/ mechanics extended for a second root.

### Glob coverage (critical)
1. `generate-feature-filename-grammar.py`: emit hybrid Compile globs for both `$(WebFeatureTreeRoot)` (Link=features\…) and `$(WebPlatformTreeRoot)` (Link=platform\…) per layer.
2. `feature-membership.targets`: set `WebPlatformTreeRoot` to `../platform`; membership guard scans both trees (Exists on platform).
3. Rebuild convention-analyzers to regenerate `feature-filename-grammar.g.props` — do not hand-edit g.props.

### Moves (git mv + rename)
**platform/postgres/** (5): infrastructure ×2 + server ×3 per task table.
**platform/identity-host/** (7): all server-suffixed.
Delete emptied subfolders under web-server/services, configuration/*, hosted-services, modules, web-infrastructure configuration/persistence if empty.

### Template / docs
- Update 5 paths in `.template.config/template.json` (!postgres) excludes.
- AGENTS.md Layout: add `platform/` line.
- `skills/tw-feature-placement/SKILL.md`: when to use platform/ vs features/ vs host (present tense).
- Reconcile Purpose/Design regions if they narrate old folder homes.
- Grep stale path references.

### Gates
FULL `dev build` 0/0, `dev test`, `dev template-smoke` both matrices (SmokeNoPostgres strips postgres).

## Session

- Created: 2026-07-27 — filed from maintainer-approved proposal (task b of two).
- Orchestrator / implement / review: grok-build 2026-07-27

## Results

### What was implemented
Cohesive `web/platform/postgres/` (5 files) and `web/platform/identity-host/` (7 files) clusters using the same `-layer` filename grammar as `features/`. Extended the grammar generator and membership guard with a second root `WebPlatformTreeRoot`. Updated template `(!postgres)` excludes. Documented features vs platform vs host placement.

### Files changed
| Path | Action |
|------|--------|
| `generate-feature-filename-grammar.py` + `.g.props` | Dual hybrid globs (features + platform) |
| `feature-membership.targets` | WebPlatformTreeRoot + membership scan |
| `web/platform/postgres/*` | 5 moves with -infrastructure/-server suffixes |
| `web/platform/identity-host/*` | 7 moves with -server suffixes |
| `.template.config/template.json` | !postgres path updates |
| AGENTS.md, tw-feature-placement skill | Layout + placement table |
| SSOT drift test | Locks WebPlatformTreeRoot |

### Key decisions
1. Second tree root via generator (not hand-edited g.props or ad-hoc csproj includes).
2. Namespaces unchanged (non-Features).
3. Seam interfaces stay in web-application/abstractions.

### Test outcomes
| Gate | Result |
|------|--------|
| `dev build` | 0/0 |
| `dev test` | All passed |
| `dev template-smoke` | Both matrices OK; SmokeNoPostgres strips all platform/postgres files |
| Analyzer SSOT drift | Pass after M1 fix |

### Review
- Rounds: 2; effort 1 general
- Final: 0 open (1 suggestion fixed)
- Disposition: clean
- Paths: review/

## Results addendum — independent verification (round 3, 2026-07-27)

Cross-vendor verification (Claude reviewing the Grok implementation): confirmed, no bugs — full
record at [review/round-3/independent-verification.md](review/round-3/independent-verification.md).
Machinery proven live, not just read: MSBuild `-getItem:Compile` evaluation shows all 12 files
in their original compilation units with platform Link metadata; a planted suffix-less file
under platform/ failed the membership guard naming both trees; generator emission matches the
checked-in g.props line-by-line; SmokeNoPostgres artifacts show platform/postgres fully
stripped with identity-host intact. Independent gate battery green (build 0/0, 15 test projects
0 failed, smoke both matrices). Recurring process nit: round-1 review was diff-only (no
empirical run by the reviewer) — third occurrence across 126-005/006/008.
