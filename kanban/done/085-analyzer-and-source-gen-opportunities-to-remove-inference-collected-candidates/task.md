# Analyzer and source-gen opportunities to remove inference (collected candidates)

## Description

Running collection per the maintainer directive: **spot places where correctness depends on two
things agreeing by memory/convention, and replace the inference with an analyzer or generator.**
Each candidate below names the agreement, how it breaks, and the proposed mechanism. Pick
individually — each accepted candidate becomes its own task (080-recipe: ship + reconcile in one
PR). Infrastructure exists: `timewarp-architecture-convention-analyzers` (wired repo-wide) for
checks; the generator assemblies for emission.

## Candidates (ranked by observed pain)

1. **Endpoint HTTP verb ↔ contract `[ApiRoute]` verb** — `CreateRoleEndpoint` says `[HttpPost]`,
   the contract says `HttpVerb.Post`; nothing checks they match. Drift = the exact 405/mismatch
   bug class hit in task 078. *Analyzer* (in server projects): for each `BaseEndpoint<TCommand,_>`,
   compare the `[Http*]` attribute against `TCommand`'s `ApiRoute` verb (readable from referenced
   contract metadata). **Highest value — this bug class already bit us.**
2. **Contract has `[ApiRoute]` but no server endpoint** — the 405 itself: roles contracts existed
   for months with no `POST api/Roles`. *Analyzer* (server projects): enumerate referenced contract
   types carrying `RouteTemplate` and warn when no endpoint class covers them. Needs an opt-out for
   deliberately client-only/mock-only contracts (attribute or config list).
3. **Canonical `JsonSerializerOptions` declared 3× by convention** — web-spa `program.cs` DI
   config, web-spa-integration-tests, and web-contracts-tests (`contract-serialization.cs` Design
   region documents this). *Refactor, not analyzer*: hoist one canonical options declaration into
   foundation-contracts; everyone references it. Silent-drift risk today: client and tests could
   diverge without any signal.
4. **Mock factory registration ↔ `GetMockResponseFactory()`** — a contract can define a factory
   that is never registered in `MockWebApiService.Factories` (or vice versa); task 078 had to
   hand-add the registration. *Source generator* (preferred): generate the `Factories` dictionary
   by scanning referenced contract types for `GetMockResponseFactory()` — the registration step
   disappears entirely. (*Analyzer* fallback: warn on unregistered factories.)
5. **Aspire resource names ↔ `ServiceNames` constants** — documented trap (memory:
   `aspire-resource-names-must-match-servicenames`): mismatch → server-side `BaseAddress` resolves
   null, only under Auto render mode. *Analyzer* (app-host project): literals passed to
   `AddProject(...)` resource names must appear in `ServiceNames`.
6. **`BaseEndpoint` ↔ `BaseFastEndpoint` "keep semantics aligned"** — the agreement lives in a
   Design-region comment only (and 079 just edited both constraints in lockstep by hand). Weakest
   candidate: consider extracting the shared `Match`/problem-mapping logic instead of checking
   alignment; an analyzer here is probably over-engineering.

## Checklist

- [x] Split into child tasks (2026-07-02):
      [[085-001-endpoint-coverage-analyzer-verb-agreement-and-missing-endpoint-detection]]
      (candidates 1+2 — same analyzer, two diagnostics),
      [[085-002-hoist-canonical-jsonserializeroptions-into-foundation-contracts]] (candidate 3),
      [[085-003-source-generate-the-mockwebapiservice-factory-registry]] (candidate 4),
      [[085-004-analyzer-aspire-resource-names-must-match-servicenames-constants]] (candidate 5).
- [x] Candidate 6 (BaseEndpoint/BaseFastEndpoint alignment): **no task** — an analyzer here is
      over-engineering; if the duplication ever hurts, extract the shared Match/problem-mapping
      logic instead. Recorded as the decision.
- [x] ~~Keep appending future candidates here~~ — **rejected as a perpetual-task anti-pattern**
      (a collector that never closes never closes). The standing directive lives in agent memory;
      future candidates are surfaced as their own new tasks directly. This task is DONE: all four
      accepted candidates shipped (085-001..004), candidate 6 recorded as no-task.

## Notes

- Directive + seed list recorded in memory (`prefer-analyzers-sourcegen-over-inference`).
- Candidates 1+2 could ship together as one "endpoint coverage" analyzer in the convention
  assembly (both walk server compilations against referenced contract metadata).
