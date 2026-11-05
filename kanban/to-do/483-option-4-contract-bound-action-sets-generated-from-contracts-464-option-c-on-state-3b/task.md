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

- Depends on timewarp-state 142 (graph), 144 (3b State side), 145 (reducer shape) and
  timewarp-mediator 029 having shipped; pin bumps in the same commit.
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
  green; version bump.

## Checklist

- [ ] Generator and State hook
- [ ] Required/optional hook shapes with CS8795 proof
- [ ] Roles migrated; tests, Playwright, template-smoke green
- [ ] 464 closed with its decision recorded

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-06)

## Depends on

- timewarp-state 142, 144, 145; timewarp-mediator 029

## Notes

Steve 2026-11-06. 464 stays parked in in-progress until this task starts; its question.md holds
the history.
