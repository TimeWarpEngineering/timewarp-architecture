# Round 1 — merged findings
**Date:** 2026-11-04
**Sources:** general (one reviewer file, fed by three read-only area helpers)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 5 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/identity/infrastructure/host-graph-lease.cs:1
- Description: `HostGraphLease` shares an ASP.NET host across classes through a process-static, reference-counted field. `co-located-jaribu-runfiles.md:24-27,52-55` forbids this, and the task requires fixtures to follow the host-lane conventions. It also has no runtime effect, because Jaribu runs classes one after another. Its Purpose region calls the original lifetime C-share (it was C-create), and its Design region describes an overlap that never happens.
- Suggestion: Delete the lease. Each split class owns a `static HostGraph?`, created in `SetupOnce` from a shared fixture factory and disposed and nulled in `CleanUpOnce`. Fixture helpers take the host as a parameter.
- Source: general
- Disposition notes: Fixed in the round-2 fix pass. `host-graph-lease.cs` is deleted. Each of the six split classes owns a `static HostGraph?`: SetupOnce stores `<Fixture>.CreateHostAsync()`, and CleanUpOnce disposes it and sets it to null. Fixture helpers that touch the host take `WebTestServerApplication web`. The fixture Purpose and Design regions now say C-create.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-test-fixture.cs:448
- Description: Screenshots go to another class's folder. All three AskSignIn classes write to the `AskSignInReprobe_Given_Wasm` folder (`ask-sign-in-test-fixture.cs:448`), and FeedbackAttachmentAdmin writes to the `FeedbackAttachment_Given_Wasm` folder (`feedback-attachment-test-fixture.cs:352`). Both break the `<test class>` folder contract in `playwright-screenshot.cs:6`.
- Suggestion: A generic `ScreenshotPath<TTest>`, or have each class pass its own type.
- Source: general
- Disposition notes: Fixed. The fixtures expose `ScreenshotPath<TTest>`, and the helpers that take screenshots are generic on `TTest`. `AskSignInTestFixture.Session.StartAsync<TTest>()` keeps the owning class's path. Each test class has a private `ScreenshotPath` for its own type. The playwright run wrote `AskSignInRenameNavigate_Given_Wasm/` and `FeedbackAttachmentAdmin_Given_Wasm/` folders.

### M3 — Severity: suggestion — Status: fixed
- File: tests/tools/dev-cli-tests/aspire-publish-targets-tests.cs:47
- Description: The new `.Distinct()` would also hide a second, non-partial class with the same name in another namespace. The substring filter would still select that class.
- Suggestion: Collapse a repeated name only when every declaration is `partial`, or dedupe on (namespace, name).
- Source: general
- Disposition notes: Fixed. The regex captures an optional `partial` before `class`. Repeated names collapse to one only when every declaration is partial. Otherwise each declaration stays, so the exact-set assertion fails.

### M4 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-search-focus-playwright-tests.cs:2
- Description: Each half of a split kept the full original Purpose and Design regions, so they describe tests that live in the sibling file. Affected: ask-sign-in-search-focus, both admin-feedback-authorization halves, both principals-authorization halves, entra-challenge-bootstrap.
- Suggestion: Trim each region to its own file. Put shared design in the fixture's region.
- Source: general
- Disposition notes: Fixed. Each half's regions are trimmed to its own tests and point to the sibling file. The files: ask-sign-in-search-focus (its Ctrl-K focus text moved to rename-navigate, where those checks run), both admin-feedback-authorization halves, both principals-authorization halves, the three entra-challenge classes (the shared design moved to the `EntraChallengeFixture` Design region), and the protected-page-deep-link pages and settings comment blocks.

### M5 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-layout-asserts.cs:122
- Description: Comments name deleted or wrong classes:
  - `ask-surface-layout-asserts.cs:122` and `feedback-attachment-admin-playwright-tests.cs:11` name `AskSignIn_Given_Wasm`.
  - `ask-focus-playwright-tests.cs:10-11` names the wrong class for the approval card.
  - `admin-feedback-authorization-{agent,human}-tests.cs:7,18` name `principals-authorization-tests`.
- Suggestion: Point each comment at the current class or files.
- Source: general
- Disposition notes: Fixed. `AskSignIn_Given_Wasm` becomes `AskSignInReprobe_Given_Wasm`. ask-focus names Reprobe for Ctrl-K and RenameNavigate for the approval card. admin-feedback names the `principals-authorization-*-tests` files.

### M6 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-admin-playwright-tests.cs:15
- Description: A Design sentence is cut off and ends on "Layout".
- Suggestion: Remove the fragment, or restore the whole sentence.
- Source: general
- Disposition notes: Fixed. The trailing "Layout" fragment is removed. The full layout sentence stays in the form-class file, which owns those tests.

### M7 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-fixture.cs:149
- Description: The parameter `HttpClient? Client` in `SendChallengeAsync` (line 149) and `SignInAsync` (line 182) is PascalCase. tw-csharp requires camelCase `httpClient`.
- Suggestion: Rename the parameter to `httpClient`.
- Source: general
- Disposition notes: Fixed. The parameter is renamed to `httpClient` in `SendChallengeAsync` and `SignInAsync`.

### M8 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-rejection-tests.cs:31
- Description: The `Store` field is set and cleared but never read in this class.
- Suggestion: Remove the field and both assignments.
- Source: general
- Disposition notes: Fixed. The `Store` field and both of its assignments are removed from `Challenge_Rejection_Given_`.

### M9 — Severity: nit — Status: fixed
- File: tests/analyzers/timewarp-architecture-analyzers-tests/endpoint-authorize-policy-agreement-edge-analyzer-tests.cs:11
- Description: The class name `Should_Enforce_Policy_Agreement_Without_Policy_Registration` does not say what the file holds (edge cases). `Given_ClientOnly_IsClean` registers a policy.
- Suggestion: Rename the class to `Should_Enforce_Policy_Agreement_Edge_Cases`.
- Source: general
- Disposition notes: Fixed. The class is renamed to `Should_Enforce_Policy_Agreement_Edge_Cases`. Nothing else referenced it.

## Duplicates / conflicts

- The area helpers raised the stale `AskSignIn_Given_Wasm` and `principals-authorization-tests` references separately. They are collapsed into M5.
- The reviewer dropped these helper notes as not worth a fix:
  - Duplicate `[TestTag]` on partial parts. It is harmless, since `AllowMultiple` is set and the filter uses `Any`.
  - The two `ask-surface-*-asserts.cs` files `using static` each other. This is a judgment call.
  - `SurfaceJsRuntime` sits outside `catalog-agent-test-doubles.cs`. This is placement polish.
  - The `_Exemptions` vs `_With_Exemptions` suffix. This is optional.
  - The protected-page-deep-link "settings" file name. This is a judgment call.
