# Axis-by-axis decisions (Steve, in-chat, 2026-07-21)

Working through the seven decision axes conversationally; positions recorded here are **Steve's**,
each pending validation noted. Survey evidence: `survey-*.md`; my synthesis (`survey-synthesis.md`)
is raw input only.

## Axis 1 — Slice granularity: feature-cohesive folders, layer projects by filename globs ✅ (pending spike)

**Decision (Steve):** Do BOTH cohesion and decoupling by decoupling disk layout from project
membership — the insight that folder=project is a default glob, not a requirement.

- **Feature folder is the unit of cohesion on disk**: all of a feature's files live together in
  one folder, spanning layers.
- **Layer projects remain the unit of compilation/deployment** (application, contracts, domain,
  infrastructure, server, …), living in their own folder that describes deployment artifacts.
  Each layer csproj includes feature files via **static filename globs** — no generated props,
  no registry dependency in MSBuild.
- **Filename grammar:** `<name>[-<function>]-<layer>.cs`
  - `get-roles-handler-application.cs` — function + layer; analyzer enforces the pairing
    (`-handler-` ⇒ `-application`, `-endpoint-` ⇒ `-server`, unknown function = error) AND the
    archetype shape (one handler class, no HTTP types in application, aggregate has nested
    Invariants, …).
  - `role-mapper-application.cs` — escape hatch falls out of the grammar: no function segment.
  - `get-roles-contracts.cs` — for contracts, function ≡ layer, so the function segment is
    dropped (avoids `-contract-contracts` stutter); every `-contracts.cs` file is held to the
    operation-contract shape.
- **Function→layer registry** is consumed ONLY by the analyzer (globs are layer-suffix-based and
  registry-free). Filename redundancy (function + layer) is a deliberate two-things-must-agree
  seam the analyzer checks.
- **Exactly-one-project membership is REQUIRED** — analyzer/build check; a file matched by zero
  or two layer globs is a build error.
- **Spa stays conventionally separate** — the Razor SDK's own sourcegen/item types make .razor
  a poor fit for cross-folder globbing; revisit only if a spike proves otherwise.
- Prior art note: this is Bazel's source-layout/build-target separation expressed in MSBuild;
  none of the surveyed repos (ardalis, FSH, JT, CASA) attempt it — they all accept folder=project
  and are forced to choose between feature cohesion (project-per-module) and layer decoupling
  (project-per-layer). This scheme gets both, and only works because enforcement is
  analyzer-based.
- **Validation needed (spike, one slice):** IDE/design-time-build behavior with cross-folder
  globs, duplicate-analysis avoidance, glob perf, dotnet-new template engine interaction.

## Axis 2 — Contracts placement + assembly granularity per layer ✅

**Decision (Steve):**

- **Contracts: single assembly.** Not a compromise — contracts are definitionally the public,
  shared, serialized seam; `internal` there is near-meaningless and module separation is a
  non-goal in that layer. TWA0009's namespace rules already govern who may consume what.
- **Implementation layers (application/domain/infrastructure): default single assembly per
  layer, enforced by TWA0009** — with **per-module assembly splits as the earned exception**
  (module gets big/sensitive/heading toward service extraction). Under the axis-1 filename-glob
  scheme, a split is a csproj/glob operation — files never move — so the template starts simple
  and extraction stays cheap ("if modules need extracting we are in a good place").
- **Server / spa: single** (they are the deployment artifacts).
- Key insight enabling this: assembly granularity is **per-layer independent** under axis 1 —
  the packaging choice (and therefore what `internal` means) can differ per layer. `internal`
  stays layer-wide in the default posture; module-privacy is expressed to the analyzer
  (TWA0009), not the compiler, until a module earns its own assembly.

## Axis 3 — Async cross-slice channel: OPEN

## Axis 4 — Intra-slice layering enforcement: OPEN

## Axis 5 — Persistence shape (joint with 113): OPEN

## Axis 6 — Template flag mechanics: OPEN

## Axis 7 — .NET 11 posture: OPEN
