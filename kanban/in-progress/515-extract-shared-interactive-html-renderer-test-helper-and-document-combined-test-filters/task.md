# Extract shared InteractiveHtmlRenderer test helper and document combined test filters

## Description

Walk performance analysis (Task 514, Item 6) found two repeated test-harness friction traps costing agents **20+ minutes** during implementation and fix passes in `timewarp-architecture`:

1. **Duplicated `InteractiveHtmlRenderer : StaticHtmlRenderer` + hidden `OnAfterRenderAsync` trap (`~18m` lost on Task 490):**
   - `InteractiveHtmlRenderer` is currently copy-pasted as a private nested class in three `web-spa-integration-tests` files:
     - `tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs`
     - `tests/container-apps/web/web-spa-integration-tests/features/admin/authentication-page-draft-tests.cs`
     - `tests/container-apps/web/web-spa-integration-tests/features/profiles/profile-page-prefill-tests.cs`
   - ASP.NET Core's `StaticHtmlRenderer` never invokes `OnAfterRenderAsync`. On Task 490, the implementer spent ~18 minutes running 11 `ilspycmd` decompiles and 4 `/tmp/*.cs` reflection scripts before adding a local `PumpAfterRenderAsync` reflection call in `profile-page-prefill-tests.cs`.
2. **Repeated Aspire + Postgres cold boots from chaining separate filtered `dotnet test` commands (`~3m` wasted on Task 490):**
   - Because neither `AGENTS.md` nor repo testing skills document how to combine multiple classes/methods into a single Microsoft.Testing.Platform (`--filter-class` / `--filter-method`) invocation, agents chain multiple separate `dotnet test` runs against Aspire integration test projects — paying a ~44s Aspire + Postgres boot on each call (`218s` across 5 calls on Task 490).

## Depends on

- 516

## Requirements

1. **Shared `InteractiveHtmlRenderer` helper in `web-spa-integration-tests`:**
   - Extract one shared internal `InteractiveHtmlRenderer` (with a clear `#region Purpose` / `#region Design` explaining that `StaticHtmlRenderer` does **not** run `OnAfterRenderAsync`) into a shared test-infrastructure location in `tests/container-apps/web/web-spa-integration-tests/`.
   - Provide a reusable `InvokeAfterRenderAsync(ComponentBase component, bool firstRender = false)` helper alongside it so page-draft and prefill tests do not re-implement reflection boilerplate or rediscover the `StaticHtmlRenderer` lifecycle limitation.
   - Replace the three duplicated private `InteractiveHtmlRenderer` classes and `PumpAfterRenderAsync` with the shared helper.
   - Add a brief pointer in `skills/tw-blazor/` (or the co-located testing reference under `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`) noting the shared `InteractiveHtmlRenderer` and `InvokeAfterRenderAsync` helper for `web-spa-integration-tests`.
2. **Document combined MTP test filters (and check `./bin/dev test` ergonomics):**
   - Document in `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md` (and `skills/tw-jaribu/` if owned in this repo; otherwise in the repo's testing reference) how to pass combined class/method filters in a single test run so Aspire/Postgres test suites boot once instead of once per test class.
   - Check whether `./bin/dev test` already supports forwarding class/method filters; if it does, document the `./bin/dev test` syntax directly, and if not, document the single-command `dotnet test --project ... -- --filter-class ...` form without broadening this task into a CLI rewrite.
3. **Verification:**
   - `./bin/dev build` (0 warnings, 0 errors), all `web-spa-integration-tests` pass, `dev lint-skills` passes (if `skills/` changed), and `ganda repo audit` passes.

## Checklist

- [ ] Extract shared `InteractiveHtmlRenderer` + `InvokeAfterRenderAsync` helper in `web-spa-integration-tests` with clear Purpose/Design comments
- [ ] Migrate `ai-page-assistant-draft-tests.cs`, `authentication-page-draft-tests.cs`, and `profile-page-prefill-tests.cs` to use the shared helper
- [ ] Add concise guidance for `InteractiveHtmlRenderer` / `InvokeAfterRenderAsync` and combined MTP test filters to the repo testing skill/reference
- [ ] Run `./bin/dev build`, `web-spa-integration-tests`, `./bin/dev lint-skills` (if skills touched), and `ganda repo audit`

## Session

- Created: 2765133 (2026-11-24)

## Notes

- Source: Task 514 (`report/analysis.md`, Item 6).
- `StateJsonContext` `SYSLIB1031` (`TypeInfoPropertyName`) was already fixed and documented in `source/container-apps/web/projects/web-spa/state-json-context.cs` during `489-003` and is intentionally out of scope here.
