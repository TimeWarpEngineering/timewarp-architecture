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

## Checklist

- [ ] Generator and State hook
- [ ] Required/optional hook shapes with CS8795 proof
- [ ] Roles migrated; tests, Playwright, template-smoke green
- [ ] 464 closed with its decision recorded

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-06)
- 2026-11-07: dependencies landed (state 141-145, mediator 029, arch 484); spec updated to the shipped shapes.

## Depends on

- timewarp-state 142, 144, 145; timewarp-mediator 029

## Notes

Steve 2026-11-06. 464 stays parked in in-progress until this task starts; its question.md holds
the history.
