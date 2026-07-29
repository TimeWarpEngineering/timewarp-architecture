# Review brief — task 131 full-repo code review

- **Review commit:** `2b5dc765` (code-identical to HEAD `6025b2c6`; only kanban files changed since)
- **Date:** 2026-07-28
- **Reviewer:** Kimi K3 (orchestrator session, opencode) — all judgment/findings rendered in-session
- **Method:** TimeWarp `/code-review` skill baseline (strict maintainability, code-judo lens) adapted to whole-repo scope per task 131 brief
- **Scope:** `source/`, `tests/`, `tools/`, `.template.config/`, root MSBuild (`Directory.Build.*`, `Directory.Packages.props`, `msbuild/`)
- **Exclusions:** `obj/`/`bin/`/`artifacts/`, generated `*.g.cs` as review targets, full `dev build`/`dev test` battery (CI owns green), program-104 future surface, settled architecture (109/114/126/127/129) except visible drift

## How the tree was walked

**Phase A — 6 parallel mechanical scans (explore sub-agents, read-only):**
- A1 file-size inventory: ~23.7k LOC source, ~15.9k tests, ~3.1k tools; largest files template-smoke-command.cs (770), template-publish-smoke-command.cs (659), typed-id-source-generator.cs (554), feature-filename-grammar-analyzer.cs (476), web-server program.cs (368). No file ≥1000 lines.
- A2 convention grep pack: MediatR (1 razor marketing link), FluentAssertions (0), auth-marker pairing (all 24 hosted contracts exactly one marker; 0 contradictions), CrossSliceReference (4 production, all reasoned), ClientOnlyContract (4, all reasoned), Purpose regions (0 missing), template flags + dual-mode symbols (98 dual-mode sites, 0 literal sourceName-rewrite hazards in live MSBuild).
- A3 placement/grammar scan: 100% grammar conformance (79/79 web, 2/2 api, 10/10 grpc, 17/17 platform); 2 bare-`Features`-namespace shared-constant files; api-server has 1 logic file in artifact folder; web-spa conventional (86 .cs + 40 .razor).
- A4 contract census: 23 hosted + 4 client-only contracts, all with validators; 2 hand-written MapGet endpoints (web-server `/service-discovery`, grpc `/`); 0 hand-written FastEndpoints/Controllers in server projects; validation exclusively on mediator FluentValidationBehavior.
- A5 duplication heat map: identity-handler problem-factory cluster (high), BaseApiService↔TestApiService mirror (high, ~43 shared lines), test ceremony duplication (medium), appsettings/GlobalUsings overlap (medium).
- A6 kanban dedup index: 32 open + done ≥100 indexed by theme; 126-review confirmed closed clean; packaging cluster (115/124/126-003/004/006/130) fully closed.

**Phase B — deep reads by reviewer (Kimi K3, this session):**
- W1: root/tests/source Directory.Build.props, timewarp-platform-packages.props, .template.config/template.json, template-smoke-command.cs (770), template-publish-smoke-command.cs (659), App.razor, web-spa.csproj DefineConstants
- W2: fast-endpoint-source-generator.cs, endpoint-metadata.cs, ingress-route-prefix-generator.cs, typed-id-source-generator.cs, feature-filename-grammar-analyzer.cs, route-registry.cs, api-endpoint-attribute.cs, endpoint-coverage-analyzer.cs (TWA0005 jurisdiction)
- W3: base-endpoint.cs, base-fast-endpoint.cs, common-server-module.cs, contract-serialization-defaults.cs
- W4: in-memory-principal-store.cs, ef-principal-store-infrastructure.cs (first 120 lines — pattern confirmed via contract tests)
- W5: web-server program.cs, web-spa program.cs, base-api-service.cs, add-passkey-handler-application.cs
- W6: aspire-app-host program.cs, yarp program.cs
- W7: via A5 pattern data + spot checks (generic-pipeline-behavior.cs, HomePage.razor, v2/overview.md)

**Honest depth statement:** every area in the coverage table was walked; deep reads concentrated on generators, host Programs, packaging/dual-mode, and identity seams per the plan's ship-risk-first order. api/grpc feature slices (3 small demo slices) were covered structurally (A3/A4) not line-by-line. Test bodies were pattern-audited, not exhaustively read. `source/foundation` leaf types (base/ types, cors policies) were sampled, not all opened.

## Verification of scan health

- 0 product contracts missing auth markers (TWA0013 clean)
- 0 both-markers / anonymous+IAuthApiRequest contradictions (TWA0014 clean)
- 0 grammar violations (TWA0015/0016 clean)
- 0 missing `#region Purpose` in source/ (TWA0004 clean)
- 0 literal `TimeWarp.Architecture.{Analyzers,Generators,Attributes}` in live MSBuild (115/126-004 holding)
- 0 hand-written endpoint shims (109 holding)

## Findings summary

17 findings: **1 blocker, 6 major, 8 minor, 2 note** — see `findings.md`. Themes: platform-package hygiene (F-001/002), generator engineering (F-003/004/005/008/014), template-exemplar duplication (F-006/015/016/017), gate/tooling duplication (F-007/011/012), convention-by-comment residuals (F-009/010/013/016).

Disposition: pending steward — `disposition.md` to be written after human review.
