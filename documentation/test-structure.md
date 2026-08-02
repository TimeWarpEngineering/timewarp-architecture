# Testing Convention

## North star (epic 145 — complete)

**Single-framework Jaribu** (zero Fixie and zero xUnit). Assertions: **Shouldly**. Decision
record: `kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md`
§6. Fixie/TimeWarp.Fixie retired in task **145-007**. Policy for agents: root **AGENTS.md**;
co-located runfile authoring: skill **`tw-feature-placement`**.

- **New product-slice tests:** co-located `*-tests.cs` under `features/` / `platform/`.
- **Host lifetime (in-proc):** C-create — per-class `SetupOnce` / `CleanUpOnce` owns the graph
  (HostGraphFactory).
- **Host-level suites:** suite-shaped under `tests/`, Jaribu Microsoft.Testing.Platform with a
  project-local `global.json` test.runner.

## Naming (Jaribu)

Prefer the SUT / Action_Given_ hierarchy (see **`tw-jaribu`** and the create-role /
weather-forecast exemplars). Suite-shaped projects use the same static `RegisterTests<T>` +
public static `Task` methods as MTP aggregators.

Historical suite-shaped names (namespace = SUT, class = action, method = result) may still
appear in older files; new tests follow Jaribu conventions.
