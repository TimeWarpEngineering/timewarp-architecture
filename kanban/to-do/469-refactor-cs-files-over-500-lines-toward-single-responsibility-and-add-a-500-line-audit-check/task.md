# Refactor .cs files over 500 lines toward single responsibility and add a 500-line audit check

## Description

A `ganda repo audit` check will fail on any `.cs` file longer than 500 lines. Before it can be
enabled, every tracked `.cs` file over that limit has to come under it. A 500-line file is a
smell that the class inside is doing more than one job, so the fix is refactoring toward the
Single Responsibility Principle, not mechanically splitting a class across partial files.

As of 2026-11-03, 55 tracked `.cs` files exceed 500 lines. Of those, 39 hold exactly one
top-level type, 4 are Jaribu runfiles with no type, and 12 hold several top-level types.
The length is mostly single large classes, not stacked types.

## Requirements

- Every tracked `.cs` file is 500 lines or fewer, except generated files that the check exempts.
- Refactoring extracts responsibilities into named types with clear purposes. `partial` is not
  an accepted way to get under the limit.
- `dev build` stays at 0 warnings. Existing tests keep passing. Behavior does not change.
- Each extracted type gets Purpose and Design regions (TWA0004) and kebab file names (TW0001).
- The audit check lands last and is enabled only once the children are merged.

## Children

- 469-001 — the audit or build check itself (exemptions for EF migrations and generated code)
- 469-002 — tools/dev-cli deploy, entra, and template-smoke services and commands
- 469-003 — Aspire AppHost program.cs and web-server program.cs
- 469-004 — source generators under source/analyzers and source/foundation
- 469-005 — web feature and platform source files
- 469-006 — test files, including the multi-type contract test files

## Notes

Full inventory from `git ls-files '*.cs' | xargs wc -l | awk '$1 > 500'` on 2026-11-03.
Each child lists its own files. One file lives under `kanban/done/238-.../research/`; a done
task's research script is not product code and is a candidate for the check's exemption list
rather than a refactor.

## Checklist

- [ ] All children done
- [ ] Audit check enabled and green on master

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
