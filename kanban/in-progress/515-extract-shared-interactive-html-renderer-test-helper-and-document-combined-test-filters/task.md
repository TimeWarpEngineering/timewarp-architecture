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

## Goal (implementer restatement)

Stop agents losing time on two test-harness traps in `web-spa-integration-tests`.

1. One shared `InteractiveHtmlRenderer` (a `StaticHtmlRenderer` that reports an interactive
   `RendererInfo`) lives in the suite's `infrastructure/` folder, with Purpose and Design regions
   that say `StaticHtmlRenderer` never calls `OnAfterRender` / `OnAfterRenderAsync`. Beside it is
   `InvokeAfterRenderAsync(ComponentBase component, bool firstRender = false)`, which runs the
   after-render lifecycle a circuit would run. The three page tests (AI page, Authentication page,
   Profile prefill) drop their private copies and `PumpAfterRenderAsync` and use the shared one.
2. The repo testing reference (`skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`)
   names that helper and says how to run several classes or methods in ONE test run, so an
   Aspire suite boots once. `./bin/dev test` already forwards `--project`, `--filter-class`, and
   `--filter-method` (task 516), so the doc shows that syntax and the one-substring rule.

Acceptance: `./bin/dev build` is clean; the three migrated classes and the rest of
`web-spa-integration-tests` pass; `./bin/dev lint-skills` passes; `ganda repo audit` passes.

## Checklist

- [x] Extract shared `InteractiveHtmlRenderer` + `InvokeAfterRenderAsync` helper in `web-spa-integration-tests` with clear Purpose/Design comments
- [x] Migrate `ai-page-assistant-draft-tests.cs`, `authentication-page-draft-tests.cs`, and `profile-page-prefill-tests.cs` to use the shared helper
- [x] Add concise guidance for `InteractiveHtmlRenderer` / `InvokeAfterRenderAsync` and combined MTP test filters to the repo testing skill/reference
- [x] Run `./bin/dev build`, `web-spa-integration-tests`, `./bin/dev lint-skills` (if skills touched), and `ganda repo audit`

## Results

- New `tests/container-apps/web/web-spa-integration-tests/infrastructure/interactive-html-renderer.cs`:
  internal `InteractiveHtmlRenderer : StaticHtmlRenderer` (interactive `RendererInfo`) plus static
  `InvokeAfterRenderAsync(ComponentBase component, bool firstRender = false)`, which runs
  `OnAfterRender` then awaits `OnAfterRenderAsync` (the ComponentBase order). Design region says
  `StaticHtmlRenderer` never runs the after-render step.
- `ai-page-assistant-draft-tests.cs`, `authentication-page-draft-tests.cs`, and
  `profile-page-prefill-tests.cs` drop their private copies, the unused
  `HtmlRendering.Infrastructure` using, and `PumpAfterRenderAsync`; Design regions point at the
  shared host.
- `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`: new sections "Page tests
  without a browser" and "One test run for several classes". `./bin/dev test` already forwards
  `--project`, `--filter-class`, `--filter-method` (task 516), so the doc shows that syntax, the
  one-substring rule, AND between the two options, and the exit-code-8 empty-selection failure.
  `tw-jaribu` is not owned in this repo. `AGENTS.md` Tests row names the two new topics.

### Gates

- `./bin/dev build` — passed, 0 warnings, 0 errors.
- The three edited test files are classes in the `web-spa-integration-tests` project, not
  standalone runfiles (no `#if !JARIBU_MULTI`); ran the project once:
  `./bin/dev test --project web-spa-integration-tests --no-build` — passed, 620/620.
- Doc examples checked: `--filter-class Draft_` — passed 16/16 (AiPage + AuthenticationDraft
  classes); `--filter-class ProfilePagePrefill_ --filter-method Draft` — passed 2/2.
- `./bin/dev lint-skills` — passed (9 skills).
- `ganda repo audit` — passes; one advisory warning `vscode-window-icon` (`.vscode/settings.json`
  missing). That file is gitignored per checkout (`vscode-settings` check), so there is nothing
  to commit for it.

### How to validate

Smoke: `./bin/dev build && ./bin/dev test --project web-spa-integration-tests --no-build --filter-class Draft_`
then `./bin/dev test --project web-spa-integration-tests --no-build --filter-class ProfilePagePrefill_`.
Expect: build 0 warnings / 0 errors; both runs pass (16 and the ProfilePage_Should_ tests) with
no test class declaring its own `InteractiveHtmlRenderer`
(`grep -rn "class InteractiveHtmlRenderer" tests/` lists only `infrastructure/interactive-html-renderer.cs`).

## Session

- Created: 2765133 (2026-11-24)
- 2026-11-24 implementer: shared renderer + docs landed; gates recorded under ## Results.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 5f6bf3a0-c9c2-442c-be7e-bad34119b821, max-turns 500, Done — 2026-11-24T11:43:04Z

## Notes

- Source: Task 514 (`report/analysis.md`, Item 6).
- `StateJsonContext` `SYSLIB1031` (`TypeInfoPropertyName`) was already fixed and documented in `source/container-apps/web/projects/web-spa/state-json-context.cs` during `489-003` and is intentionally out of scope here.
