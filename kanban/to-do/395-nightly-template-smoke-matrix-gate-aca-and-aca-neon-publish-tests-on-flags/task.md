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

## Goal (implementer restatement)

Nightly `template-smoke --matrix` (with generated-app tests) fails three flag-off variants while the default variant stays green. The aca and aca-neon publish facts added for external Neon assume `web`, `postgres`, and `yarp` are all on. Each fact must follow the same flag gates as the AppHost code it reads: do not require a Flexible Server, a web-server connection string, or an external ingress when that flag removed the resource. The aca-neon secret parameters stay declared only on the aca-neon publish path that web-server and the migration bundle actually reference. Default `template-smoke` and the full matrix, including the generated aspire publish suites, both pass.

## Checklist

- [x] Reproduce with `dev template-smoke --matrix` (or the three variants)
- [x] Gate the failing tests on the flags the code they assert on depends on (no test asserts on a resource that a flag removed)
- [x] SmokeNoYarp: find why the Neon parameters are required there and fix the AppHost or the test
- [x] `dev template-smoke --matrix` passes locally
- [x] Default `dev template-smoke` still passes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Implemented (2026-10-21): gated the aca and aca-neon publish facts on `web`/`postgres`/`yarp`. The Neon parameters were already declared only for aca-neon inside that web+postgres block. Aspire logs `MissingParameterValueException` while copying those unset secrets into deployment state; the warning does not fail publish, and a default would put the connection string in the Bicep. SmokeNoYarp failed on `external: true` because there is no ingress, not because the parameters were missing.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-21T16:54:15Z
- Review oracle (2026-10-21): tw-implementation-review, effort 1, roster general (claude-opus-5-5), session id not exposed, 1 round, disposition clean

## Results

The pooled-string and Flexible Server facts compile only when `web` and `postgres` are on, matching the AppHost block that declares Neon and Flexible Server. Passkey-host facts compile only when `web` and `yarp` are on. Without an ingress module, aca-neon requires zero container apps with `external: true` and still forbids the dashboard, mock auth, and devtools. Leak checks (no direct Neon parameter, no web-migrations module, no scale-to-zero, no Neon names on the aca target) stay unconditional.

The two Neon parameters are unchanged: secret, no default, declared only when `Publish:Target=aca-neon` inside `#if web` / `#if postgres`. Publish still emits the pooled name as `@secure()`. The direct parameter stays so migrations can reference it without colliding with the pooled `ConnectionStrings__postgres-db` snapshot.

### How to validate

**Smoke**

```bash
cd tests/container-apps/aspire/aspire-tests
dotnet test -c Release -- --filter-class AcaNeonPublish_Given_
dotnet test -c Release --no-build -- --filter-class AcaPublish_Given_
dotnet run tools/dev-cli/dev.cs -- template-smoke --matrix --app-tests
```

**Expect**

- In-repo (all flags on): aca-neon 7/7 and aca 13/13. The aca-neon publish log warns `MissingParameterValueException` for `neon-pooled-connection-string` and `neon-direct-connection-string`, then saves 3 parameter values. The aca publish log saves 5 parameter values and does not warn about those names.
- `template-smoke --matrix --app-tests` exits 0 (`Template smoke SUCCEEDED`). That command is the nightly gate and includes SmokeDefault, which is the default `template-smoke` variant. Aspire-tests: SmokeDefault 72/72, SmokeNoPostgres 69/69, SmokeNoApi 71/71, SmokeNoWeb 56/56, SmokeNoGrpc 72/72, SmokeNoYarp 60/60.

**Not in scope**

- A second bare `template-smoke` without `--matrix`. SmokeDefault already generated, built, and passed its app tests inside the matrix run.

### Review

- **Disposition:** clean. 1 round at effort 1 with the `general` reviewer.
- **Final counts:** bug 0, suggestion 0, nit 0. All statuses are 0 open, 0 fixed, 0 wontfix.
- **Reviewer re-ran:** in-repo `AcaNeonPublish_Given_` 7/7 and `AcaPublish_Given_` 13/13, both green.
- **Not re-run by the reviewer:** the full `template-smoke --matrix --app-tests`. It has no single-variant option and runs past the foreground limit. The flag-off counts above are implementer-reported, and the nightly run confirms them.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Notes

- Nightly matrix policy: per-PR CI runs the default variant only; flag-off variants run nightly.
- Job: https://github.com/TimeWarpEngineering/timewarp-architecture/actions/runs/39603636207
