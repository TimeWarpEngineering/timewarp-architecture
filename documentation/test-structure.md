# Testing Convention

## North star (epic 145)

**Single-framework Jaribu** (zero Fixie and zero xUnit). Assertions: **Shouldly**. Decision
record: `kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md`
§6. Policy for agents: root **AGENTS.md** Stack/tests; co-located runfile authoring:
skill **`tw-feature-placement`**.

- **New product-slice tests:** co-located `*-tests.cs` under `features/` / `platform/`.
- **Host lifetime (in-proc):** C-create — per-class `SetupOnce` / `CleanUpOnce` owns the graph
  (HostGraphFactory when task 145-002 lands).
- **Remaining Fixie / xUnit suites:** migration debt under epic 145 — do not extend them.

## Historical Fixie naming (legacy suite-shaped tests)

Older suite-shaped Fixie tests used a highly configurable convention (`TestingConvention.cs` /
TimeWarp.Fixie). Naming pattern that still appears in unmigrated suites:

The `namespace` is the name of Class being tested  
The Class name is the Method/Action/Request being tested  
The Method name is the Result expected stating any conditions

Example:

  Test Name:	CounterState.IncrementCounterAction_Should.Decrement_Count_Given_NegativeAmount  
  File Name: `CounterState_IncrementCounterAction_Tests.cs`

  * Namespace: CounterState
  * Class: IncrementCounterAction_Should
  * Method: Decrement_Count_Given_NegativeAmount

The Filename uses the above `<Namespace>_<Class-Verb>_Tests.cs`

**Jaribu co-located naming** prefers the SUT / Action_Given_ hierarchy (see **`tw-jaribu`** and
the create-role / weather-forecast exemplars).
