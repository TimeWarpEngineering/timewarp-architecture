# Round 2 — general
**Date:** 2026-10-08
**Scope reviewed:** 20de37319 fix delta + re-verification of round-1 M1–M10

## Summary
All ten round-1 findings are fixed. Checks run:

- `tests/tools/dev-cli-tests`: `dotnet test -c Release` passed 147/147 (round 1 had 134).
- `dotnet build tools/dev-cli/dev.cs` built with no warnings, so the process half compiles too.
- All five `.githooks/*.cs` runfiles build against the bumped Amuru 2.0.0-beta.2 pin.

Timeout handling is correct per the Amuru 2.0.0-beta.2 XML docs. With `WithNoValidation`, a timed-out `CaptureAsync` returns `TimedOut = true` and `Success = false` (exit 124), and it does not throw. Cancellation by the caller still throws `OperationCanceledException`, so Ctrl+C is not swallowed. The worst case per probe is the timeout plus the 5s default grace period.

The new pure functions (`CollectParameterProblems`, `CollectClusterProblems`, `CollectKubernetesProblems`, `RegistryToProbe`) reproduce the old precedence:

- A missing kind cluster suppresses the reachability error.
- The registry is probed only when deploying to a kind context with `registry-endpoint` resolved.
- Parameter problems come first, then helm, then the context or cluster problems.

Each rule has a test. `CaseInsensitiveEnvironment` lets an exact-case match win and otherwise matches case-insensitively, which is a reasonable deterministic choice.

Deprovision forwarding works as described:

- It resolves parameters best-effort and never refuses on a missing one.
- It does not probe the registry.
- It forwards set values after the `--Publish:Target` argument.

The Design regions in deploy-command.cs, deprovision-command.cs, aspire-deploy.cs and aspire-deploy-preflight.cs match the new code. Two small new issues are listed below. Neither blocks the change.

## Prior findings
| ID | Severity | Status | Note |
|----|----------|--------|------|
| M1 | suggestion | fixed | `ProbeAsync` uses `.WithTimeout(ProbeTimeout)` (20s). Timed-out probes give specific messages: helm, the kubectl context, kubectl reachability, kind, az and user-secrets. Semantics confirmed against the Amuru beta.2 XML docs. |
| M2 | suggestion | fixed | `secretsFailure` (not on PATH / timed out / first stderr line) is passed to `CollectParameterProblems`, which reports it before the missing list, and only when something is missing. Tested. |
| M3 | suggestion | fixed | `KubernetesRequiredParameters_Should_MatchTheAppHostsValuelessParameters` scans program.cs and constants.cs, both inside the kubernetes branch and across the whole file. See N1 for a regex gap. |
| M4 | suggestion | fixed | Decisions moved into the pure `CollectClusterProblems` / `CollectKubernetesProblems` / `RegistryToProbe`, plus `ClusterProbeResults`. Precedence and combination tests added. |
| M5 | suggestion | fixed | Deprovision resolves parameters best-effort and forwards the set ones (`BuildDestroyArguments` with parameters), and never refuses. Design region and skill updated. No proof run of `aspire destroy` parameter handling is cited, but the hedge makes that moot. |
| M6 | nit | fixed | Both catches use `when (!cancellationToken.IsCancellationRequested)`. The failure reason (including the TLS inner exception) is passed into `ValidateRegistry`. |
| M7 | nit | fixed | `CaseInsensitiveEnvironment(EnvironmentVariables())`, with a test. The skill and the Design region say the name is matched case-insensitively. |
| M8 | nit | fixed | `ValidateRegistry` takes `appHostProject` and prints the real path. The test asserts there is no `<apphost csproj>` placeholder. |
| M9 | nit | fixed | aspire-deploy.cs, deploy-command.cs and the skill now say forwarded parameters must be non-secret. |
| M10 | nit | fixed | The synopses in deploy-command.cs and deprovision-command.cs list `[--Parameters:<name>=<value> …]`. |

## New issues

### N1 — Severity: nit
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs (`ValuelessParameterPattern`, `AddParameter\(\s*(\w+)\s*\)`)
- Description: The agreement test only detects value-less parameters written as `AddParameter(<Identifier>)`. It misses value-less forms that also make Aspire prompt:
  - `AddParameter(Name, secret: true)`, which is the shape a secret would take now that the Design region forbids adding secrets to `RequiredParameters`;
  - `AddParameter("literal")`;
  - a named argument `AddParameter(name: X)`.

  With any of these, preflight passes and the deploy hits the non-interactive prompt crash this task fixes.
- Suggestion: Widen the pattern to treat an `AddParameter(` call as value-less when its second argument is absent or is only `secret:`/`publishValueAsDefault:`. Alternatively, fail the test when an `AddParameter(` call appears that the pattern cannot classify.
- Status: open

### N2 — Severity: nit
- File: skills/tw-deploy/SKILL.md:120 (and the matching Design wording "Every probe has a timeout (ProbeTimeout)" in tools/dev-cli/services/aspire-deploy.cs)
- Description: The text says "Each probe times out after 20 seconds". The registry HTTP probe uses a 5s `HttpClient` timeout, and the kubectl reachability call is limited by `--request-timeout=5s`, so the 20s only applies to the process-level limit. The skill also says "if `dotnet user-secrets list` fails, the report says so", but the report says so only when a parameter is also missing.
- Suggestion: Reword to "each process probe is bounded at 20 seconds" and "…says so when a parameter is reported missing".
- Status: open
