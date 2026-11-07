# Option 4: contract-bound action sets generated from contracts

## Description

Source: timewarp-state task 140 `report/analysis.md`, Option 4, and this repo's task 464
(`proposal.md` Option C, `question.md`). Steve decided 2026-11-06 to take the whole 140 family;
this is the last step: 0 → 1 → 3b → 5 → 4.

A thin generic hook lives in State; the contract knowledge lives here. The developer writes

```csharp
[ContractState(typeof(GetRoles), typeof(CreateRole), typeof(DeleteRole))]
public sealed partial class RoleState : State<RoleState>
{
  private partial void OnCreateRoleSucceeded(CreateRole.Response response) => /* update list */;
}
```

and the generator emits each `XxxActionSet.Action` and its `Handler : DefaultApiHandler<…>`
marked for Mediator dispatch (3b), the `RoleState.CreateRole(...)` dispatch method and catalog
entry through State's graph (1), with `Description` and `Permissions` taken from the contract so
the hand-copied `[CatalogAction]` drift goes away.

## Requirements

- Dependencies shipped and pinned (task 484): TimeWarp.State 12.0.0-beta.17 and TimeWarp.Mediator
  15.0.0-beta.5. What they give you: Mediator dispatches to any Action whose attribute derives from
  `GeneratedHandlerAttribute` (abstract; `HandlerTypeName` is a constructor argument because Mediator
  reads it from the reference assembly). State ships `[Reducer]` as one such attribute plus analyzer
  TWS0014, the `[StateManifest]` assembly attribute with `StateManifest.Parse`, and TWS0013 for the
  `JsonSerializerContext` (web-spa has `StateJsonContext`; generated states and actions must land in it
  or TWS0013 fires). Read the beta.17 release note in the timewarp-state repo first.
- This generator's emitted `Action` is not visible to State's generator (generators do not see each
  other's output in one compilation). So either this generator emits the dispatch method and catalog
  entry itself, matching State's shape so TWS0001-0012 and the manifest stay true, or you stop and
  report what State would need to expose. Do not open timewarp-state or timewarp-mediator tasks or PRs
  from this task; record the need in `## Notes` and stop (cross-repo work is Steve's call).
- The attribute that marks the generated `Handler` for Mediator may be a new architecture attribute
  deriving from `GeneratedHandlerAttribute`; do not reuse `[Reducer]` for an `ApiHandler`.
- Required hooks are extended partial methods with an access modifier (missing body is CS8795):
  draft seeding from a fetch, draft-to-request builds, `ConfirmAsync` when the contract is
  `[Destructive]`. The 464 proposal §4f data-loss case must be impossible to compile past.
- Optional hooks (`OnXxxSucceeded`, `OnBuildingRequest`) are `partial void`.
- Markup, CSS, copy, local-only state and actions stay hand-written; the generator emits no Razor.
- Resolve the 140 report's open point on who emits the dispatch method: State exposes the rule as
  a reusable emitter library, or this generator emits it. Record the choice in a Design region.
- Pair with the 464 Option A analyzers (coverage, destructive agreement, catalog agreement, mock
  presence) where they still apply after generation; retire any the generator makes moot.
- Roles slice migrated as the worked example; existing tests pass; Playwright and template-smoke
  green; `dev check-version` (2.0.0-beta.32 is unreleased; bump only if it says so).

## Goal (implementer restatement)

Add a source generator in the web-spa compilation. A person writes
`[ContractState(typeof(GetRoles), typeof(CreateRole), …)]` on a state. For each contract it
emits the `XxxActionSet.Action`, a `Handler : DefaultApiHandler<…>` that Mediator dispatches,
the `RoleState.Xxx(...)` dispatch method, and a catalog entry whose `Description` and
`Permissions` come from the contract. Hooks that must exist (draft seeding from a fetch,
draft-to-request builds, `ConfirmAsync` for `[Destructive]`) are extended partial methods, so a
missing body is CS8795. Optional hooks are `partial void`. No Razor is generated.

Acceptance:

- The generator and its hook shapes ship with a test that proves CS8795 fires when a required
  hook is missing (the 464 §4f data-loss case does not compile).
- A Design region says who emits the dispatch method: State through a reusable emitter, or this
  generator.
- The 464 Option A analyzers that still apply are added; the ones the generator makes moot are
  retired.
- Roles is migrated as the worked example. Existing tests, Playwright, and template-smoke pass.
  `dev check-version` passes.
- 464 is closed with its decision recorded.

## Checklist

- [ ] Generator and State hook
- [ ] Required/optional hook shapes with CS8795 proof
- [ ] Roles migrated; tests, Playwright, template-smoke green
- [ ] 464 closed with its decision recorded

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-06)
- 2026-11-07: dependencies landed (state 141-145, mediator 029, arch 484); spec updated to the shipped shapes.
- 2026-11-07 implementer (ganda task work): read the beta.17 release note, 140 `report/analysis.md`
  Options 3b/4, 464 `proposal.md`, and the shipped Mediator 15.0.0-beta.5 generator source.
  Stopped before writing code. The generated Action cannot be dispatched by the shipped
  Mediator (see Notes, "Blocker"). No product code changed. `ganda repo audit` exits 0; its 3
  warnings are other tasks' claims (kanban-landed), the migration and research files over the
  line cap (already severity warning since 469-001), and the git-ignored local
  `.vscode/settings.json`. None is fixable on this id.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 67ac9ff3-78aa-4dd7-b80d-283880f4cc9d, max-turns 500, Blocked — 2026-11-07T08:20:34Z

## Results

Blocked before implementation. No product code changed. The finding and the unblock choices
are under Notes, "Blocker".

### How to validate

- Smoke: in a timewarp-mediator checkout at `d8bd10e`, run
  `grep -n "CreateSyntaxProvider" source/timewarp-mediator-generators/mediator-generator.cs` and
  `grep -n "NoHandlerException" source/timewarp-mediator-generators/mediator-emitter.cs`.
- Expect: host requests come only from syntax providers (lines 36-47), and each `Send` switch
  ends in a `default` arm that throws `NoHandlerException` (lines 270 and 299). A request type
  emitted by a sibling generator gets no switch arm.

## Depends on

- timewarp-state 142, 144, 145; timewarp-mediator 029

## Notes

Steve 2026-11-06. 464 stays parked in in-progress until this task starts; its question.md holds
the history.

### Blocker: Mediator cannot dispatch an Action this generator emits (2026-11-07)

The spec assumes 3b covers Mediator for a generated action set. It does not. 3b lets a request
**written in source** name a handler another generator emits. It does not let another generator
emit the request.

Evidence (timewarp-mediator `master` at `d8bd10e`, which is what 15.0.0-beta.5 ships):

- `source/timewarp-mediator-generators/mediator-generator.cs:36-37`: host requests come from
  `context.SyntaxProvider.CreateSyntaxProvider(...)` over the host's own syntax trees. A type
  another generator adds is not in those trees.
- `candidate-extractor.declared-handlers.cs`: the `GeneratedHandlerAttribute` fact is read from
  the attributes of a request that discovery already found. A generated request is never found,
  so its attribute is never read.
- `mediator-emitter.cs:245-270` and `:282-299`: `Send` is a closed type switch, and the `default`
  arm throws `NoHandlerException`. There is no runtime handler table (029 rejected one on
  purpose).
- Mediator task 029, `task.md:5`: "A request written in source can name the handler another
  generator emits."

So a generated `RoleState.CreateRoleActionSet.Action` compiles. The generated `RoleState.CreateRole(...)`
calls `Sender.Send(action)`, and that throws `NoHandlerException` at run time. Calling the handler
directly instead would skip the compile-time woven State behaviors (`[TrackAction]`, render
subscriptions, telemetry). Referenced assemblies are walked only when they carry
`[assembly: MediatorAssembly]`, and the Action must be nested in `RoleState`, which lives in
web-spa itself.

State side, for the same decision (smaller):

- This generator can emit the dispatch method itself, in State's shape. It can also add catalog
  entries through a generated registration of the public `ActionCatalogSource` record, because
  the assembly's `ActionCatalogProviderAttribute` is `AllowMultiple = false` and belongs to
  State's generator.
- The State manifest cannot list generated actions. `StateManifestAttribute` is
  `AllowMultiple = false` and State's generator writes it from source it sees. Without a State
  change, "the manifest stays true" fails for every generated action. TWS0001-0012 also see only
  source actions.

What would unblock this walk (Steve's call; no cross-repo task or PR was opened):

1. **Mediator change.** A Mediator-owned attribute on a source type (for example, `[ContractState]`
   deriving from it) that declares the request types another generator will emit, with their
   handler name and response kind as constants (the names have to be constants, since Mediator
   cannot run the architecture naming rule). Paired with a State change so the manifest can take
   facts from another generator, or acceptance that the manifest omits generated actions.
2. **No cross-repo change: Action shells stay in source.** An analyzer reports a contract in
   `[ContractState]` with no `XxxActionSet.Action` in source, and a code fix writes the shell
   (140 Option 2, analyzer plus code fix). The shell carries a new architecture attribute deriving
   from `GeneratedHandlerAttribute`, so Mediator 3b dispatches it, and State's generator emits the
   dispatch method, catalog entry, and manifest from it. This generator emits only the `Handler`
   with the required and optional hooks. Catalog `Description` and `Permissions` are written by
   the code fix and checked against the contract by an analyzer (464 A3), not generated, because
   a generator cannot add an attribute to a type in source. This changes the spec's "the
   generator emits each Action".
3. **Move contract-bound states into a referenced project with `[assembly: MediatorAssembly]`.**
   Mediator would then find generated Actions by metadata. This moves state out of the slice
   layout (`tw-slice-isolation`), so it is listed only for completeness.

## Blocked

2026-11-07: Steve to choose an unblock path (task 483 Notes, "Blocker"). Mediator 15.0.0-beta.5 throws NoHandlerException for any Action a sibling generator emits, because 3b only covers requests written in source. Options: (1) a Mediator change to declare generated requests, plus a State change for the manifest; (2) re-scope so a code fix writes the Action shells into source and this generator emits only Handlers and hooks; (3) move contract-bound states into a referenced [MediatorAssembly] project.
