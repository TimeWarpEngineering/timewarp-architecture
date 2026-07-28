# Axis-1 for api and grpc families: multi-family grammar machinery, features and platform trees

## Description

Maintainer decision (2026-07-28): bring the api and grpc families to the same axis-1 shape as
web — family-level `features/` (+ `platform/` where platform concerns exist) cherry-picked into
the `projects/` compilation units by filename-grammar suffix globs. This is direct preparation
for task 118: the marketplace's agent-plane endpoints target api-server (host-role mapping
recorded on 118), and its code needs the grammar there to land cleanly.

**Current state (survey 2026-07-28):** api and grpc are pre-axis-1 — project-local `features/`
trees inside the project folders, `queries/` subfolders, suffix-less filenames
(`get-weather-forecasts.cs`, `hello-request.cs`). Content is demo-only (weather-forecast;
hello + superhero), so the migration is small in files but machinery-heavy.

**Stage 0 — machinery generalization (the real work):**

- The grammar pipeline is web-only today: the registry generator emits
  `web/msbuild/feature-filename-grammar.g.props` with `WebFeatureTreeRoot`/`WebPlatformTreeRoot`
  and per-project-conditioned globs; `feature-membership.targets` guards those two roots.
  Generalize to per-family emission (api/msbuild/, grpc/msbuild/ — or one parameterized props
  consumed per family; implementer proposes, SSOT rule absolute: all changes via the generator,
  never hand-edited outputs; extend the SSOT drift test to the new families).
- Anchoring rule from 127: keep tree roots anchored to the msbuild file's own directory.
- TWA0009 SliceRoot semantics: confirm the analyzer's slice detection covers the new family
  namespaces (`…Features.<Id>` in api/grpc contracts) or is web-scoped — extend deliberately,
  not accidentally.

**Stage 1 — api family:** rehome demo content to `api/features/weather-forecast/` with grammar
names + use-case folders (e.g. `get-weather-forecasts/get-weather-forecasts-contracts.cs`,
`…-handler-application.cs`); evaluate api-server's `features/base/` (base-error, base-exception
— platform-ish error shapes?) and `generic-pipeline-behavior.cs` against the placement rule
(concern vs bootstrap; surface judgment calls, don't guess); api-application-module follows the
modules-follow-concerns rule.

**Stage 2 — grpc family:** same treatment (hello, superhero slices); note grpc's
proto/generated-service specifics — the grammar registry may need grpc-appropriate layer
mapping decisions (contracts vs server for service interfaces/DTOs); surface anything that
does not map cleanly rather than forcing it.

**Gates every stage:** `dev build` 0/0, `dev test`, `dev template-smoke` both matrices
(api/grpc are template flags — the `!api`/`!grpc` exclude paths in template.json and slnx
conditionals are part of the blast radius, and SmokeNoPostgres-class canaries apply). Stage
checkpoint with the maintainer after Stage 0 (machinery design is the risk concentration).

## Checklist

- [ ] Stage 0: multi-family generator emission + membership guards + SSOT drift-test extension;
      maintainer checkpoint on the design before family migrations
- [ ] Stage 1: api content migration (grammar names, use-case folders, placement judgments
      surfaced); template.json/slnx `!api` path updates; gates
- [ ] Stage 2: grpc content migration; `!grpc` path updates; gates
- [ ] Update tw-feature-placement skill + AGENTS.md: grammar now family-generic (drop
      web-only framing); worked examples stay web-based
- [ ] Full battery + both smoke matrices at the end

## Notes

- Lineage: 126/127 folder program successor; prerequisite direction for 118's agent-plane
  endpoints (see 118 host-role mapping note, 2026-07-28).
- Sequencing vs 118: this task standardizes the ground; 118 builds on it. Do not add
  marketplace content here.
- yarp: single-project family, no concern trees — out of scope (127 precedent).

## Session

- Created: 2026-07-28 — filed from maintainer decision after the api/grpc weight discussion.

## Checkpoint record

- Stage-0 design APPROVED (Steve, 2026-07-28): Decisions 1–5 as proposed (per-family generated
  g.props via explicit per-family Exec; registry unchanged; mirrored per-family
  Directory.Build.targets + feature-membership.targets with own canonical hosts and platform
  roots defined as no-ops; drift test parameterized over documented three-family list; TWA0009
  reframed to document-as-already-universal).
- Decision 6a RESOLVED (Steve, 2026-07-28): grpc service interfaces (i-hello-service,
  i-superhero-service) take the seam-interface pattern — **`-application.cs`**, living beside
  their `-server.cs` implementations in the use-case folder (identity-host precedent).
- Remaining maintainer questions queued one-at-a-time: 6b protobuf codegen tool placement,
  6c greeter slice naming, api base-error/base-exception, api generic-pipeline-behavior.
