# Replace Morris.Moxy with standard C# source generators

## Parent

047-migrate-timewarparchitecture-to-root (related tooling improvement)

## Summary

Replace the use of Morris.Moxy (and its custom `.mixin` template system) with standard Roslyn `IIncrementalGenerator` source generators. This will give us a single, coherent, maintainable code generation story using normal C# instead of a custom templating DSL.

## Rationale

- Morris.Moxy derives attribute names from the `.mixin` filename (e.g. `page.mixin` → `pageAttribute`), causing confusing case and naming issues. (File has been renamed to `Page.mixin` to generate `PageAttribute`.)
- The more complex mixins (`Page.mixin`, `RouteMixin.mixin`) contain non-trivial template logic.
- We already maintain real `IIncrementalGenerator`s (e.g. `FastEndpointSourceGenerator`) and have the expertise.
- AI assistance has dramatically lowered the cost of writing proper source generators.
- Having two generation systems (Moxy + custom analyzers) increases cognitive load and long-term maintenance cost.
- Standard generators are more powerful, debuggable, and accessible to other developers.

## Current State

- ~12 `.mixin` files across the repo
- Heavy usage in:
  - `source/container-apps/web/web-spa/` (page routing, state access)
  - Contract projects (command/query mixins, auth mixins, route mixins)
  - Foundation contracts
- Most mixins are small and simple
- `Page.mixin` and `RouteMixin.mixin` are significantly more complex than the others.
- One existing production `IIncrementalGenerator` already in `source/analyzers/timewarp-architecture-analyzers`

## Target State

- Zero `.mixin` files and zero dependency on Morris.Moxy for generation
- All current generation behavior re-implemented as standard `IIncrementalGenerator`(s)
- Clear attribute naming under our control (e.g. `PageAttribute`)
- Better diagnostics and incremental generation behavior
- Single generation technology stack

## Scope

Replace generation currently provided by:

- `Page.mixin`
- `RouteMixin.mixin`
- `state-access-mixin.mixin`
- Command/Query mixins (`CreateCommand`, `DeleteCommand`, `GetQuery`, `GetListQuery`, `UpdateCommand`)
- Various foundation mixins (`IAuthApiRequestMixin`, `IOpenDataQueryParametersMixin`, etc.)
- Any other `.mixin` files

## Checklist

### Phase 1: Inventory & Analysis
- [ ] Catalog all current `.mixin` files and what they generate
- [ ] Classify each by complexity and usage volume
- [ ] Document the exact generated output for each (so we can match behavior)
- [ ] Identify shared infrastructure needed (attribute base classes, helper methods, etc.)

### Phase 2: Design
- [ ] Decide generator architecture (one big generator vs several focused ones)
- [ ] Design attribute API (naming, required vs optional parameters, etc.)
- [ ] Plan incremental generator pipeline(s)
- [ ] Define diagnostic strategy for generation errors

### Phase 3: Implementation
- [ ] Build supporting infrastructure (attribute definitions, common models, helpers)
- [ ] Implement generator(s) for simple command/query style mixins first
- [ ] Implement more complex generators (`PageAttribute`, routing, state access)
- [ ] Add comprehensive tests for generators
- [ ] Migrate usage site by site, removing `.mixin` files as we go

### Phase 4: Cleanup
- [ ] Remove all Morris.Moxy package references
- [ ] Delete all `.mixin` files and related documentation
- [ ] Update any build/CI scripts that referenced Moxy
- [ ] Remove Moxy from `Directory.Packages.props`
- [ ] Verify full solution builds cleanly with no Moxy dependency

### Phase 5: Documentation & Handoff
- [ ] Update developer documentation on how code generation now works
- [ ] Add examples for common patterns
- [ ] Consider creating a small "generator cookbook" for the team

## Notes

- This task became much more attractive after realizing that AI assistance makes writing real `IIncrementalGenerator`s significantly less painful than it was when Moxy was originally adopted.
- `Page.mixin` highlighted some limitations in Morris.Moxy (particularly attribute naming derived from the filename).
- We have precedent and existing investment in proper source generators via `timewarp-architecture-analyzers`.

## Implementation Notes

(Track progress and decisions here once work begins)
