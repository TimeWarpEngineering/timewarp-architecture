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
- [ ] Analyzer project wiring (reuse the existing analyzer assembly + package plumbing).
- [ ] Rule engine: map `RuleFor(...).NotEmpty()/.NotNull()` -> property symbols on `T`.
- [ ] Follow `SetValidator(new XDetailsValidator())` into shared `AbstractValidator<IDetails>`.
- [ ] Diagnostic A (nullable + presence rule) and Diagnostic B (`= string.Empty` + presence rule).
- [ ] Fixie analyzer tests incl. the hard edges above + a negative test (`string?` w/o rule = clean).
- [ ] Run across TWA contracts; confirm it retro-catches the 077 targets.
- [ ] **Clear every violation the analyzer reports in this same PR** (that work *is* 077 —
      `hello.cs`, `track-event.cs`, `create/update-todo-item.cs`, plus anything else it surfaces), so
      the tree is green from the first commit that turns the rule on. See "Sequencing" below.
- [ ] (Optional) code fix provider: nullable->non-nullable and `= string.Empty` -> `= null!`.

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
