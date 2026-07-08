# Template generation mangles any .cs file with an `#if`-family token in a comment (blocks 071)

Blocker spun out of [[071-decouple-template-feature-flags-apiwebyarpcountereventstreampostgres]].

## Root cause

The dotnet template engine runs C# conditional-processing on every generated `.cs` file. It treats
the literal tokens `#if` / `#elif` / `#else` / `#endif` as preprocessor directives **even when they
appear inside `//` comments or string literals**. When it encounters a bogus `#if` in a comment it
strips from there to the next `#endif` — and if there is none, to end-of-file. That truncates the
file mid-member, leaving unbalanced braces, so the generated app fails to compile with **CS1513
"} expected"**.

The original title blamed a "syntax error in purpose-region-analyzer.cs" — that was a misdiagnosis.
The analyzer source compiles clean (0/0); the file is only *mangled at generation time*, and it was
one of three files carrying the same landmine, not a lone offender.

## Impact

Generation breaks regardless of which feature flag is toggled, so the 071 per-flag verification loop
(`dotnet new … --X false` → build) cannot even start. Hard block on 071.

Introduced 2026-07-02 by the region-backfill work (tasks 084 and 050-010), which is *after* the 071
matrix was last verified 2026-06-26 — which is why a loop that used to run now fails immediately.

## Fix (done)

Reworded the `#if`-family tokens out of the comments in the three affected template files (the
literal string `#if ` must never appear in template `.cs` comments/strings):

- `source/analyzers/timewarp-architecture-convention-analyzers/purpose-region-analyzer.cs` (`#if false`)
- `source/container-apps/web/web-spa/features/developer/components/user-claims-base.cs` (`#if false`)
- `source/container-apps/aspire/aspire-app-host/program.cs` (`#if blocks …`)

Verified by regenerating `--postgres false` and confirming all three come through intact (braces
balance; only legitimate template directive/`cnd:noEmit`-marker stripping remains). Edits are
comment-only, so repo compilation is unchanged (analyzers build 0/0).

## Checklist

- [x] Identify root cause (template conditional-processor misreads `#if` in comments)
- [x] Reword the three affected comments
- [x] Regenerate and confirm the three files come through intact
- [x] Confirm repo build stays 0/0 (comment-only edits)

## Follow-up (separate task, prefer-analyzers directive)

Add a guard so this class of bug is caught before generation instead of only when someone runs the
loop: a build-time or template-time check that fails when template content contains an `#if` / `#elif`
/ `#else` / `#endif` token inside a comment or string literal. The scan
`grep -rn --include='*.cs' -E '//.*#(if|elif|else|endif)|"[^"]*#(if|elif|else|endif)' source/ tests/`
is the crude form; a Roslyn check keyed off comment/string trivia is the durable one.

The check must NOT be absolute — such tokens are legitimate (analyzer/generator tests embedding C#
source in string literals, a generator emitting preprocessor directives, comments explaining
preprocessor behavior). The rule is "reword it, or wrap it in the template engine's
`//-:cnd:noEmit` … `//+:cnd:noEmit` escape"; the guard should flag only occurrences that are neither
real feature-flag directives nor inside a `cnd:noEmit` region, and its message should name both
remedies. See [[prefer-analyzers-sourcegen-over-inference]].

## Session

- Created: 2026-07-08 (blocker for 071)
