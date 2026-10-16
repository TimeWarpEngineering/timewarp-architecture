# Fix yarp-off generated app aspire-tests that assume the ingress resource

## Description

A generated app made with `dotnet new timewarp-architecture ... --yarp false` builds clean
(`SmokeNoYarp` is in the template-smoke matrix since task 373), but its `aspire-tests` project
fails at runtime: 13 of 57 tests fail.

Found by task 373 while checking the single-flag-off variants. `dev template-smoke` builds the
solution and runs the Jaribu family aggregators, but it does not run `aspire-tests`, so this
runtime failure is not caught.

Observed failures (`dotnet test -c Release` in the generated `aspire-tests` project):
- Every fact in `tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs`: `SetupOnce`
  waits for the `ingress` resource, which does not exist without yarp ("Stopped waiting for
  resource 'ingress' to become healthy because it does not exist in the application model").
  This includes `StateDevTools_Should_RunAndServeItsUi`, which is not ingress-specific.
- `closed-box-app-host-guard-tests.cs`: `Ingress_Endpoints_Should_Not_Use_DevRun_Ports` and
  `Default_Config_Should_Keep_DevRun_Pins` call `Single(...)` for the ingress resource and throw
  "Sequence contains no matching element".

`--grpc false` aspire-tests pass (57/57). `--web false` is fixed by task 373, which excludes
`ingress-smoke-tests.cs` under `!web`.

## Goal (implementer restatement)

A generated `--yarp false` app has no `ingress` resource, so the aspire-tests that wait on or
read that resource must not compile into it. Guard every ingress-only gate and fact with
`#if yarp` (ingress smoke class and the closed-box guard's ingress model facts). Keep the facts
that do not need the ingress (web-server first-run log check, state dev tools UI) running when
yarp is off and web is on. The default app's aspire-tests must stay the same and green. Decide
whether `dev template-smoke` should run each variant's aspire-tests and record the decision with
its cost.

Acceptance: `--yarp false` aspire-tests green; default aspire-tests green and unchanged in
coverage; decision recorded; `dev build` 0/0; `dev test` and `dev template-smoke` green;
`ganda repo audit` clean.

## Requirements

1. Make `--yarp false` aspire-tests pass. Exclude or guard the ingress-only facts under `!yarp`.
   Facts that do not need the ingress (state dev tools, the first-run web-server log check)
   should still run when yarp is off and web is on. Move them out of the ingress file or point
   them at web-server directly.
2. Guard the ingress assertions in `closed-box-app-host-guard-tests.cs` with `#if yarp`.
3. Decide whether `dev template-smoke` should also run each generated app's `aspire-tests` so this
   class of runtime bug fails smoke. Record the decision and its cost.

## Checklist

- [ ] Generated `--yarp false` app: aspire-tests green
- [ ] Generated default app: aspire-tests unchanged and green
- [ ] Decide and record whether template-smoke runs aspire-tests
- [ ] `dev build` 0/0, `dev test` green, `dev template-smoke` green
- [ ] `ganda repo audit` clean

## Notes

Reproduce: run `dev template-smoke`, then `dotnet test -c Release` in
`artifacts/template-smoke/work/SmokeNoYarp/tests/container-apps/aspire/aspire-tests`.
