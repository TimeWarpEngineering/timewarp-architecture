# Task 135 — Implementation plan (Phase 2 output)

Plan agent output, 2026-07-29. Grounded in direct code reads + empirical probes
(dotnet build -getProperty runs, NoWarn scoping test, gh issue lookup). Four decided
recommendations:

## Recommendations (headline)

1. **Grammar schema:** add `"unroutedLayers": ["tests"]` alongside `"layers"` in
   `feature-filename-grammar.json`. Generator is a Python script
   (`generate-feature-filename-grammar.py`, invoked incrementally via `<Exec>` target in
   the convention-analyzers csproj) — six surgical edits: read/validate/nest over the
   combined layers+unrouted set; `.g.cs` Layers HashSet and `FeatureFilenameLayerSuffixRegex`
   built from combined set (guard ACCEPTS `-tests.cs`); compile_groups loop iterates ONLY
   routed layers (no Compile ItemGroup for tests); omit/blank `Project=` metadata for
   unrouted layers.
2. **TWA0015/16:** register `tests` ONLY as a layer — add NOTHING to `functions`.
   `create-role-tests.cs` = escape-hatch form (clean); `create-role-handler-tests.cs` /
   `-endpoint-tests.cs` automatically trip TWA0015 via existing pairing logic. ZERO analyzer
   code changes. New analyzer test cases (escape clean ×2, handler/endpoint TWA0015 ×2,
   optional guard that no function maps to tests). Split `Should_Keep_Grammar_Registry_In_Sync`
   (currently asserts every layer gets a Compile glob — will fail loudly as a canary if not
   updated first).
3. **Template safety:** `cnd:noEmit` comment-marker escape wrapping ONLY the
   `#if !JARIBU_MULTI` / body / `#endif` lines. Proven precedent in-repo
   (web-spa/program.cs wraps non-template `#if` symbols the same way); enforced by TWA0008;
   zero template.json changes; sourceName rewriting undisturbed. REJECTED: `copyOnly`
   (unused repo-wide; would disable sourceName rewriting the test files need) and a
   template-recognized symbol (flags are architecture-axis-only; TWA0010 would demand
   JARIBU_MULTI in DefineConstants repo-wide).
4. **Analyzer noise:** standardized preamble directive
   `#:property NoWarn=CA1707;CA1849;IDE0161;IDE0021;IDE0058` (empirically verified valid and
   additive with ambient NoWarn). CA1052/CA1515/RCS1102 already ambient in
   `source/container-apps/Directory.Build.props` — drop. TWA0004 NEVER suppressed — real
   Purpose region (spike files already correct). Rejected Directory.Build.props
   project-name-pattern condition (feasible — verified — but reason lives far from file;
   directive is self-contained and greppable).

Also resolved: timewarp-jaribu#19 OPEN/not shipped → keep Lazy-static workaround. Repo-local
`skills/tw-feature-placement/SKILL.md` EXISTS → edit that one; global tw-jaribu skill is
cross-repo → pointer at most (orchestrator: skip editing the external repo in this task;
record as follow-up). `documentation/developer/standards/` has only file-naming.md → extend
it, don't invent testing.md.

## Canonical runfile preamble (the convention to document)

shebang → `#:project`/`#:package` → `#:property PublishAot=false` →
`#:property NoWarn=CA1707;CA1849;IDE0161;IDE0021;IDE0058` → `#region Purpose`
[+ `#region Design`] → `//-:cnd:noEmit` / `#if !JARIBU_MULTI` /
`return await TimeWarp.Jaribu.TestRunner.RunAllTests();` / `#endif` / `//+:cnd:noEmit`

## Ordered steps

1. JSON: add `"unroutedLayers": ["tests"]`.
2. `generate-feature-filename-grammar.py`: the six touch points above.
3. `dev build --clean` (regenerates .g.cs/.g.props AND invalidates cached analyzer DLLs —
   the mandated regen step; `--clean` runs real `dotnet clean`, confirmed in
   build-command.cs).
4. Analyzer tests: split registry-sync assertion (routed get glob, unrouted do NOT); add
   grammar cases per §2.
5. Confirm dev tree clean of spike's Exclude-glob (confirmed — just don't reintroduce).
6. Author + document the preamble convention.
7. Port both spike proof files fresh onto dev in template-safe form (cnd:noEmit switch,
   preamble, Lazy-static host); verify both pass standalone via `dotnet run`.
8. Extend `dev template-smoke` (template-smoke-command.cs + services/template-smoke-harness.cs
   `SmokeOneAsync`): two tiers — (a) cheap: line-diff generated `#if !JARIBU_MULTI`/`#endif`
   presence vs source post-generation; (b) authoritative: `dotnet run` both generated test
   files standalone, assert exit 0 + `Total: N, Passed: N`. Existing solution build is
   structurally blind to M1 (tests files in no project's Compile glob) — new step required.
   workflow template-smoke.yml likely unchanged (single entry point) — confirm.
9. `dev build` 0/0 + `dev template-smoke` end to end.
10. Docs: AGENTS.md (layout + enforcement table note for tests/TWA0015-16);
    `skills/tw-feature-placement/SKILL.md` grammar table (~lines 106, 114-117);
    `documentation/developer/standards/file-naming.md` extension; migration policy stated
    (new tests co-located Jaribu; Fixie migrates opportunistically; tests/ host-level last or
    never; Playwright unchanged; co-located tests standalone-only until task 136 — NO
    aggregator in this task).
11. `ganda repo audit` (+ `--fix`) per tw-pr gate.
12. Kanban checklist/Session current, committed (orchestrator).

## Risks

- Stale analyzer DLLs on registry edit → step 3 `dev build --clean` mandatory.
- Registry-sync test fails loudly if step 4 lags step 3 — expected canary, not regression.
- cnd:noEmit scope: wrap ONLY the directive pair; verify via real `dotnet new` output diff
  (TWA0008 quiet ≠ proof) — template-smoke tier 2 is the real gate.
- Template-smoke cost +~22s per matrix entry — acceptable; note in PR.
- M2 discipline: no aggregator commit here (task 136).
- NoWarn directive drift: future repo-wide analyzers won't auto-apply to runfile preambles —
  one-line doc note so future analyzer authors grep `-tests.cs` preambles.

## Open questions

- Strategic: none blocking. Nod for Results: layer-casing near-miss (`-Tests.cs`/`-test.cs`)
  silent to TWA0015/16 (pre-existing for all layers; MSBuild guard catches at build) —
  recommend accept as-is unless Steve wants friendlier diagnostics later.
- Tactical (implementer's call): `unroutedLayers` key name; whether unrouted layers get an
  (inert) FeatureFilenameGrammarLayer item; AGENTS.md wording.
