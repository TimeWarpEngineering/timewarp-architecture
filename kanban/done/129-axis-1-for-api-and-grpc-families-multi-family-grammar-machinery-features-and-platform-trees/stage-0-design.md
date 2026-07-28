# 129 Stage 0 — Multi-family grammar machinery: design proposal

Produced 2026-07-28 by a read-only design agent; maintainer checkpoint gates implementation.
(Verbatim deliverable; decisions numbered for the checkpoint.)

## Part A — How the machinery actually works today (evidence)

**Pipeline:** `feature-filename-grammar.json` (pure {layers, functions}, zero family identifier)
→ convention-analyzers csproj target `GenerateFeatureFilenameGrammar` runs
`generate-feature-filename-grammar.py <json> <out-cs> <out-props>`; the props DESTINATION path
is hardcoded in the csproj (web/msbuild/...). Inside the script, "web" appears only in output
f-string templates (Project="web-{layer}" conditions, literal $(WebFeatureTreeRoot)/
$(WebPlatformTreeRoot) names) — the emitted .g.cs constants are already family-agnostic.

`feature-membership.targets` anchors tree roots to its OWN directory (127 lesson already
applied) and is imported via web/Directory.Build.targets. Guard runs once, gated to web-server
as canonical host. **api/ and grpc/ have NO Directory.Build.props/targets or msbuild/ folder —
MSBuild walk-up cannot share web's; each family needs its own mirror.**

**SSOT drift test** (Should_Keep_Grammar_Registry_In_Sync): static JSON↔.g.cs↔.g.props↔targets
comparison, entirely hardcoded to web — needs real per-family parameterization, not just added
assertions.

**TWA0009 — key correction to the task's framing: already universal, zero changes needed.**
SliceRoot = {RootNamespace}.Features (no path/family logic anywhere; wired repo-wide via
source/Directory.Build.props). api/grpc demo code ALREADY uses
TimeWarp.Architecture.Features.* namespaces and is governed today. Per-assembly scoping filters
cross-family references, so the shared root namespace poses no isolation risk (slice-id
collisions across families would be a readability nit only).

**api/grpc wiring today:** SDK-default globs only; api = weather-forecast demo + placeholder
module/base-error/base-exception/generic-pipeline-behavior; grpc = code-first (hello, superhero)
+ proto-first (greet.proto → generated GreeterBase) + protobuf-generation hosted service;
template.json !api/!grpc exclude whole family trees; slnx uses #if comment-marker convention.

## Part B — Decisions

1. **Emission shape (lean: per-family g.props)** — api/msbuild/ + grpc/msbuild/ generated from
   the SAME unmodified JSON, script parameterized by family prefix; explicit per-family <Exec>
   invocations in the analyzers csproj (three legible lines over batching cleverness). Shared
   single props file REJECTED: $(MSBuildThisFileDirectory) can only self-anchor one location
   (127 lesson), and cross-family imports into web/msbuild/ are worse coupling. yarp excluded
   (single-project family).
2. **Registry: zero changes** — grammar is universal; families differ only in tree roots.
   Escape hatch covers non-mediator shapes; no forced function mappings.
3. **Guard/targets: per-family mirrors** — each family gets Directory.Build.targets +
   msbuild/feature-membership.targets structurally identical to web's, own tree roots anchored
   to own directory, own canonical host (api-server / grpc-server). Define platform roots now
   (absent-tree guards make them no-ops until 118 needs them).
4. **Drift test: parameterize over an explicit family list** — [(Web,web),(Api,api),(Grpc,grpc)]
   in the test; accepted DOCUMENTED duplication with the csproj's family list (comment both
   sides; promote to a real registry only if a fourth family appears).
5. **TWA0009: reframe from "extend" to "document as already-universal"** — skill + AGENTS.md
   note; no code change.
6. **grpc layer mapping — genuinely open, STAGE 2 maintainer calls (not stage-0 blockers):**
   (a) service interfaces (i-hello-service, i-superhero-service): `-contracts.cs` vs the
   seam-interface pattern (`-application.cs` beside their `-server.cs` impls — the skill's own
   identity-host precedent argues for application); (b) protobuf-generation-hosted-service:
   bootstrap vs platform/codegen cluster; (c) greeter slice naming (own slice vs hello-adjacent).
   DTOs → `-contracts.cs` verified safe (TWA0005/0006 gate on [ApiRoute] presence, not suffix);
   .proto files + generated code stay OUT of grammar scope (documented exemption).

## Stage 1 preview (api) — judgment calls flagged

get-weather-forecasts contract+handler → api/features/weather-forecast/get-weather-forecasts/
(grammar names, straightforward). base-error/base-exception: platform-ish vs product-adjacent —
no derived types exist, surface to maintainer. generic-pipeline-behavior: bootstrap (lean) vs
platform concern — borderline, surface. api-application-module: placeholder with no concern to
anchor to yet; follows the base-error/pipeline calls.

## Stage 2 preview (grpc) — judgment calls flagged

hello/superhero DTOs + impls straightforward (contracts/server suffixes, use-case folders);
service interfaces + codegen hosted-service + greeter naming per Decision 6.
