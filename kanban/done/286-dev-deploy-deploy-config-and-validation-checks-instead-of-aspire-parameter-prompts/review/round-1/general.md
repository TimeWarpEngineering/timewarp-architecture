# Round 1 — general
**Date:** 2026-10-08
**Scope reviewed:** commit 93c6a653b (HEAD~1..HEAD)

## Summary
The change meets the task's requirements. Parameters resolve with the env var first, then the user secret, which matches .NET config order and the AppHost's own lookup. They are forwarded after `--` as separate argv entries. Amuru's TTY mode builds `ProcessStartInfo.Arguments` from CliWrap's escaped argument string and applies the env vars, so values with spaces or `=` survive, and aca's `Azure__SubscriptionId` still reaches Aspire. `Parameters__k8s-namespace` maps to `Parameters:k8s-namespace` in .NET config because only `__` is translated. TtyPassthroughAsync configures no stdin string, so a redirected stdin is inherited and does not throw. I re-ran `dotnet test -c Release` in tests/tools/dev-cli-tests and got 134/134. I found no blocking bugs. The findings below cover robustness: probe hangs, a failed user-secrets probe that is reported as unset parameters, and a parameter list that can drift from the AppHost. They also cover gaps in the process-half orchestration and a few text nits.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/services/aspire-deploy-preflight.cs:213
- Description: The read-only probes have no timeout. `ProbeAsync` runs `kubectl`, `kind`, `helm` and `dotnet user-secrets` through `CaptureAsync` without `WithTimeout`. `--request-timeout=5s` limits only the API call. Several cases can still hang preflight indefinitely: a kubeconfig exec credential plugin that waits for an interactive login (AKS kubelogin device code, `gke-gcloud-auth-plugin`), and `kind get clusters` while the Docker daemon is wedged. The task's goal is to "fail fast with one clear message".
- Suggestion: Add `.WithTimeout(...)` in `ProbeAsync` (Amuru ShellBuilder has `WithTimeout`; a timed-out result is `Success == false`). A timed-out probe then becomes the normal refusal, and the reachability message can say "timed out".
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/services/aspire-deploy-preflight.cs:70-71
- Description: A failed `dotnet user-secrets list` probe is treated the same as "nothing set". `ResolveParameters` gets `userSecretsProbeSucceeded == false` and reports all four parameters as missing. The refusal then tells the operator to `dotnet user-secrets set` values they may already have set. This can happen when the dotnet CLI cannot evaluate the AppHost csproj (restore or MSBuild evaluation error), and the probe's stderr is discarded. The aca path has the same silent fallback (line 91), but it is less harmful there because az is the fallback.
- Suggestion: When the probe fails and something is missing, add one problem line that names the failure and the first stderr line, for example "could not read AppHost user secrets (`dotnet user-secrets list` failed: …)".
- Status: open

### Issue 3 — Severity: suggestion
- File: tools/dev-cli/services/aspire-deploy.cs:122-125 (and tests/tools/dev-cli-tests/aspire-deploy-tests.cs, `Kubernetes_Should_RequireTheValuelessAppHostParameters`)
- Description: The required-parameter list is a hand-kept copy of the AppHost's value-less `AddParameter` calls (program.cs:246-257, constants.cs:68-73). The test asserts the dev CLI against string literals, not against the AppHost. If someone adds or renames a value-less kubernetes parameter in program.cs, preflight passes and Aspire hits the same "Failed to read input in non-interactive mode" crash this task fixes. That is the agreement-by-memory pattern AGENTS.md asks to replace with a build-time check.
- Suggestion: At minimum, Compile-include `source/container-apps/aspire/projects/aspire-app-host/constants.cs` in dev-cli-tests (it is an `internal class Constants`, but it depends on `TimeWarp.Foundation.Configuration`, so check the include is cheap), or move the four names into a shared file. Then assert `RequiredParameters(Kubernetes)` equals the AppHost constants. Better: a test that scans program.cs for `AddParameter(<Name>)` calls without a default inside the kubernetes branch.
- Status: open

### Issue 4 — Severity: suggestion
- File: tools/dev-cli/services/aspire-deploy-preflight.cs:146-178
- Description: Only the pure halves of the refusal logic are tested. Several orchestration decisions live only in the process half, which dev-cli-tests does not compile:
  - a kind error suppresses the reachability error (root-cause choice);
  - the registry is probed only for kind contexts and only when `registry-endpoint` resolved;
  - `resolveParameters: false` (deprovision) skips parameters and the registry;
  - all kubernetes problems collect into one report.

  The requirement asks for tests of "each validation refusal". Each message is covered, but the precedence and combination rules are proven only by the manual run in the PR.
- Suggestion: Move the decision into a pure function, for example `CollectClusterProblems(context, reachOk, reachErr, kindOk, kindOut, registry?, registryAnswered)` in aspire-deploy.cs, and test the combinations. CheckClusterAsync would then only gather probe results.
- Status: open

### Issue 5 — Severity: suggestion
- File: tools/dev-cli/endpoints/deprovision-command.cs:54 (preflight with `resolveParameters: false`)
- Description: `dev deprovision --target kubernetes` forwards no `--Parameters:*`, while the AppHost still declares the four parameters with no value. Per the aspire.dev `aspire destroy` page, Helm destroy uses "the persisted release name and namespace" from the deployment state. It is not shown that destroy's pipeline skips parameter resolution when that state is missing, stale or from another checkout. `dev deprovision --yes` (`--non-interactive`) could then hit the same `process-parameters` crash. The skill and the Design region say deprovision does not need parameters, but no proof run is cited.
- Suggestion: Verify with a `--list-steps` / dry run of `aspire destroy` against a kind deployment. If the parameters step runs, resolve parameters best-effort in deprovision and forward any that are set, without refusing on missing ones.
- Status: open

### Issue 6 — Severity: nit
- File: tools/dev-cli/services/aspire-deploy-preflight.cs:195
- Description: The registry probe catches `TaskCanceledException` with no condition. If the operator presses Ctrl+C during the probe, the cancellation is swallowed and reported as "registry does not answer". An HTTPS registry with a self-signed or untrusted certificate throws `HttpRequestException` and gets the same "does not answer" message, which points the operator away from the real problem (TLS).
- Suggestion: Add `when (!cancellationToken.IsCancellationRequested)` to the catch. Optionally pass the exception's message into the refusal, as the kubectl reachability check already does.
- Status: open

### Issue 7 — Severity: nit
- File: tools/dev-cli/services/aspire-deploy.cs:144
- Description: The env var is read case-sensitively, but .NET config matches keys case-insensitively. On Linux, `Environment.GetEnvironmentVariable("Parameters__k8s-namespace")` will not find `PARAMETERS__K8S-NAMESPACE`, which the AppHost's configuration would accept. The dev CLI then refuses a value Aspire would have used. The user-secret parse is case-insensitive (line 282), so the two sources are inconsistent.
- Suggestion: Either document that the canonical casing is required, or scan `Environment.GetEnvironmentVariables()` case-insensitively for the key.
- Status: open

### Issue 8 — Severity: nit
- File: tools/dev-cli/services/aspire-deploy.cs:239
- Description: The registry refusal prints the placeholder `--project '<apphost csproj>'`. The missing-parameter refusal next to it prints the real AppHost path, which can be copied and run. The two refusals in one report are inconsistent, and only one is paste-ready.
- Suggestion: Pass `appHostProject` into `ValidateRegistry` and print the real path.
- Status: open

### Issue 9 — Severity: nit
- File: tools/dev-cli/services/aspire-deploy.cs:363, :118
- Description: The plan prints every forwarded value in clear text, and each value is also visible in the `aspire` process's argv (`ps`). This is fine today because all four kubernetes parameters are non-secret. But `RequiredParameters` is the extension point for a target that needs a secret parameter, and `ResolvedDeployParameter` has no notion of secret. Adding one would print it to the terminal and expose it in argv.
- Suggestion: Record in the Design region that required parameters must be non-secret (secrets stay in user secrets or env vars and Aspire reads them itself). Alternatively, add a `Secret` flag that masks the value in the plan and passes it through the env var instead of argv.
- Status: open

### Issue 10 — Severity: nit
- File: tools/dev-cli/endpoints/deploy-command.cs:10
- Description: The Design region's "Thin wrapper over" synopsis still shows `aspire deploy … -- --Publish:Target=<t>` without the forwarded `--Parameters:<name>=<value> …`. The added text a few lines later describes the forwarding, but the synopsis line is now incomplete. The skill's synopsis was updated.
- Suggestion: Append `[--Parameters:<name>=<value> …]` to the synopsis line.
- Status: open
