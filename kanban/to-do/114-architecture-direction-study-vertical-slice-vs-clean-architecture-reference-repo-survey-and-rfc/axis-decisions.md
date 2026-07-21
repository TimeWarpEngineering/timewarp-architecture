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

## Axis 3 — Async cross-slice channel ✅ (delivery substrate deferred to 113)

**Decision (Steve):** Adopt the **RiverBooks bridge pattern, in-process** as the golden channel:

- Domain events stay module-private; a handler in the module's `Integrations/` area translates
  them into **integration events** — public contract types (`<name>-event-contracts.cs` in the
  axis-1 grammar) delivered via mediator notifications, in-process by default.
- TWA enforcement: domain event types must never appear outside their slice; integration events
  are the only event types allowed across the boundary.
- **Outbox (FSH pattern) is explicitly deferred to task 113** — durable delivery is a
  persistence decision (same-transaction write + dispatcher), to be weighed there alongside the
  postgres flag / no-postgres fallback story.
- **Actor-pattern interaction (113 RFC)**: the channel is designed contract-first so the
  delivery substrate is swappable (VCMM-04's lesson — same contract, different transport). If
  actors are adopted for aggregates, actor-hosted aggregates publish the SAME integration-event
  contract types (via whatever substrate — mediator, Akka event bus, Orleans streams); the
  bridge shape (domain event → integration event translation) maps cleanly onto actor
  processing. Axis-3's contract-level design must not bake in mediator-notification delivery as
  an assumption — keep the publish seam abstract.

## Axis 4 — Intra-slice layering enforcement ✅ (resolved by construction)

**Decision (Steve, agreed 2026-07-21):** No new mechanism needed — axes 1+2 dissolve the problem
that made RiverBooks reach for NsDepCop and FSH for LayerDependencyTests (their layers are
folders inside one module project; ours remain separate projects):

- **Layer directionality = ordinary project reference graph** (domain.csproj references nothing
  outward) — compile-time, free.
- **Package discipline = per-layer csproj + CPM** (domain carries no EF PackageReference, so
  domain files using DbContext don't build) — free.
- **Residual enforcement = the axis-1 archetype analyzer** (function-segment shape rules:
  `-handler` can't use HTTP types, `-endpoint` can't hold business logic, aggregates carry
  `Invariants`, …) with **teaching-quality diagnostics** — errors name the layer the offending
  dependency belongs to ("EF Core types are infrastructure; move to `<name>-infrastructure.cs`
  or remove the dependency"), since there's no layer folder structure left to teach the scheme.

## Axis 5 — Persistence shape + actors ✅ (tech choice via dual spike in 113)

**Decision (Steve, 2026-07-22):**

- **State-store EF is the golden path** — no event sourcing for now (paradigm shift without
  team depth; revisit only if a real need emerges). Actors do NOT require it: Akka.Persistence
  is Akka's optional event-sourced durable-state mechanism, and Orleans is state-store-first
  (`IPersistentState<T>` snapshots) — actors + state-store EF is fully coherent.
- **DbContext + schema per slice** (RiverBooks/FSH consensus pattern) on the existing
  `PostgresDbContext` golden seam (invariants guard + version token).
- **Actors: optional, with a shipped example.** The aggregate stays EF-golden by default;
  specific aggregates earn actor hosting (single-writer serialization, in-memory hot state,
  long-lived processes). The actor is a concurrency/lifetime shell around the SAME golden
  aggregate + EF path — no second persistence stack. Natural first example: the 104 credit
  ledger (per-principal balance = textbook high-contention single-writer).
- **Actor technology: decided empirically, not in the abstract** — dual spike in 113 (child
  task): same example aggregate on Akka.NET and Orleans, pick with hands on. License is NOT a
  differentiator (corrected 2026-07-22: Akka.NET is Apache 2.0 — the BSL move was JVM
  Akka/Lightbend only; Orleans MIT). Real axes: consumer approachability, Aspire wiring,
  golden-aggregate/EF fit, upstream-port freeze (Akka.NET independent since JVM 2.6.20),
  community/support (Steve knows the Akka.NET author).
- The axis-3 substrate-agnostic publish seam guarantees the losing spike leaves no residue.

## Axis 6 — Template flag mechanics ✅ (non-blocking, deal with as it comes)

**Decision (Steve):** Keep in-file `#if` as the mechanism of record — it's needed for
line-granular seams, it works, and TWA0008/0010 guard it. Folder-exclusion `sources.modifiers`
(FSH style) is easy to add per-case when a flag's footprint is a whole directory; adopt
opportunistically, watch for a pattern, revisit only if one emerges. Explicitly NOT a blocker
for anything else.

## Axis 7 — .NET 11 posture ✅

**Decision (Steve):**

1. **Track latest, always** — STS/LTS distinction is irrelevant; the template deliberately
   forces consumers toward latest ("they don't distrust STS, they're just lazy"). Adopt .NET 11
   on release (Nov 2026), C# latest throughout. Consistent with the ecosystem's standing
   never-backward-pin / migrate-forward rule.
2. **No new runtime-reflection seams, ever** — codified as a design constraint: anything
   discovery-shaped is generated at build time (globs / analyzers / source generators). All
   axis 1–4 machinery already complies; three of five surveyed repos still boot-scan assemblies
   and serve as the counterexample. Citable in reviews.
