# Round 2 — general
**Date:** 2026-11-04
**Scope reviewed:** the fix delta `a91c4721f..b5027d0be` (commit `b5027d0be`, "address 469-005
review round 1 findings"), excluding `kanban/`. 10 product files, +45 / -45. Effort 2, roster
general.

## Summary

The fix commit addresses the four round-1 findings and nothing else. One test moves between two
runfiles, the triage `Filed` helper and the xAI test GUID are each written once, and four Design
notes name the Entra types that now hold each rule. No product code changed, only test code and
comments. No new defects found.

## Re-verification of round-1 findings

- **M1 (fixed).** `Upload_Should_SucceedWhenAnExpiredBlobCannotBeDeleted` is now in
  `feedback-attachment-tests.cs:272`, next to the expiry test. The body is unchanged. The
  `feedback-attachment-tests.cs` Purpose region names that test ("an expiry that survives a blob
  delete that throws"). The `feedback-attachment-submit-tests.cs` Purpose region does not, and
  that file no longer has the test. `UndeletableBlobStore` and `UploadBlobs` live in the shared
  `feedback-attachment-world-tests.cs`, so the moved test compiles standalone. Standalone counts
  are 14 + 8 = 22, the same total as before.
- **M2 (fixed).** `TriageWorld.Filed(owner, filedAt, kind)` at `feedback-triage-world-tests.cs:95`
  is the only place that builds the item text. `TriageWorld.AddAsync` and all nine call sites in
  `feedback-triage-tests.cs` call it, and `At` is passed where the old default used it. The test
  file's `Filed` and its now-unused `DomainKind` alias are removed. A grep finds no other caller of
  the old `Filed`.
- **M3 (fixed).** `XaiKeyTestHarness.UserA` (`user-xai-api-key-harness-tests.cs:51`) holds the
  GUID and is the site-key setter. `user-xai-api-key-tests.cs:38` reads it. The GUID string is
  written once.
- **M4 (fixed).** `credential-rules-application.cs`, `get-credentials-contracts.cs`, and
  `get-credentials-handler-application.cs` now name `EntraAccountLinker` for HoldsMicrosoft365.
  `credential-usage-recorder.cs` now names `EntraSignInStamp` as the RecordAsync caller and
  lists the callers of the stamp. I checked that against the code: `EntraTicketProcessor` calls
  `Stamp.RecordSignInAsync` at `entra-ticket-processor-application.cs:140`, `:208`, and `:257`,
  and `Stamp.RecordNewAsync` at `:264` (sync hit, already-linked bootstrap, and bootstrap create).
  `EntraAccountLinker` calls them at `entra-account-linker-application.cs:184` and `:229` (merge,
  link, and attach). The note matches the code. The other remaining `EntraTicketProcessor`
  mentions (`entra-issuer-validator-server.cs:10` IssuerMatchesTenant and
  `entra-account-linker-application.cs:7`) are still correct, because the issuer check and the
  mode dispatch stayed in the processor.

## Issues

None.

## Goal check

The implementer restatement matches the task: the six files, 500 lines or fewer, behavior
unchanged, decomposition by responsibility, runfiles split by scenario with C-create / C-share
kept, and the integration tests and runfiles passing. The restatement's added criteria (one type
per file, no `partial`, helpers written once, the aggregator has no duplicate names) all tighten
the task and do not depart from it. No misread.

## Gates the reviewer ran in the claim worktree

- Standalone `dotnet run` of each changed runfile: `feedback-attachment-tests.cs` 14/14,
  `feedback-attachment-submit-tests.cs` 8/8, `feedback-triage-tests.cs` 14/14,
  `user-xai-api-key-tests.cs` 12/12.
- `dotnet test` in `tests/container-apps/web/web-jaribu-tests`: 458/458 passed. That run
  compiles every runfile together under JARIBU_MULTI.
- The over-500 line check (`git ls-files 'source/container-apps/web/*.cs'`, migrations excluded)
  printed nothing.
- I did not run the web-server integration tests again. The delta outside the runfiles is
  comments only, and round 1 ran that suite (435 passed, 1 skipped, 0 failed).
