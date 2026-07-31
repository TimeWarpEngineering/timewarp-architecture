# Integration Testing

## Framework

**North star (epic 145):** [TimeWarp.Jaribu](https://github.com/TimeWarpEngineering/timewarp-jaribu)
is the single test framework target (zero Fixie, zero xUnit). Assertions:
[Shouldly](https://github.com/shouldly/shouldly).

| Lane | When | Host |
|------|------|------|
| **In-proc** | DI substitution, mediator/pipeline, fixed-port BFF | `WebApplicationHost` / timewarp-testing (ports 7000 / 7255 / 8443) |
| **Closed-box** | Topology, ingress, process isolation | `Aspire.Hosting.Testing` / AppHost (dynamic ports) |

Product-slice tests co-locate as Jaribu runfiles; topology suites stay under `tests/` but
migrate to Jaribu MTP. See root **AGENTS.md** and skill **`tw-feature-placement`** (C-create
host lifetime).

Legacy suite-shaped projects may still use [Fixie](https://github.com/fixie/fixie) +
TimeWarp.Fixie until epic 145 children retire them — **do not add new Fixie or xUnit tests**.

TimeWarp Architecture favors integration testing over unit testing. Mock only code you do not
control.

## Libraries

- [TimeWarp.Jaribu](https://github.com/TimeWarpEngineering/timewarp-jaribu) (target framework)
- [Shouldly](https://github.com/shouldly/shouldly)
- [FakeItEasy](https://github.com/FakeItEasy/FakeItEasy) (when fakes are needed)
- [Fixie](https://github.com/fixie/fixie) (legacy only — epic 145 retirement)
