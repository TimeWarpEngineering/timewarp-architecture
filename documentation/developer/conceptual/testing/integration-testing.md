# Integration Testing

## Framework

**Single framework:** [TimeWarp.Jaribu](https://github.com/TimeWarpEngineering/timewarp-jaribu)
(zero Fixie, zero xUnit — epic 145 / task 145-007). Assertions:
[Shouldly](https://github.com/shouldly/shouldly).

| Lane | When | Host |
|------|------|------|
| **In-proc** | DI substitution, mediator/pipeline, fixed-port BFF | `WebApplicationHost` / timewarp-testing (ports 7000 / 7255 / 8443) |
| **Closed-box** | Topology, ingress, process isolation | `Aspire.Hosting.Testing` / AppHost (dynamic ports) |

Product-slice tests co-locate as Jaribu runfiles; topology and host-level suites under `tests/`
use Jaribu MTP. See root **AGENTS.md** and skill **`tw-feature-placement`** (C-create host
lifetime).

TimeWarp Architecture favors integration testing over unit testing. Mock only code you do not
control.

## Libraries

- [TimeWarp.Jaribu](https://github.com/TimeWarpEngineering/timewarp-jaribu)
- [Shouldly](https://github.com/shouldly/shouldly)
- [FakeItEasy](https://github.com/FakeItEasy/FakeItEasy) (when fakes are needed)
