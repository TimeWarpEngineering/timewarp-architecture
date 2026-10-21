# Nightly template-smoke matrix: gate aca and aca-neon publish tests on flags

## Description

The scheduled CI run on master (run 39603636207, 2026-10-21, head `1c1a5a917`) fails
`template-smoke --matrix` in three flag-off variants. The default variant (the PR gate) passes,
so the PRs that added these tests merged green.

All failures are in `tests/container-apps/aspire/aspire-tests`:

| Variant | Failing tests |
|---------|---------------|
| SmokeNoPostgres | `AcaNeonPublish_Given_.Publish_Should_GiveWebServerThePooledStringAndLeaveTheDirectStringOffAzure` (`ModuleContaining("name: 'web-server'")`), `AcaPublish_Given_.Publish_Should_KeepAcaScaleAndPostgres` (aca-publish-tests.cs:243) |
| SmokeNoWeb | same two (`Sequence contains no matching element`, aca-neon-publish-tests.cs:97) |
| SmokeNoYarp | `Publish_Should_OnlyExposeTheIngressAndNoDashboard`; publish logs `MissingParameterValueException` for `Parameters:neon-pooled-connection-string` / `neon-direct-connection-string` |

Likely cause: tests (and possibly AppHost parameter wiring) added by task 317 assume `postgres`,
`web`, and `yarp` are on. Gate each test with the template's `#if` flags the way the code under
test is gated, and make the AppHost declare the Neon parameters only where aca-neon actually uses
them.

## Checklist

- [ ] Reproduce with `dev template-smoke --matrix` (or the three variants)
- [ ] Gate the failing tests on the flags the code they assert on depends on (no test asserts on a resource that a flag removed)
- [ ] SmokeNoYarp: find why the Neon parameters are required there and fix the AppHost or the test
- [ ] `dev template-smoke --matrix` passes locally
- [ ] Default `dev template-smoke` still passes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)

## Notes

- Nightly matrix policy: per-PR CI runs the default variant only; flag-off variants run nightly.
- Job: https://github.com/TimeWarpEngineering/timewarp-architecture/actions/runs/39603636207
