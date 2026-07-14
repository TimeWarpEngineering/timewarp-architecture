# Guard against `#if`-family tokens in template .cs comments/strings (allow cnd:noEmit escape)

Follow-up to [[086-fix-syntax-error-cs1513-in-purpose-region-analyzercs-blocking-all-feature-flag-verification]].
Realizes the [[prefer-analyzers-sourcegen-over-inference]] directive for this bug class.

## Problem

Because this repo IS the `dotnet new timewarp-architecture` template, the template engine runs C#
conditional-processing on every generated `.cs` file and treats `#if` / `#elif` / `#else` / `#endif`
as directives **even inside `//` comments and string literals**. An unguarded token strips from
itself to the next `#endif` (or EOF), truncating the generated file → CS1513 in every generated app,
regardless of feature flag. 086 was three such comments; nothing catches the next one until someone
runs the (expensive) generate-and-build loop.

## Goal

Catch this at build time in THIS repo, before generation.

## Not an absolute ban

These tokens are legitimate in normal code — analyzer/generator tests embedding C# source in string
literals, a generator emitting preprocessor directives, comments explaining preprocessor behavior.
The template engine's sanctioned escape is the `//-:cnd:noEmit` … `//+:cnd:noEmit` guard pair (see
`user-claims-base.cs`). So the rule is: **reword one-liners, or wrap real content in `cnd:noEmit`.**

The guard must flag only occurrences that are BOTH:
- not a real feature-flag preprocessor directive (i.e. the token is inside comment or string trivia), and
- not inside a `//-:cnd:noEmit` … `//+:cnd:noEmit` region.

Its diagnostic message must name both remedies.

## Approach

- Crude form (a pre-commit / build script): `grep -rn --include='*.cs' -E
  '//.*#(if|elif|else|endif)|"[^"]*#(if|elif|else|endif)' source/ tests/` — but this can't see
  `cnd:noEmit` regions and will false-positive once a legit case appears.
- Durable form: a Roslyn analyzer (in `timewarp-architecture-convention-analyzers`) keyed off comment
  and string-literal trivia, that also tracks `cnd:noEmit` region markers to suppress guarded spans.
  Assign the next free TWPA id.
- Scope: only files that ship as template content (all of `source/`, `tests/` except the
  per-flag/foundation excludes in `.template.config/template.json`). Decide whether to scope the
  analyzer or accept repo-wide (repo-wide is simpler and the tokens are rare).


## Implementation Plan (2026-07-14)

Decision: Roslyn analyzer (TWPA0008) in timewarp-architecture-convention-analyzers.

1. `template-conditional-token-analyzer.cs`: syntax-tree action scanning comment trivia
   (single/multi-line + doc) and every string-literal token kind (regular, verbatim, raw,
   interpolated text, UTF-8) for `#(if|elif|else|endif)\b`. Line ranges between `//-:cnd:noEmit`
   and `//+:cnd:noEmit` are exempt. Real preprocessor directives are directive trivia — never
   scanned. Diagnostic anchors on the exact match span; message names both remedies.
2. Self-hosting: the analyzer source and its tests are themselves template content — the raw byte
   sequence must not appear in them (the engine matches it even mid-line, per 086). Token text in
   the analyzer and embedded test sources is composed at runtime ("#" + "if").
3. Wire convention analyzers into tests/Directory.Build.props (template content includes tests/;
   analyzer-test files embedding C# in raw strings are the likeliest future carriers) with
   `NoWarn TWPA0004` for tests — Purpose-region adoption in tests is a separate decision.
4. Register TWPA0008 in AnalyzerReleases.Unshipped.md; add AGENTS.md enforcement-table row.
5. Tests: comment hit, string hit, raw-string hit, doc-comment hit, real-directive clean,
   cnd:noEmit-guarded clean.
6. Verify: dev build 0/0 (sweep confirmed repo currently clean), analyzer tests green, default
   template generation sanity check.

## Checklist

- [ ] Decide crude-script vs Roslyn analyzer (prefer analyzer per the standing directive)
- [ ] Implement so real directives and `cnd:noEmit`-guarded spans pass; unguarded comment/string tokens fail
- [ ] Message names both remedies (reword / wrap in `cnd:noEmit`)
- [ ] Add tests (positive: bare `#if` in comment and in string flag; negative: real directive, guarded span)
- [ ] Register the TWPA id in AnalyzerReleases.Unshipped.md; `dev build` stays 0/0

## Session

- Created: 2026-07-10 (follow-up to 086)
