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

- [ ] Path-reference inventory for web-* (slnx, ProjectReferences, template.json, msbuild
      anchoring verification, aspire AddProject, CI filters, docs/skills)
- [ ] Resolve the WebFeatureTreeRoot/WebPlatformTreeRoot anchoring question BEFORE moving
      (generator fix if emission is csproj-relative)
- [ ] `git mv` the 6 web project folders → `web/projects/`; update all inventoried references
- [ ] Update `tw-feature-placement` opening table + AGENTS.md Layout diagram (projects/ level)
- [ ] Gates: `dev build` 0/0, `dev test` all projects, `dev template-smoke` both matrices
- [ ] Maintainer review checkpoint before stage 2

### Stage 2 — api, grpc, aspire

- [ ] Same inventory + move + reference sweep per family (one commit per family)
- [ ] yarp: no move; placement-guide note about single-project families
- [ ] Gates after each family; full battery + smoke at the end

## Notes

- Lineage: successor to the 126 folder program (126-001/002/007/008/011 made the artifact
  folders pure enough that grouping them is now honest). Filed as a NEW top-level task per the
  parent-done-requires-children-done ruling — 126 is closed.
- Related pending: task 118's marketplace will evaporate `web-server/configuration/` (samples
  → concern-owned options); independent of this move.
- Cross-family references make family-at-a-time staging slightly awkward (tests/aspire
  reference web paths in stage 1 while api/grpc paths are still old) — that is fine; each
  stage's gates prove the mixed state.

## Session

- Created: 2026-07-27 — filed from maintainer decision (all container-apps, web first).
