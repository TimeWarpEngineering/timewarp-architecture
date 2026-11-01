# Round 2 — general
**Date:** 2026-11-01
**Scope reviewed:** commit `97ff4440a` ("build: take Mediator 15 and the current nuget.org pins"), the package-roll scope add. That is 131 files: `Directory.Packages.props`, the Mediator 15 migration across hosts, handlers, behaviors, and test helpers, plus the task.md Results. Round 1 already covered the extraction commit `3cfa29bae` and closed clean. This round re-checked only that the extraction stays intact.

## Summary

The roll does what the scope add asked. `ganda nuget outdated` now reports only the `Microsoft.OpenApi` major, which the implementer correctly left at `2.13.0` after NU1608. `TimeWarp.Mediator` is `15.0.0-beta.4`. That package carries the contracts and TWM001–TWM004 (nuspec description verified), so dropping `.Contracts`/`.Analyzers` is correct. The metadata names the repo's analyzers key on (`TimeWarp.Mediator.ISender`, ``ISender`1``, `IAction`) still exist in the 15 assembly. `dev build` is 0/0, the two named test projects pass, `dev check-version`, `dev lint-skills`, and `ganda repo audit` pass.

One test the gate list does not name fails deterministically. `web-spa-integration-tests` is 577/578: `CatalogAgent_Should.AddPasskey_Off_Settings_Offers_Navigation_And_On_Settings_Runs` fails on both runs. The cause is a semantic change in Mediator 15. CI's `--phase test --group integration` job runs that suite, so the PR cannot go green as is.

## Issues

### Issue 1 — Severity: bug
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:1066
- Description: The test expects `dispatcher.InvokeTool("Credentials.AddPasskey", null)` in automatic-edit mode to throw `InvalidOperationException` mentioning `IWebServerApiService`. That exception is the closed-box SPA's DI failure when it activates `CredentialsState.AddPasskeyActionSet.Handler`. After the roll, nothing is thrown. Shouldly reports: "should throw System.InvalidOperationException but did not". The failure reproduced on 2 of 2 runs (`dotnet test -c Release --no-build` from the project directory, 577 passed, 1 failed).

  Root cause: the Mediator 15 generator resolves the handler **inside** the pipeline chain. In `artifacts/generated/web-spa/.../Sender_ClientPipeline.g.cs`, `Chain_…AddPasskeyActionSet_Action.Step8` calls `GetRequiredService<…AddPasskeyActionSet.Handler>`, behind `Step1` = `StateTransactionBehavior`. The `StateTransactionBehavior` code is the same in State `12.0.0-beta.15` and `12.0.0-beta.16` (decompiled both). It catches every non-cancellation exception, rolls back, publishes `ExceptionNotification`, and rethrows only when `TimeWarpStateOptions.RethrowHandlerExceptions` is true. That option defaults to false and this repo never sets it. So the activation failure becomes an error message bar in `NotificationState`, and `WebMcpDispatcher.InvokeTool` (`source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:237`) goes on to return `WebMcpCompleted(Completed: true)`. Exceptions thrown inside a handler body were already swallowed this way before the roll. What changed is handler-activation failures.

  The task's gate list (Requirements and the restatement) names only the foundation-contracts and sourcegenerator test projects, so the implementer never ran this suite. Results does not claim it ran.
- Suggestion: Keep the test's intent, which is proving that the on-Settings call executed the action rather than returning a navigate offer, and assert the Mediator 15 contract instead. For example, record `NotificationState.Messages.Count` before the call, `await dispatcher.InvokeTool(...)`, then assert one new error message whose Title contains `IWebServerApiService`, and that `HasPendingApproval` is false. Fix the comment at lines 1064–1065 to match. If the intended product behavior is that activation failures still surface to the agent as errors and not as `Completed: true`, make that a product change in the dispatcher or State options instead, and say so in Results. Either way, add `web-spa-integration-tests` to the gates re-run for this walk, since a dispatcher-semantics change lands in that suite.
- Status: open

### Issue 2 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs:26
- Description: The analyzer's Design region still says "TimeWarp.Mediator 14 also has ISender<TScope>", and its method-group example is `Func<IRequest, CancellationToken, Task>`. The test stub (`tests/analyzers/timewarp-architecture-analyzers-tests/spa-mediator-send-analyzer-tests.cs:30-33`) says it "Mirrors TimeWarp.Mediator 14.0.0-beta.1" with `CreateStream` and `Task` returns. In 15, `ISender` returns `ValueTask` and has no `CreateStream`. The analyzer keys on type and method names, so it still works against the 15 assembly (the build is clean). The text now describes an API the repo no longer uses.
- Suggestion: Update the Design text to Mediator 15 (`ValueTask`, no `CreateStream`), and update the stub comment and its `CreateStream` member, or say plainly that the stub is version-agnostic.
- Status: open

## Verified (no finding)

- Pins: `TimeWarp.Contracts` `2.0.0-beta.31`, `TimeWarp.Mediator` and `.Generators` `15.0.0-beta.4`, seven `TimeWarp.State*` at `12.0.0-beta.16`, `Scalar.AspNetCore` `2.18.7`, `Microsoft.OpenApi` `2.13.0`. `ganda nuget outdated` lists only `Microsoft.OpenApi` (1 major), matching the NU1608 stop rule in the scope add.
- No `TimeWarp.Mediator.Contracts`/`.Analyzers`, `RequestHandlerDelegate`, or `IStreamPipelineBehavior` reference remains outside kanban, except a pre-existing commented-out block in `tests/common/timewarp-testing/test-application.cs` and the analyzer test stub (Issue 2).
- `agent-identity-cli` stays on Nuru, which carries Mediator.Contracts 14. Its only ProjectReference is `timewarp-identity`, which has no Mediator reference, so no compilation sees both 14 and 15.
- Generated-app smoke artifacts (`artifacts/template-smoke/work/SmokeDefault/Directory.Packages.props`, written 12:49, before the 12:52 commit) carry the new pins, consistent with Results' smoke claim. I did not re-run `dev template-smoke` this round.
- Extraction intact: `source/foundation/timewarp-contracts/` is absent, the contracts pin is a consumer pin, and the forward test passes (25/25).
- Goal restatement matches the scope add. No misread.

## Gates re-run by reviewer (2026-11-01)

| Gate | Result |
|------|--------|
| `./bin/dev build` | 0 warnings, 0 errors |
| foundation-contracts-tests (Release) | 25/25 |
| timewarp-architecture-sourcegenerator-tests (Release) | 154/154 |
| web-spa-integration-tests (Release, `--no-build`) | **577/578**: Issue 1, reproduced twice |
| `./bin/dev check-version` | exit 0 (source `2.0.0-beta.30`, nuget `2.0.0-beta.29`) |
| `./bin/dev lint-skills` | pass |
| `ganda repo audit` | exit 0; only the `kanban-landed` warning about other tasks' claims |
| `ganda nuget outdated` | 1 outdated (`Microsoft.OpenApi` major, intended) |
| `dev template-smoke` | not re-run (artifacts post-date the code; see above) |
