# Round 1 — general
**Date:** 2026-11-04
**Scope reviewed:** the whole branch diff vs `master` (see review-framework.md), split across three read-only helpers by test area, with every claim below re-checked by the reviewer.

## Summary

The change splits 35 test files into scenario or per-type files. The mechanical result is good:
- No tracked test `.cs` file is over 500 lines.
- A body-by-body comparison of every method in the changed `.cs` files, master vs HEAD with renames off, finds every test method moved verbatim. The only body edits are expected ones: `Register()` type arguments for renamed classes, `SetupOnce` / `CleanUpOnce` delegating to the new fixtures, one comment, and the `.Distinct()` below.
- Shouldly calls are 2993 before and 2993 after.

The reviewer re-ran the gates in this worktree:
- `./bin/dev build`: 0 warnings, 0 errors.
- `-g fast`: 14 of 14 passed.
- `-g integration`: 8 of 8 passed.
- `-g playwright`: 33 of 33 passed.

Jaribu 1.0.0-beta.15 runs registered classes one after another, so the split adds no parallelism.

The main defect is a new host-sharing helper that breaks the repo's written host-lane rule. The rest are accuracy problems in Purpose and Design regions, plus small convention slips.

## Issues

### Issue 1 — Severity: bug
- File: tests/container-apps/web/web-server-integration-tests/features/identity/infrastructure/host-graph-lease.cs:1
- Description: `HostGraphLease` is a reference-counted host share across test classes, held in a process-static field. It is used from:
  - `principals-authorization-fixture.cs:22`
  - `admin-feedback-authorization-fixture.cs:26`
  - `protected-page-deep-link-fixture.cs:21`

  `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md:24-27,52-55` forbids this: never share ASP.NET hosts across classes through process-static fields or a bare static, because "that reintroduces the undisposed/refcounted bug class". The task requires "Shared fixtures follow tw-jaribu and the host-lane conventions."

  Classes run one after another, so at runtime the count only ever goes 0→1→0 and each class still builds its own host. The lease adds risk and adds nothing.

  Its comments are also wrong:
  - The Purpose region calls the original lifetime C-share. On master it was C-create: a `private static HostGraph? Graph` built in `SetupOnce` and disposed in `CleanUpOnce`.
  - The Design region says classes "whose lifetimes overlap share one". Jaribu never runs them that way.
- Suggestion: Delete `HostGraphLease`. Give each split class back its own `private static HostGraph? Graph`, created in `SetupOnce` (keep the `#if(api)` branch) and disposed and nulled in `CleanUpOnce`. Move the host factory into the fixture as a plain `CreateAsync()` that every class calls. Have the fixture helpers take the `WebTestServerApplication` (or `HostGraph`) as a parameter, the same way `EntraChallengeFixture` takes the client. If sharing were really needed, the documented route is C-share through `SessionHostFixture` / `RegisterSessionFixture`, but these in-proc hosts are cheap, so C-create is the convention.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-test-fixture.cs:448
- Description: Screenshot folders no longer match the class that takes the shot:
  - `AskSignInTestFixture.ScreenshotPath` sends shots from all three AskSignIn classes to `PathFor<AskSignInReprobe_Given_Wasm>`.
  - `feedback-attachment-test-fixture.cs:352` sends shots from `FeedbackAttachmentAdmin_Given_Wasm` to the `FeedbackAttachment_Given_Wasm` folder.

  `playwright-screenshot.cs:6` documents the contract as `<test bin>/playwright-screenshots/<test class>/<file>`. File names are still unique and the CI upload is a glob, so nothing breaks, but someone looking for a failing class's shots will not find them under that class.
- Suggestion: Make the fixture helper generic (`ScreenshotPath<TTest>(fileName)`), or pass the path or test type in from each class, so each class writes to its own folder.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/tools/dev-cli-tests/aspire-publish-targets-tests.cs:47
- Description: `.Distinct(StringComparer.Ordinal)` is needed now that `AcaPublish_Given_` and `KubernetesPublish_Given_` each span three partial files. It also hides one case the exact-set assertion used to catch: a second, non-partial class with the same name in another namespace. The `--filter-class` substring in `tools/dev-cli/services/aspire-publish-targets.cs` would still select that class, so it would run as an extra suite.
- Suggestion: Capture `partial` in the class regex. Collapse a repeated name only when every one of its declarations is `partial`, and fail otherwise. Or dedupe on (namespace, name).
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-search-focus-playwright-tests.cs:2
- Description: Purpose and Design regions were copied whole into each half of a split, so they describe tests that now live in the sibling file. The repo treats regions as the record, so this is an accuracy defect.
  - `ask-sign-in-search-focus-playwright-tests.cs:2-11` claims task 381's Ctrl-K-row caret check and the approval-card "composer does not take focus" check. Those assertions are in `ask-sign-in-rename-navigate-playwright-tests.cs:131,164`. This class only covers the search-field close path.
  - `admin-feedback-authorization-agent-tests.cs:6-25` describes the anonymous, Member and Administrator human cases, which are in the `-human` file.
  - Both `principals-authorization-*-tests.cs` files (`:6-14`) carry the same full Design region.
  - `entra-challenge-bootstrap-tests.cs:5-14` describes link merge and 409, which are in `entra-challenge-link-tests.cs`.
- Suggestion: Trim each Purpose and Design region to the tests in its own file. Shared design (host, minting, roles) goes in the fixture's Design region, and each class can point to its siblings.
- Status: open

### Issue 5 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-layout-asserts.cs:122
- Description: Comments still name deleted classes and files:
  - `ask-surface-layout-asserts.cs:122` and `feedback-attachment-admin-playwright-tests.cs:11` name `AskSignIn_Given_Wasm`, which no longer exists.
  - `ask-focus-playwright-tests.cs:10-11` says the Ctrl-K path and the approval card are both proven in `AskSignInReprobe_Given_Wasm`. The approval card is in `AskSignInRenameNavigate_Given_Wasm`.
  - `admin-feedback-authorization-agent-tests.cs:7,18` and `admin-feedback-authorization-human-tests.cs:7,18` name `principals-authorization-tests`, which is now split.
- Suggestion: Point each comment at the class or files that exist now. For the principals file, use `principals-authorization-*-tests`, as the edits on the `source/` side already do.
- Status: open

### Issue 6 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-admin-playwright-tests.cs:15
- Description: A Design-region sentence is cut off. Line 15 ends "...(PlaywrightBrowserSession). Layout", and the rest of the "Layout measures fluent-textarea..." sentence went to the other file.
- Suggestion: Delete the trailing "Layout", or restore the whole sentence if layout is covered in this class.
- Status: open

### Issue 7 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-fixture.cs:149
- Description: `SendChallengeAsync` (line 149) and `SignInAsync` (line 182) take a PascalCase parameter, `HttpClient? Client`. tw-csharp says parameters are camelCase and gives `httpClient` as the example for this exact type.
- Suggestion: Rename the parameter to `httpClient` in both methods and update the uses in their bodies.
- Status: open

### Issue 8 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-rejection-tests.cs:31
- Description: The `Store` field is assigned in `SetupOnce` (line 40) and cleared in `CleanUpOnce` (line 47), but no test in this split class reads it. It was carried over from the original single class.
- Suggestion: Remove the field and its two assignments.
- Status: open

### Issue 9 — Severity: nit
- File: tests/analyzers/timewarp-architecture-analyzers-tests/endpoint-authorize-policy-agreement-edge-analyzer-tests.cs:11
- Description: The new class name `Should_Enforce_Policy_Agreement_Without_Policy_Registration` does not match its contents. The file and its Purpose region say "edge cases": CORS, missing policy, client-only, no endpoints, and the diagnostic id. `Given_ClientOnly_IsClean` does register a policy (`AddPolicy("other", …)`).
- Suggestion: Rename the class to `Should_Enforce_Policy_Agreement_Edge_Cases` to match the file name, the Purpose region and the task rule that a split file's name says which scenario it holds.
- Status: open

## Verified fine

- **Coverage:** no test method lost or edited. Shouldly 2993 → 2993. Runtime totals: dev-cli 342, analyzers 259, sourcegen 155, playwright 33, web-spa-integration 578, web-server-integration 436.
- **Hooks:** none of the originals had per-test `Setup` / `CleanUp`. Every split class that touches a host has `SetupOnce` / `CleanUpOnce`. `CatalogAgent_Should` keeps one `SetupOnce` / `CleanUpOnce` across its partial files. Each Aspire publish class keeps one publish per run.
- **`#if` blocks:** the `#if web` / `postgres` / `yarp` blocks in `aca-publish-secrets-tests.cs` moved as balanced pairs. The `#if(api)` branches are kept in all three web-server fixtures.
- **Renamed classes and files:** nothing outside `kanban/` references the old class or file names in tools, CI, skills, or `.template.config`. The publish `--filter-class` still selects partial classes.
- **Packaging:** the `principal-store-contract-tests/` folder compiles by the default glob in every variant. Its namespace `PrincipalStoreContract_` is unchanged, so the EF and in-memory fixtures still find their base classes.
- **Naming and layout:** all new files are kebab-case and have Purpose regions. Multi-type files now hold one top-level type each.
