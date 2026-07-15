# Analyzer: csproj DefineConstants must agree with template.json feature-flag symbols

Realizes [[prefer-analyzers-sourcegen-over-inference]] for the surviving template flags
(api/grpc/web/yarp/postgres).

## Why

`#if(flag)` regions in template content are kept in the real repo build only because each csproj
hand-lists the flags in DefineConstants (web-spa, web-server, aspire-app-host, timewarp-testing,
web-spa-integration-tests...). Nothing checks that list against `.template.config/template.json`.
Miss one and the real build silently compiles WITHOUT a region the template would emit —
agreement-by-memory, the exact failure mode this repo eliminates with analyzers. Found during 071
when web-spa-integration-tests needed constants added by hand.

## Rule sketch

For every project whose sources contain `#if(<flag>)` where `<flag>` is a template.json symbol,
the project's DefineConstants must include that flag (build-time check; MSBuild target or analyzer
+ additional-files carrying template.json). Also flag the reverse: constants naming template
symbols that no source in the project uses (stale).


## Implementation Plan (2026-07-14)

TWA0010 in timewarp-architecture-convention-analyzers. Check ONE direction only: a `#if`/`#elif`
condition naming a template.json bool symbol that is NOT in the project's preprocessor symbols =
error at the directive (the region silently vanishes from the real build — the dangerous drift).
Stale direction (constant defined, no directive) is deliberately out of scope: razor `@*...*@` and
csproj `<!--...-->` conditional forms are invisible to the C# compilation (web-spa's `web` would
false-positive).

1. Wire `.template.config/template.json` as AdditionalFiles in source/ and tests/
   Directory.Build.props (Exists-conditioned; generated apps have neither the file nor surviving
   flag directives → analyzer silent there).
2. CompilationStart: parse bool symbols from template.json (System.Text.Json; net10 analyzer);
   per-tree action collects identifiers from directive conditions and compares against
   ((CSharpParseOptions)tree.Options).PreprocessorSymbolNames.
3. Known violation to fix alongside: web-infrastructure's `#if(postgres)` guarded global using has
   zero consumers in the project (verified in 071) — DELETE it rather than define the constant.
4. Tests: fake template.json via TestState.AdditionalFiles + SolutionTransforms for preprocessor
   symbols. Directive syntax in embedded sources is COMPOSED at runtime (087 lesson: the engine
   processes real directives in template content — including this test file).

## Checklist

- [x] Choose mechanism (analyzer with AdditionalFiles=template.json vs MSBuild target)
- [x] Implement; next free TWA id if analyzer
- [x] Tests both directions (missing constant; stale constant)
- [x] dev build 0/0

## Session

- Created: 2026-07-11 (spun out of 071)

## Results (2026-07-14)

**Implemented** (commit 43c70ca6): `TemplateFlagConstantsAnalyzer` (TWA0010).

- Reads template.json bool symbols via AdditionalFiles (Exists-conditioned wiring in source/ and
  tests/ Directory.Build.props); flags any `#if`/`#elif` condition naming a template flag absent
  from the project's preprocessor symbols, at the identifier's exact span.
- One direction only, per plan: the stale reverse would false-positive (razor/csproj conditional
  forms are invisible to the C# compilation — web-spa's `web`). Generated apps are naturally
  silent (no template.json, no surviving flag directives).
- **The known violation fixed**: web-infrastructure's `#if(postgres)` guarded global using had
  zero consumers — deleted (NoPostgres generated app verified 0 errors after removal).
- Test directive syntax composed at runtime with made-up flag names (087 lesson); all new files
  verified shipping intact through generation.
- AGENTS.md: enforcement rows for 0009/0010 added; stale counter/eventstream flag list
  reconciled to post-071 reality.

**Tests**: 6 green (missing-constant flags; defined, compound-partial, non-template symbol,
non-bool symbol, no-template.json clean/silent). Live-fire: replanting the web-infrastructure
violation fails the build with TWA0010 at the exact span. `dev build` 0/0.
