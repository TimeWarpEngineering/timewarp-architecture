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

## Checklist

- [ ] Multi-type files split one class per file
- [ ] web-spa-integration-tests split by scenario
- [ ] web-spa-playwright-tests split by scenario
- [ ] web-server-integration-tests split by scenario
- [ ] analyzer and sourcegenerator tests split by scenario
- [ ] dev-cli, aspire, and timewarp-testing tests split
- [ ] All test suites green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
