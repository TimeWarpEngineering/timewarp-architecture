# Round 1 — general
**Date:** 2026-11-04
**Scope reviewed:** same as the framework: the 17 generator files, merge-base de6a1a998 to HEAD 2486db61a.

## Summary

The change splits four oversized incremental generators into a thin `IIncrementalGenerator`
plus `internal` collector, model, and emitter types. It is a mechanical refactor. Moved bodies
match the originals apart from `Collector.`/`Emitter.` qualifiers, `private` to `internal`, and
type renames that avoid collisions (`TypedIdCandidate`, `TypedIdEfModel`,
`FastEndpointGenerationOptions`, `PageParsedRoute`, `ContractPart`, `ContractTarget`). The one
non-move is contracts `BuildRoute`, now split into collector parsing and
`ContractsEmitter.RouteBody`. Its StringBuilder sequence is unchanged on every branch:
no parameters, parameters, and datetime format. Risk is low. No issues raised.

## Verification (independent of the implementer's claims)

- **Byte-identical output.** I built `git archive` snapshots of the merge-base and of HEAD in
  separate `/tmp` dirs and ran `diff -rq` over the whole `artifacts/generated` tree (331 files
  per side under the two generator projects). Every output of these generators is identical. The
  only differences:
  - The generator projects' own `RegexGenerator.g.cs` (2 files) differs only in the containing
    class name (`ContractsGenerator` → `ContractsCollector`, and the page equivalent). That is
    expected, because `[GeneratedRegex]` moved with its method.
  - 332 Razor-generated files differ only in absolute `#line` paths. The diff after stripping
    the temp-dir prefix is empty.
- **Tests:** HEAD snapshot `timewarp-architecture-sourcegenerator-tests` 155/155 passed and
  `timewarp-architecture-analyzers-tests` 259/259 passed. `git diff` shows no test edits.
- **Build:** `dotnet build timewarp-architecture.slnx` has 0 errors in both snapshots. The 51
  warnings in each `/tmp` snapshot are all TimeWarp.Build.Tasks "not a git repository" messages
  from building outside git. The worktree build reports 0 warnings and 0 errors. TWA0004 Purpose
  regions are therefore present on every new file.
- **Line counts:** the largest file is `contracts-collector.cs` at 382 lines. All 17 are 500 lines or fewer.
- **Pipeline shape:** `Initialize` diffs consist only of qualifiers and renames. The providers
  and the Select/Where/Collect/Combine/SelectMany order are unchanged, and the lambdas are still
  `static`. No model newly holds an `ISymbol`, `SyntaxNode`, `Location`, or `Compilation`.
  `TypedIdCandidate.InvalidLocation` was already in the base `Candidate` and is not new. Equality
  and hash members (`TypedIdEfModel`, `ContractTarget`, `FastEndpointGenerationOptions`) are
  unchanged.
- **Raw string literals:** the typed-id and fast-endpoint raw literals keep their nesting depth
  relative to their closing delimiters. Output proves it.
- **Golden:** `IngressRoutePrefixGenerator` is untouched, and the whole generated tree is
  identical, so `goldens/ingress` cannot change.
- **`ganda repo audit`:** passes. The one advisory is `kanban-landed` (nine other tasks' claims
  without worktrees), which is board state and outside this task.
- **Docs:** Design regions stayed in each generator, with a Layout note added.
  `fast-endpoint-source-generator.md` names behaviour only and is not stale. No live skill or
  doc references a moved member name. `skills/tw-web-api-contracts/analysis/` mentions the old
  `Part`/`Target` names, but that folder is the historical record and is not live.

## Considered and dismissed

- Helper pass flagged that the `contracts-generator.cs:48` Layout line ("ContractsCollector builds
  the models in contracts-model.cs") could read as the collector living in the model file. Read
  in context, it says the collector builds the models that the model file defines. It is not a defect.

## Issues

None.
