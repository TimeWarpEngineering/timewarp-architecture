# Group container-app artifact folders under projects/ (web first, then api/grpc/aspire)

## Description

Maintainer decision (2026-07-27, closing the 126 folder program): make the placement model
visible in the tree itself. Each container-app family groups its artifact (project) folders
under `projects/`, so the family root reads as the one-sentence placement rule:

```text
web/
  features/    # product concerns
  platform/    # platform concerns
  projects/    # the artifacts: web-contracts/ web-application/ web-domain/
               #                web-infrastructure/ web-server/ web-spa/
  msbuild/     # build machinery
```

Scope: **all container-app families, executed in stages — web first** (maintainer sequencing).

- **web** (stage 1): 6 project folders → `web/projects/`. The hard one — all the machinery
  lives here.
- **api** (stage 2): 5 project folders → `api/projects/`.
- **grpc** (stage 2): 5 project folders → `grpc/projects/`.
- **aspire** (stage 2): `aspire-app-host` + `aspire-service-defaults` → `aspire/projects/`.
- **yarp**: single-project family (`yarp/` IS the project — appsettings at its root); nothing
  to group. Leave unchanged; note the asymmetry in the placement guide ("a single-project
  family is its own artifact folder"). If uniformity is later wanted, that is a separate call.

Naming decided: **`projects/`** (maintainer's term; every occupant is a csproj home).
`artifacts/` rejected (collides with gitignored build-output root), `deployables/` rejected
(false for library projects). Folder names inside stay identical to project names
(`projects/web-server/web-server.csproj`).

**Path blast radius (largest of the program — inventory BEFORE moving, per family):**

- `timewarp-architecture.slnx` project paths (including the `<!--#if (false) -->` platform
  blocks — keep their paths in sync too).
- Every `ProjectReference` repo-wide (tests tree references
  `../../../source/container-apps/web/web-*`; cross-family references e.g. web-server →
  web-spa; aspire-app-host → all servers; foundation references FROM container-apps).
- `.template.config/template.json`: every exclude/modifier path touching moved folders
  (postgres excludes, sourceModifiers, ide configs).
- Directory.Build.props/targets inheritance is walk-up and survives deepening BUT verify:
  `web/msbuild/feature-membership.targets` + `feature-filename-grammar.g.props` — how csprojs
  import them and how `WebFeatureTreeRoot`/`WebPlatformTreeRoot` are anchored (if relative to
  the importing csproj, one level deeper breaks them; if anchored to the msbuild file's own
  directory, they survive — VERIFY, and if the generator emits relative segments, fix at the
  generator per SSOT rule, never hand-edit g.props).
- Aspire `AddProject` project paths (aspire-app-host.csproj) — resource names themselves must
  NOT change (ServiceNames rule / TWA0007).
- CI workflow path filters (`.github/workflows/*.yml`), `dev` CLI hardcoded paths
  (template-smoke scan roots include `source/container-apps` — root-level, survives; verify
  any deeper literals), docs/skills path references (placement guide examples say
  `web/features/…` — unchanged; artifact-folder references like "web-server/" in docs need a
  sweep), `HowToRemoveDemoFeatures.md` and friends.
- launchSettings/appsettings are inside the moved folders — no path content changes expected,
  but Properties/ paths appear in some tooling configs (verify `.vscode/`, `Directory.Build.*`).

**Execution rules:** pure `git mv` of whole project folders (no renames inside); one family
per commit with full gates between (`dev build` 0/0, `dev test`, `dev template-smoke` both
matrices — template content paths change every stage); update the placement guide + AGENTS.md
Layout diagram in the same stage as web (stage 1) so docs track the tree; per the 126-011
regression lesson, treat SmokeNoPostgres as the canary for template.json path mistakes.

**Stage gate:** after web (stage 1) lands green, pause for maintainer review before stage 2
(api/grpc/aspire) — sequencing was an explicit maintainer instruction.

## Checklist

### Stage 1 — web

- [x] Path-reference inventory for web-* (slnx, ProjectReferences, template.json, msbuild
      anchoring verification, aspire AddProject, CI filters, docs/skills)
- [x] Resolve the WebFeatureTreeRoot/WebPlatformTreeRoot anchoring question BEFORE moving
      (generator fix if emission is csproj-relative)
- [x] `git mv` the 6 web project folders → `web/projects/`; update all inventoried references
- [x] Update `tw-feature-placement` opening table + AGENTS.md Layout diagram (projects/ level)
- [x] Gates: `dev build` 0/0, `dev test` all projects, `dev template-smoke` both matrices
- [x] Maintainer review checkpoint before stage 2

### Stage 2 — api, grpc, aspire

- [x] Same inventory + move + reference sweep per family (one commit per family)
- [x] yarp: no move; placement-guide note about single-project families
- [x] Gates after each family; full battery + smoke at the end

## Notes

- Lineage: successor to the 126 folder program (126-001/002/007/008/011 made the artifact
  folders pure enough that grouping them is now honest). Filed as a NEW top-level task per the
  parent-done-requires-children-done ruling — 126 is closed.
- Related pending: task 118's marketplace will evaporate `web-server/configuration/` (samples
  → concern-owned options); independent of this move.
- Cross-family references make family-at-a-time staging slightly awkward (tests/aspire
  reference web paths in stage 1 while api/grpc paths are still old) — that is fine; each
  stage's gates prove the mixed state.

### Implementation plan (2026-07-27)

# Implementation Plan: Task 127 — Group Container-App Artifact Folders Under `projects/`

## Goal

Make the placement model visible in the tree. Each multi-project container-app family groups its csproj homes under `projects/`, so the family root reads as the placement rule:

```text
web/
  features/    # product concerns (unchanged)
  platform/    # platform concerns (unchanged)
  projects/    # artifact folders (csproj homes)
  msbuild/     # build machinery (unchanged)
```

Naming is fixed: **`projects/`** only. Inner folder/csproj names stay identical (`projects/web-server/web-server.csproj`).

---

## Pre-verified: MSBuild anchoring (do this first, then move)

### Verdict: **safe to move without generator changes**

`WebFeatureTreeRoot` / `WebPlatformTreeRoot` are **not csproj-relative**. They are anchored to the MSBuild file that defines them via `MSBuildThisFileDirectory` in `source/container-apps/web/msbuild/feature-membership.targets`:

- Roots resolve to `web/features` and `web/platform` regardless of where the consuming csproj lives
- Import chain: `web/Directory.Build.targets` → `msbuild/feature-membership.targets` (also `MSBuildThisFileDirectory`-anchored)

### Directory.Build discovery after one extra directory level

Projects at `web/projects/web-server/` still walk up and find web/Directory.Build.targets and container-apps/Directory.Build.props.

**No generator fix required.** Do **not** hand-edit `feature-filename-grammar.g.props`.

---

## Inventory approach (before any `git mv`)

Run a frozen inventory per family; classify hits as:
- **A — mechanical path**: update with move
- **B — conceptual name only** (assembly name, ServiceNames): leave
- **C — historical** (kanban/done): leave
- **D — still-valid shared trees** (features/platform/msbuild): leave

---

## Exact move lists

### Stage 1 — web (one commit)
6 folders: web-contracts, web-application, web-domain, web-infrastructure, web-server, web-spa → `web/projects/`

Do not move: features/, platform/, msbuild/, Directory.Build.targets, tests/

### Stage 2 — one family per commit
- api: 5 projects → api/projects/
- grpc: 5 projects → grpc/projects/
- aspire: aspire-app-host + aspire-service-defaults → aspire/projects/
- Order: api → grpc → aspire
- yarp: no move; document asymmetry

---

## Path depth rule

Moving one level deeper: outward relative paths need +1 `../`. Sibling refs among co-moved projects stay same form. External → web: insert `projects/` segment.

Special: web-spa web-contracts ref today uses non-sibling detour; normalize to `..\web-contracts\web-contracts.csproj`.

---

## Stage 1 reference surface
1. timewarp-architecture.slnx (5 web entries; web-spa absent from slnx — keep)
2. All 6 web csprojs (outward ../, DockerfileContext, constants Compile Include)
3. External: aspire-app-host, yarp, timewarp-testing, 5 web test csprojs
4. .template.config/template.json spa excludes under (!grpc)/(!api) need projects/
5. Aspire AddProject resource names: DO NOT change
6. CI path filters: likely no change
7. scripts: describe.ps1, postgres/ef-shared-variables.ps1
8. Docs: AGENTS.md Layout, tw-feature-placement SKILL.md + yarp note, HowToRemoveDemoFeatures.md

---

## Commit strategy
1. web (full gates) → **pause for maintainer review**
2. api → gates
3. grpc → gates  
4. aspire → gates + final battery

Gates after each: `dev build` 0/0, `dev test`, `dev template-smoke` both matrices. SmokeNoPostgres is the canary (126-011 lesson).

---

## What NOT to change
- Project/assembly names, ServiceNames, feature/platform/msbuild trees
- yarp layout, test tree locations
- hand-edit of feature-filename-grammar.g.props
- no artifacts/ naming

---

## Success criteria
Family roots show features+platform+projects+msbuild (web); api/grpc/aspire show projects/; yarp unchanged with documented asymmetry; all gates green; docs match tree.

## Session

- Created: 2026-07-27 — filed from maintainer decision (all container-apps, web first).
- 2026-07-27 — planning completed: MSBuild anchoring pre-verified (safe to move, no generator fix); full implementation plan appended under Notes.
- 2026-07-27 — Stage 1 landed (web under projects/); gates green; **paused for maintainer review before Stage 2** (api/grpc/aspire still pending).
- 2026-07-27 — Phase 4b review (effort 1, general): disposition **clean** under `review/`; 0 open findings.
- 2026-07-28 — Maintainer approved proceeding past stage 1 checkpoint; Stage 2 executed (api → grpc → aspire).
- 2026-07-28 — Phase 4b round 2 (stage 2): disposition **clean**; 0 open findings. Results written; task done.

## Results

**Status:** success — both stages complete; placement model visible in tree for all multi-project container-app families.

### What shipped

| Stage | Family | Commit(s) |
|-------|--------|-----------|
| 1 | web (6 projects → `web/projects/`) | `267b4523`, `ad19d511` |
| 2 | api (5 → `api/projects/`) | `156ccb72` |
| 2 | grpc (5 → `grpc/projects/`) | `f62064da` |
| 2 | aspire (2 → `aspire/projects/`) | `6e049ff1` |
| — | yarp | no move; asymmetry documented |

**End-state tree:** multi-project families expose `projects/` for csproj homes; web also keeps `features/`, `platform/`, `msbuild/` as siblings of `projects/`; yarp remains a flat single-project family.

**Mechanical updates:** slnx paths; ProjectReferences (sibling form kept, outward +1 `../`, external insert `projects/`); template.json spa excludes (stage 1); grpc Dockerfile COPY/WORKDIR; DockerfileContext on servers; aspire.config.json + dev-cli `run-command.cs`; scripts; AGENTS.md + `tw-feature-placement` + HowToRemoveDemoFeatures.

**Unchanged by design:** project/assembly/InternalsVisibleTo names; Aspire ServiceNames / AddProject resource strings (TWA0007); features/platform/msbuild trees; tests tree locations; yarp layout; no hand-edit of grammar g.props.

### Gates

After stage 1 and after **each** stage 2 family: `dev build` 0/0 · `dev test` all green · `dev template-smoke` both matrices (SmokeDefault + SmokeNoPostgres canary).

### Review (Phase 4b)

| Field | Value |
|-------|--------|
| Effort / roster | 1 · general only |
| Rounds | 2 (stage 1 + stage 2) |
| Final counts | open 0 · fixed 0 · wontfix 0 (all severities) |
| Disposition | **clean** |
| Paths | `review/review-framework.md`, `review/round-1/`, `review/round-2/`, `review/disposition.md` |

### Residual / deferred

- `api-server/Dockerfile` still has pre-historical PascalCase path debt (pre-existing; not stage-2 residual of kebab-without-`projects/`). Out of scope for this task.
- Task 118 marketplace (sample options) independent.

## Stage 1 progress (complete)

**Commits:**
- `267b4523` — `refactor(web): group artifact folders under web/projects/`
- `ad19d511` — `chore(web): fix spa wwwroot/js gitignore after projects/ move`

**Tree:** `web/{features,platform,projects/{web-contracts,web-application,web-domain,web-infrastructure,web-server,web-spa},msbuild}`

**Gates (stage 1):** `dev build` 0/0 · `dev test` all green · `dev template-smoke` both matrices (incl. SmokeNoPostgres canary)

**Review:** `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md` — outcome `clean`, rounds 1, effort 1 general only, final open 0

## Stage 2 progress (complete; awaiting orchestrator Results + done)

**Commits:**
- `156ccb72` — `refactor(api): group artifact folders under api/projects/`
- `f62064da` — `refactor(grpc): group artifact folders under grpc/projects/`
- `6e049ff1` — `refactor(aspire): group artifact folders under aspire/projects/`

**Trees:**
- `api/projects/{api-contracts,api-application,api-domain,api-infrastructure,api-server}`
- `grpc/projects/{grpc-contracts,grpc-application,grpc-domain,grpc-infrastructure,grpc-server}`
- `aspire/projects/{aspire-app-host,aspire-service-defaults}`
- `yarp/` left flat (single-project family; placement skill note already present)

**Gates (after each family):** `dev build` 0/0 · `dev test` all green · `dev template-smoke` both matrices (SmokeDefault + SmokeNoPostgres)

**Docs:** AGENTS.md Layout diagram updated for api/grpc/aspire under `projects/` + yarp flat note.

**Still open:** orchestrator Results section + move task to done after review.
