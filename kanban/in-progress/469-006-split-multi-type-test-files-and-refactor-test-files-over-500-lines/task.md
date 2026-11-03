# Split multi-type test files and refactor test files over 500 lines

## Parent

469

## Description

Most over-limit files are tests. Two patterns: contract or serialization test files that
bundle many per-type test classes, and single large test classes covering many scenarios.
Split the first by type, one test class per file. Split the second by scenario or feature
area, and pull repeated arrange code into shared fixtures or builders where a file only got
long through duplication.

## Files (lines, top-level types)

Multi-type:
- tests/container-apps/web/web-contracts-tests/features/identity/identity-contracts-serialization-tests.cs (770, 19)
- tests/common/timewarp-testing/principal-store-contract-tests.cs (884, 13)
- tests/tools/dev-cli-tests/deploy-operate-tests.cs (907, 9)
- tests/tools/dev-cli-tests/aspire-deploy-tests.cs (972, 7)
- tests/container-apps/web/web-server-integration-tests/features/identity/entra-challenge-tests.cs (646, 3)
- tests/analyzers/timewarp-architecture-sourcegenerator-tests/fast-endpoint-source-generator-tests.cs (507, 2)

Single large class:
- tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs (1836)
- tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-tests.cs (970)
- tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-playwright-tests.cs (960)
- tests/analyzers/timewarp-architecture-sourcegenerator-tests/page-source-generator-tests.cs (842)
- tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs (781)
- tests/container-apps/web/web-server-integration-tests/features/identity/entra-ticket-processor-tests.cs (775)
- tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs (760)
- tests/container-apps/web/web-server-integration-tests/features/agent-chat/xai-chat-upstream-tests.cs (715)
- tests/container-apps/web/web-spa-integration-tests/features/application/command-palette-tests.cs (644)
- tests/analyzers/timewarp-architecture-analyzers-tests/type-stem-identifier-analyzer-tests.cs (633)
- tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs (625)
- tests/analyzers/timewarp-architecture-analyzers-tests/page-local-message-bar-analyzer-tests.cs (609)
- tests/container-apps/web/web-spa-integration-tests/features/application/navigation-destinations-tests.cs (601)
- tests/container-apps/web/web-server-integration-tests/features/feedback/admin-feedback-authorization-tests.cs (597)
- tests/analyzers/timewarp-architecture-analyzers-tests/component-side-effect-analyzer-tests.cs (587)
- tests/analyzers/timewarp-architecture-analyzers-tests/endpoint-authorize-policy-agreement-analyzer-tests.cs (579)
- tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs (573)
- tests/analyzers/timewarp-architecture-sourcegenerator-tests/ingress-route-prefix-generator-tests.cs (566)
- tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-parity-tests.cs (557)
- tests/container-apps/web/web-spa-integration-tests/features/application/ask-tag-tests.cs (557)
- tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs (554)
- tests/container-apps/web/web-spa-integration-tests/features/design-system/design-system-page-contract-tests.cs (515)
- tests/container-apps/aspire/aspire-tests/kubernetes-publish-tests.cs (512)
- tests/container-apps/web/web-spa-integration-tests/components/time-warp-page-render-tests.cs (503)
- tests/container-apps/web/web-server-integration-tests/features/admin/principals/principals-authorization-tests.cs (502)

## Requirements

- Each file 500 lines or fewer. Test count and assertions unchanged (or more, never fewer).
- Jaribu and Shouldly only. Shared fixtures follow tw-jaribu and the host-lane conventions.
- A file split by scenario gets a name that says which scenario it holds.

## Notes

This child is the largest by file count and may be worked in several PRs, one test project
at a time, under this same task id.

## Goal (implementer restatement)

Every test `.cs` file listed below ends at 500 lines or fewer, with no test or assertion lost.
Multi-type files become one top-level type per file. Large single-class files are split by
scenario into files whose names say which scenario they hold. Shared arrange code moves into one
fixture per suite instead of being copied. Namespaces, test names, host lifetimes (C-create /
C-share), `#if flag` regions and Purpose regions are kept. Acceptance: no tracked test `.cs` over
500 lines; per-project test-method and Shouldly counts at or above baseline; `dev build` 0/0;
fast, integration and playwright groups green; `ganda repo audit` passes.

## Checklist

- [x] Multi-type files split one class per file
- [x] web-spa-integration-tests split by scenario
- [x] web-spa-playwright-tests split by scenario
- [x] web-server-integration-tests split by scenario
- [x] analyzer and sourcegenerator tests split by scenario
- [x] dev-cli, aspire, and timewarp-testing tests split
- [x] All test suites green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)

- Implement (2026-11-04): all 35 listed files split in one PR (seven helper agents, one per test project, then a verify pass).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-sonnet-5-5), session 7834b05d-fdfa-41ef-ae9b-b70f76a89a95, max-turns 500, Done — 2026-11-03T23:29:45Z

## Results

All 35 files are split. No tracked test `.cs` is over 500 lines; the largest new file is
450 lines. The only `.cs` files still over 500 are EF migration snapshots and a `kanban/done`
research script, which are not tests and are outside this task.

Counts from a static scan (test methods / Shouldly asserts, baseline then after):

| Project | Baseline | After |
|---|---|---|
| web-spa-integration-tests | 621 / 3044 | 621 / 3044 |
| web-spa-playwright-tests | 49 / 395 | 49 / 395 |
| web-server-integration-tests | 582 / 1674 | 610 / 1674 (the +28 are fixture helper methods, not tests) |
| analyzers-tests | 350 / 36 | 350 / 36 |
| sourcegenerator-tests | 156 / 594 | 156 / 594 |
| dev-cli-tests | 343 / 993 | 343 / 993 |
| aspire-tests | 93 / 269 | 93 / 269 |
| timewarp-testing | 45 / 151 | 45 / 151 |
| web-contracts-tests | 49 / 165 | 49 / 164 (the "missing" one was the text `Should.Never` inside a Design comment) |

How each group was split:

- **Multi-type files:** one type per file. The 19 identity contract classes are now
  `features/identity/<type>-should.cs`. The 13 abstract principal-store contract classes moved to
  `tests/common/timewarp-testing/principal-store-contract-tests/`, plus `i-principal-store-factory.cs`.
  dev-cli is now `aspire-deploy-<type>-given-tests.cs` and `deploy-operate-<type>-given-tests.cs`.
  Entra challenge became three scenario classes, a fixture, and the two fakes.
- **web-spa-integration-tests:** split into `partial` files by scenario plus a `*-test-fixture.cs`.
  These classes share static host fields and nested doubles, and `CatalogAgent_Should` was already
  partial, so class names, registrations and lifetimes are unchanged.
- **web-spa-playwright-tests:** ask-sign-in became three scenario classes (reprobe,
  rename-navigate, search-focus) plus `AskSignInTestFixture`. Feedback-attachment became the form
  class and an admin class plus a fixture. ask-surface and drawer each hold one test, so their
  assert helpers moved into `*-asserts.cs` classes. Sign-in screenshots now go to
  `playwright-screenshots/AskSignInReprobe_Given_Wasm/`.
- **web-server-integration-tests:** split into scenario classes plus fixtures. Admin-feedback,
  protected-page-deep-link and principals-authorization share one host through the new
  `HostGraphLease` (`features/identity/infrastructure/host-graph-lease.cs`). The first class to
  acquire it builds the host and the last to release it disposes the host.
- **analyzer and sourcegenerator tests:** scenario classes plus an `internal static` fixture each.
  The sourcegenerator files gained Purpose regions (boy-scout: the originals had none).
- **aspire-tests:** `AcaPublish_Given_` and `KubernetesPublish_Given_` stay single classes split
  into partial files (`*-secrets-tests.cs`, `aca-publish-bicep-parsing.cs`,
  `kubernetes-publish-chart-parsing.cs`). That keeps one publish per run and the class names that
  `dev publish aca|kubernetes` filter on. `CombinedFilter_Should_SelectExactlyThePublishSafetySuites`
  scans source for `class X`, so it now applies `.Distinct()`: a partial class declared in several
  files is still one class. The exact-set assertion is unchanged.
- Comments that named the old file names in `source/` and sibling tests were updated.

Gates (all foreground, this worktree):

- `./bin/dev build`: succeeded (Release, warnings are errors).
- `./bin/dev test -g fast --no-build`: 14 of 14 projects passed (dev-cli-tests 342/342).
- `./bin/dev test -g integration --no-build`: 8 of 8 projects passed.
- `./bin/dev test -g playwright --no-build`: 1 of 1 project passed (33/33).
- `ganda repo audit`: passes. One advisory kanban-landed warning remains: live claims on other
  tasks (070-001, 104-024..026, 123, 141, 198, 244, 454) have no worktree. Those are not this
  task's claims, so they are not fixed here.

### How to validate

Smoke:

```bash
for f in $(git ls-files 'tests/**/*.cs'); do n=$(wc -l < $f); [ $n -gt 500 ] && echo "$n $f"; done
./bin/dev build && ./bin/dev test --no-build
```

Expect: the loop prints nothing. The build succeeds with 0 warnings, and every fast, integration
and playwright project passes, with dev-cli-tests at 342 and web-spa-playwright-tests at 33 tests.
