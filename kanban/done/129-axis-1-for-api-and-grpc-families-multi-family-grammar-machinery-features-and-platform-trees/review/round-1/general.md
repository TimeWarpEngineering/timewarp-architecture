# Round 1 — General review — task 129

**Date:** 2026-07-28
**Reviewer:** general (empirical, read-only on product code)
**Scope:** commits `001f0ad0` (stage 0 machinery), `2c175b16` (stage 1a analyzer
family-generalization), `4b1e4fa9` (stage 1b api migration), `30bbdd06` (stage 2 grpc
migration), `c2b3f4a2` (`!api` exclude fix) — plus current tree state.

## Summary

Verified against task.md's Checkpoint record (all maintainer rulings), stage-0-design.md
(approved Decisions 1–6), and the review framework. Every ruling was independently re-checked
against the actual committed artifacts, not just the commit messages — including re-deriving
the 6a consumer graph from scratch and running two of my own planted-file guard proofs. I found
**zero issues**. This is a clean, faithfully-executed implementation of an already-approved
design; the asymmetric 6a split in particular is correctly reasoned and empirically verifiable.

Working tree at the end of review: `git status --porcelain` shows only the pre-existing
`.vscode/settings.json` modification noted in the session's starting git status — no residue
from my own verification steps (planted files removed, builds did not touch tracked files).

## Empirical verification performed

1. **Generator (`generate-feature-filename-grammar.py`)** — argparse contract is sound:
   `--family-prefix` required per invocation, `--families`/`--out-cs` co-required and validated
   in `parse_args`, layer-nesting rejection preserved. Read the full script; the family list only
   ever appears in `Families` ImmutableArray emission and prop/project-name string templates —
   the SSOT JSON stays 100% family-agnostic as designed.
2. **csproj (`timewarp-architecture-convention-analyzers.csproj`)** — three explicit `<Exec>`
   lines (Web emits `.g.cs` + `.g.props` with `--families web,api,grpc`; Api and Grpc each emit
   only their own `.g.props`). `Outputs` lists all four generated files so MSBuild's incremental
   check is correct for all of them; `Inputs` is JSON + generator script.
3. **Emitted artifacts** — read `feature-filename-grammar.g.cs` (Families = `["web","api","grpc"]`,
   matches design) and all three `.g.props` files; api/grpc are structurally identical to web's
   except for the family prefix substitution (property names, project-name conditions, comment
   text) — confirmed by diffing after normalizing family tokens.
4. **`Directory.Build.targets` + `feature-membership.targets` (api, grpc)** — diffed against
   web's originals after family-name normalization: identical except one added explanatory
   comment (both trees may be absent pre-migration → no-op glob). Tree roots anchor to
   `$(MSBuildThisFileDirectory)../features` / `../platform` (own-directory anchoring, the 127
   lesson), canonical host conditions correctly reference `api-server` / `grpc-server`.
5. **TWA0015/TWA0016 marker construction** — read the full analyzer. `LayerProjectFeatureMarkers`
   is now built from `Families × Layers` (`/api-server/features/`, `/grpc-application/features/`,
   etc.) plus the literal `web-spa` exception; the absolute-path cohesive-tree check loops over
   `FeatureFilenameGrammar.Families` testing `/{family}/features/` and
   `/container-apps/{family}/features/`. This correctly matches how Roslyn reports absolute
   paths under `source/container-apps/api/features/...` — confirmed by the new analyzer test
   cases and independently reasoned through the string-matching logic.
6. **Analyzer test coverage** — `Given_Api_Family_Layer_Project_Features_Path_IsSilent`,
   `Given_Absolute_Api_Family_Cohesive_Path_Flags_TWA0015`,
   `Given_Api_Family_Relative_Cohesive_Path_IsClean` mirror the existing web-path shapes for the
   api family. Ran the full analyzer test project:
   `dotnet fixie tests/analyzers/timewarp-architecture-analyzers-tests` → **98 passed**.
7. **Drift test (`Should_Keep_Grammar_Registry_In_Sync`)** — genuinely parameterized over an
   explicit `(Prefix, Family)[]` list (web/api/grpc), loops per family asserting layer items,
   function items, hybrid-glob strings, and `FeatureFilenameLayerSuffixRegex` presence in each
   family's `.g.props`, plus membership-targets existence and glob-consumption sanity. Also
   asserts `FeatureFilenameGrammar.Families` (compiled) equals its own family list ordered —
   the previously-"documented" duplication (design Decision 4) is now a checked one, per the
   commit's claim.
8. **Rulings compliance:**
   - `base-error.cs` / `base-exception.cs`: read the pre-deletion content at `4b1e4fa9~1`, grepped
     `BaseError`/`BaseException` at that revision — only the two declaration files themselves
     matched, zero external references or derived types. Deletion is safe; files and all repo
     references are gone at HEAD.
   - `api-application-module.cs`: read pre-deletion content (`ConfigureServices` throws
     `NotImplementedException`), grepped `ApiApplicationModule` at `4b1e4fa9~1` — only the
     declaration matched, confirming the commit's "zero call sites" claim (nothing in
     `program.cs` or elsewhere ever invoked it). Gone at HEAD, zero references.
   - `generic-pipeline-behavior.cs`: `git diff 001f0ad0~1 30bbdd06 -- '**/generic-pipeline-behavior*'`
     is empty — untouched across the whole task range, still at `api-server/` project root
     (bootstrap exemplar, not the cohesive tree), matching the ruling.
   - 6b codegen: `protobuf-generation-hosted-service-server.cs` lives at
     `grpc/platform/codegen/`, namespace `TimeWarp.Architecture.HostedServices` (non-Features,
     matches "platform, not product" convention), `-server.cs` suffix keeps it in
     grpc-server's compile set. Confirmed `program.cs`'s `AddHostedService` line for it is
     still commented out (opt-in, as documented in the file's own Design region).
   - 6c greeter: own slice `grpc/features/greeter/greeter/greeter-service-server.cs`, namespace
     `TimeWarp.Architecture.Features.Greeters` — separate from hello/superhero as ruled.
9. **6a asymmetric split (centerpiece)** — re-derived the consumer graph myself from scratch:
   `grep -rn "ISuperheroService"` / `"IHelloService"` across `source/` and `tests/` shows
   `web-spa/services/superhero-grpc-service-provider.cs` calls
   `CachedGrpcChannel.CreateGrpcService<ISuperheroService>()` and `web-spa/features/superhero/...`
   consumes it — cross-family, client-side consumption. `IHelloService` has no reference outside
   `grpc/features/hello/`. Confirmed `web-spa.csproj` only has a `<ProjectReference>` to
   `grpc-contracts.csproj` (`#if(grpc)` block, line 94) — no reference to `grpc-application` at
   all, so moving `ISuperheroService` there would either break the client or force a new,
   heavier reference. Ran `dotnet build -getItem:Compile` on both projects:
   `i-superhero-service-contracts.cs` appears in `grpc-contracts`'s Compile items,
   `i-hello-service-application.cs` appears in `grpc-application`'s — compilation-unit placement
   is exactly as claimed. Confirmed the global-usings addition
   (`Grpc.Core`, `System.ServiceModel`) in `grpc-application/global-usings.cs` — needed because
   global usings don't flow through `ProjectReference`, and `IHelloService`'s
   `[ServiceContract]`/`[OperationContract]`/`ServerCallContext` need them. The skill's new
   "Proto exception" section states the client-consumed-vs-family-internal rule accurately and
   generalizes it as a check to perform before applying the seam-interface pattern to any gRPC
   service interface — matches what actually happened here.
10. **Empty/near-empty projects** — no stale `features/`/`queries/` folders remain under any
    api/grpc project directory; grep found no stale hand-globs in any api/grpc `.csproj`, no
    stale references in `*.slnx`. Built all four affected projects directly:
    `api-contracts.csproj`, `api-application.csproj`, `grpc-contracts.csproj`,
    `grpc-server.csproj` — all **0 warnings / 0 errors**.
11. **Proto exemption** — `grpc-server/protos/greet.proto` untouched
    (`git diff 001f0ad0~1 30bbdd06 -- '**/*.proto'` empty); the old
    `grpc-server/services/greeter-service.cs` was deleted and replaced by the new slice file
    (not a copy — namespace changed, `HelloRequest` fully-qualified to disambiguate from the
    code-first `Hellos.HelloRequest`, documented in its own Design region). Skill's "Proto
    exception" section documents the exemption and the implementation-vs-artifact distinction
    correctly.
12. **`!api` fix (`c2b3f4a2`)** — added exclude path
    `tests/container-apps/web/web-spa-integration-tests/Serialization/ApiService_Body_Casing_Tests.cs`
    is correct (confirmed the file exists at that path and references `ApiServerApiService`
    directly, unguarded by any `#if`). Checked every other repo-wide reference to
    `ApiServerApiService`: the other five are either inside `tests/container-apps/api/**`
    (already excluded wholesale under `!api`) or guarded by `#if(api)`/`#if api` blocks in
    `AspireSpaTestApplication.cs` and `program.cs` — no other orphan reference exists. The
    task.md note's distinction between this flag-bug fix and the separate CS7036 pins-lag issue
    is consistent with what's in the repo (the pins-lag issue is a known, tracked, unrelated
    condition — not part of this diff).
13. **Docs** — read both `AGENTS.md` diffs (`001f0ad0`, `30bbdd06`) and both `SKILL.md` diffs
    (`001f0ad0`, `30bbdd06`) in full. Present tense throughout, no task-number narration inside
    the skill body (task 129 is cited only as an inline attribution for specific rulings, which
    matches the skill's existing convention of citing rulings by number elsewhere, e.g. TWA0009).
    Cross-checked the current `AGENTS.md` tree diagram against the actual final tree
    (`api/features/weather-forecast/`, `grpc/features/hello,superhero,greeter/`,
    `grpc/platform/codegen/`) — accurate, no drift.
14. **My own empirical planted-file guard proofs** (two, as required, in addition to the
    orchestrator's own prior proofs):
    - Planted `source/container-apps/api/features/weather-forecast/get-weather-forecasts/rogue-file-no-suffix.cs`
      and built `api-server.csproj` → build failed with the expected membership-guard error
      naming the api tree and registered layers. Reverted; `git status` clean.
    - Planted `source/container-apps/grpc/platform/codegen/rogue-file-no-suffix.cs` and built
      `grpc-server.csproj` → same guard fired correctly for the platform tree. Reverted;
      `git status` clean.
    - Also built `tests/container-apps/api/api-server-integration-tests` (references the
      migrated `get-weather-forecasts` contract/handler) → 0 warnings / 0 errors.

## Issues

None found. Zero issues at bug, suggestion, and nit severity.

## Notes for the record

- `tests/container-apps/grpc/` does not exist as a directory — confirmed this predates task 129
  (no grpc-specific integration test project has ever existed in this repo); not a gap
  introduced by this change, out of scope for this task's Definition of Done.
- The `!api`/`!grpc` template.json exclude blocks use wildcard globs
  (`source/container-apps/api/**`, `source/container-apps/grpc/**`), so they transparently cover
  the newly-created `msbuild/`, `Directory.Build.targets`, `features/`, and `platform/` trees
  without needing any additional lines — good foresight, nothing to flag.
- The asymmetry in `FeatureTreeFile Include` (the `features/` glob has no `Exists()` guard while
  the `platform/` glob does) is inherited verbatim from web's pre-129 original — not a regression
  introduced here.
