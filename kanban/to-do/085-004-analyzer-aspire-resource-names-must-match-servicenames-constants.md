# Analyzer: Aspire resource names must match ServiceNames constants

## Parent
085-analyzer-and-source-gen-opportunities-to-remove-inference-collected-candidates

## Description

Documented trap: AppHost resource names must equal the `ServiceNames` constants
(web-server/api-server/grpc-server) or server-side `BaseAddress` resolves null — and it only
bites under server render (Auto), making it a delayed runtime failure. Analyzer (app-host
compilation only): string literals passed as resource names to `AddProject(...)` must be members
of `ServiceNames`.

## Checklist

- [ ] Diagnostic (next free TWPA id) in the convention-analyzers assembly, scoped to compilations
      referencing Aspire.Hosting.
- [ ] Match `builder.AddProject<T>("name")` invocations; compare literal against `ServiceNames`
      constants from referenced foundation-contracts metadata.
- [ ] Fixie tests: matching name clean; mismatch flagged; non-service resources (postgres, etc.)
      unaffected (decide: only check names when T is one of the server projects, or check any
      literal that is *close* to a ServiceNames value — start narrow).
- [ ] Wire + reconcile (current app-host should already be clean).

## Notes

- Trap documented in memory `aspire-resource-names-must-match-servicenames` and in
  aspire-app-host/constants.cs inline comment.
