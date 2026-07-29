# Adopt co-located Jaribu test convention

## Description

Land the co-located Jaribu test convention on dev for real, using the evidence and blockers
from spike task 134 (`kanban/done/134-spike-jaribu-co-located-integration-tests/findings.md` —
read it first; the spike branch `spike/134-jaribu-co-located-integration-tests` holds worked
proof files to port, not merge).

**Decided (Steve, 2026-07-29):** the membership-guard carve-out is a **registered-unrouted
`tests` suffix** in `feature-filename-grammar.json` — NOT the spike's exclude-glob. Test files
under `features/`/`platform/` stay matched-and-validated by `ValidateFeatureFileMembership`
(so orphaned/misnamed files still get the teaching error) but are routed into no layer
project. Grammar remains the single vocabulary for the trees.

Testing philosophy constraints carry over from 134: integration-over-unit, "don't mock your
friends" (mock only externalities via the timewarp-testing `configureServicesDelegate` hook),
framework docs stay in the `tw-jaribu` skill.

## Requirements

1. **Grammar registration:** add `tests` to
   `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`
   as a recognized suffix with NO layer-project routing (no Compile glob emitted for it in any
   family's `feature-filename-grammar.g.props`). Applies to all grammar families (web, api,
   grpc). Define TWA0015/TWA0016 semantics for it (a `-tests.cs` file is a valid archetype;
   function-segment pairing rules must not false-positive on it). Registry edit ⇒ full
   rebuild (AGENTS.md). Remove the spike's `Exclude="**/*-tests.cs"` approach — the guard
   must ACCEPT `-tests.cs` via the grammar, not skip it.
2. **Template safety (spike blocker M1):** the `#if !JARIBU_MULTI` switch in co-located
   runfiles must survive `dotnet new` generation. Candidates (implementer picks, proves via
   regression gate): `cnd:noEmit` comment-marker escape around the directive pair;
   template.json exclusion of `-tests.cs` from conditional processing; or a
   template-recognized symbol. Extend **`dev template-smoke`** to assert generated apps'
   co-located tests still compile and run (this is the permanent regression gate; TWA0008/0010
   family hazard: non-template `#if` symbols in template content).
3. **Runfile preamble convention:** standardize and document the co-located runfile header:
   shebang → `#:project`/`#:package`/`#:property PublishAot=false` (spike evidence: .NET 10
   file-based apps default `PublishAot=true`, breaking `ContractSerializationDefaults`
   reflection JSON) → `#region Purpose` → JARIBU_MULTI switch. Decide the analyzer-noise
   story for `-tests.cs` under `source/`: per-file pragmas (spike form) vs a scoped NoWarn
   for the suffix — prefer whatever keeps production files in the same folders fully analyzed.
4. **Port the spike proof files onto dev** as the first real co-located tests (template-safe
   form, per this task's conventions):
   `source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs` and
   `source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs`.
   Both must pass standalone. Keep the timewarp-testing manual-instantiation pattern; adopt
   class-scoped lifetime from timewarp-jaribu#19 if it has shipped by then (check), else keep
   the documented Lazy-static workaround.
5. **Aggregator sequencing (spike blocker M2):** do NOT commit a JARIBU_MULTI aggregator
   under `tests/` while `dev test` still uses the `dotnet test <csproj-path>` invocation —
   it breaks `dev test` on .NET 10 MTP projects. The aggregator + `dev test` MTP support land
   together in the follow-up `dev test` task (create it when its discovery-shape decision is
   made — 134 findings §8 Q2). Until then co-located tests are standalone-run only; note this
   explicitly in the docs delivered by requirement 6.
6. **Documentation:** update AGENTS.md (layout + enforcement sections), the
   `tw-feature-placement` skill (grammar table), `documentation/developer/standards/`
   file-naming/testing pages as applicable, and the `tw-jaribu` skill if the repo-specific
   preamble belongs there. State the migration policy: new tests are co-located Jaribu from
   adoption; existing Fixie projects migrate opportunistically slice-by-slice; `tests/`
   host-level integration suites migrate last or never; Playwright e2e unchanged.
7. **Gates:** `dev build` 0/0; `dev template-smoke` green (including the new regression
   coverage); both runfiles pass standalone; `ganda repo audit` clean.

Out of scope: `dev test` changes and the aggregator commit (own task after findings §8 Q2);
Aspire multi-resource tier (findings §8 Q3); migrating any existing Fixie suite.

## Checklist

- [ ] `tests` registered in grammar SSOT as recognized-unrouted; g.props regenerated; spike
      exclude-glob approach not used
- [ ] TWA0015/0016 semantics for `-tests.cs` defined + analyzer tests
- [ ] Template-safety mechanism chosen, implemented, proven
- [ ] `dev template-smoke` regression coverage for co-located tests in generated apps
- [ ] Runfile preamble convention decided (incl. PublishAot=false + analyzer-noise story) and applied
- [ ] Both spike proof files ported onto dev, passing standalone
- [ ] timewarp-jaribu#19 status checked; lifetime pattern per outcome
- [ ] AGENTS.md + skills + standards docs updated, migration policy stated
- [ ] Gates: `dev build` 0/0, template-smoke green, `ganda repo audit` clean
- [ ] Kanban mutations committed

## Notes

- Origin: task 134 findings §§4–5, 8–9. Strategic decision Q1 resolved by Steve (registered-
  unrouted suffix); Q2 (`dev test` discovery shape) and Q3 (Aspire tier) remain open and are
  deliberately fenced out of this task.
- Spike branch is reference material; port files fresh (template-safe), don't merge it.

## Session

- Created: c6f1a13b-487f-4085-bf61-ba4761e8579e (2026-07-29)
