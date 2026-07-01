# Roslyn analyzer: flag validator presence-rule vs declared-nullability contradictions in contracts

## Description

Turn RFC Decision 7 from a convention people must remember into a **compile-time analyzer**. The
rule is **not** "properties must be non-nullable" — `string?` is perfectly legitimate for a genuinely
**optional** field. The defect is a **contradiction**: a property carries a FluentValidation
**presence rule** (`NotEmpty()` / `NotNull()`) while its declared type says the value may be absent
(`string?`) or is masked by a fake-valid default (`= string.Empty`).

This must be the **semantic cross-check** — walking the validator is required; you cannot judge
nullability in isolation. A blanket "flag all `string?`" would be wrong.

**Proven premise (task 078):** `IRoleDetails.Name`/`.Description` are non-nullable `string`, the
`CreateRole.Command` implements them with `= null!`, and the form bound + `NotEmpty()` fired — so
binding never required `string?`. Copic's `string?` on presence-validated fields was an oversight.
See [[contract-conventions-rfc]] and [[077-contracts-compliance-01-nullability-validator-agreement]].

## The rule (diagnostic conditions)
For a property `P` on type `T` where an `AbstractValidator<T>` (directly or via a shared
`AbstractValidator<IDetails>` set with `SetValidator`) declares `RuleFor(x => x.P).NotEmpty()` or
`.NotNull()`:
- **Contradiction A** — `P` is declared **nullable** (`string?` / `NullableAnnotation.Annotated`).
  → the presence rule and the type disagree; make `P` non-nullable (or drop the presence rule if
  optional was intended).
- **Contradiction B** — `P` is non-nullable but initialized with **`= string.Empty` / `= ""`**
  (a fabricated valid-looking default that hides an unset field). → use `= null!` or `required`.

Conversely: `string?` with **no** presence rule is **fine** — do not flag it.

## Feasibility / approach
- Host in the existing analyzer/source-gen project (same one that ships the FastEndpoint generator +
  `contracts-mixin-generator.cs`); test via the `Tests/Analyzers` Fixie harness.
- `DiagnosticAnalyzer` registered on `AbstractValidator<T>` subclasses:
  1. Resolve `T` (the validated type).
  2. Walk each `RuleFor(x => x.P)...` fluent chain; collect the terminal method names
     (`NotEmpty`/`NotNull`) applied to each mapped property symbol. Handle chained calls and
     `SetValidator(new SharedDetailsValidator())` composition (follow into the referenced validator
     over the shared interface `T` implements).
  3. For each property with a presence rule, read `IPropertySymbol.NullableAnnotation` and any
     initializer syntax; emit Contradiction A / B.
- Known hard edges to cover in tests: lambda bodies that aren't simple member access
  (`x => x.P.Trim()`), `RuleFor` on the whole object (`RuleFor(x => x)`), rules split across
  multiple statements, and shared-validator composition (`SetValidator`).

## Checklist
- [x] Analyzer project wiring — **NEW analyzer-only assembly** `timewarp-architecture-contract-analyzers`
      (see Results for why not the existing assembly).
- [x] Rule engine: map `RuleFor(...).NotEmpty()/.NotNull()` -> property symbols on `T` (direct detection).
- [ ] Follow `SetValidator(new XDetailsValidator())` into shared `AbstractValidator<IDetails>` —
      **deferred** (not needed for the 077 targets; shared `AbstractValidator<IDetails>` are analyzed
      *directly* against the interface, and no current concrete command masks a composed rule). Fast-follow.
- [x] Diagnostic A (TWPA0002 nullable + presence rule) and B (TWPA0003 `= string.Empty`/`= ""` + presence rule).
- [x] Fixie analyzer tests incl. hard edges (non-trivial lambda, whole-object `RuleFor(x => x)`) + negatives
      (`string?` w/o rule, compliant `= null!`). 8 new tests; all 16 analyzer tests green.
- [x] Run across TWA contracts (web-contracts): surfaced **exactly the 4 expected** violations, no extras.
- [x] **Cleared every reported violation in this PR** (hello `Name`, track-event `EventName`,
      create/update-todo-item `Title`). `dev build` green (0/0). See Results.
- [ ] (Optional) code fix provider: nullable->non-nullable and `= string.Empty` -> `= null!` — deferred.

## Results
- **Delivery: new analyzer-only assembly** `source/analyzers/timewarp-architecture-contract-analyzers/`
  (added to `.slnx`). *Why not the existing `timewarp-architecture-analyzers`:* that assembly also
  contains the FastEndpoint source generator, which triggers on `[RouteMixin]` — abundant in
  web-contracts — and would emit endpoint classes into a project that can't compile them. An
  analyzer-only assembly is safe to reference from contracts (verified: no generator fired).
- **Diagnostics:** `TWPA0002` (nullable + presence rule), `TWPA0003` (empty-string default + presence
  rule). Registered in the new project's `AnalyzerReleases.Unshipped.md`; removed from the old one.
- **Detection is direct** — `RuleFor(x => x.Prop)...NotEmpty()/NotNull()` inside `AbstractValidator<T>`,
  checked against `T.Prop`. Whole-object rules and non-trivial lambda bodies are conservatively skipped;
  `string?` **without** a presence rule is never flagged (legitimate optional field).
- **Wired into web-contracts only** (this pass = 077 scope). First build reported precisely:
  `hello.cs:8 Name` (A), `track-event.cs:9 EventName` (A), `create-todo-item.cs:11 Title` (B),
  `update-todo-item.cs:10 Title` (B) — an exact match to 077's table. Fixed each to non-nullable `= null!`
  (validators + `init`/`set` untouched). Full `dev build` green.
- **Note fields intentionally left** `= string.Empty` (create/update-todo-item `Note`): no presence rule,
  so not a contradiction this rule targets. 077 also listed `Note`->`string?` as a style cleanup; that is
  *not* an analyzer violation and is out of this rule's scope (a mismatch we investigated and accepted).
- **Fast-follows:** wire the analyzer into `api-contracts`/`grpc-contracts`/`foundation-contracts` and
  fix whatever surfaces; optional `SetValidator` composition-following; optional code-fix provider.

## Sequencing — absorbs 077 (chosen: option 1)
Do this **before** [[077-contracts-compliance-01-nullability-validator-agreement]] and fold 077's
fixes into this PR. At Error severity under warnings-as-errors, shipping the analyzer with existing
violations present would break `master`; adding the analyzer *and* fixing everything it reports in one
change keeps the tree green and makes the rule a guard from commit one. 077's file/property table is
the **expected** worklist — cross-check the analyzer's actual output against it (a mismatch either way
is a signal to investigate before mass-fixing).

## Notes
- Enforcement mechanism for [[077-contracts-compliance-01-nullability-validator-agreement]] — 077 is
  the manual cleanup; 080 prevents regressions.
- Consider whether an off-the-shelf FluentValidation analyzer covers any of this before building, but
  the type<->validator *nullability* correlation is bespoke to our convention, so expect to own it.
