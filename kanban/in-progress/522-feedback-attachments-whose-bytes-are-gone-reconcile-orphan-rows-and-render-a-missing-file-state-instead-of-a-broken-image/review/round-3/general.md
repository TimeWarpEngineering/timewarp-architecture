# Round 3 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/522-feedback-attachments-whose-bytes-are-gone` vs `master` (`git diff master...HEAD`, 2127 lines), against the task requirements and `## Goal (implementer restatement)`. Gates were not re-run. The implementer's `## Results` cover build, the changed feedback and SPA tests, template-smoke (including the matrix), audit, the doc-only M1 fix, and the template-smoke runner change (`./bin/dev build`, `dev-cli-tests` 399/399, `./bin/dev template-smoke`, audit).

## Summary

The server sets `GetFeedback.Attachment.IsFilePresent` from `IFeedbackAttachmentBlobStore.ExistsAsync` on the filer read and on every admin view (`FeedbackContractMap.AttachmentsAsync` / `AdminViewAsync`). `FeedbackBody` renders `MissingFile` (file name and "file no longer stored") for a missing file in the list and in the body, and it does not put that URL in the image allow-list, so the browser does not request it. In-memory feedback stores are gone. Bytes are Azure, then Postgres, with `UnavailableFeedbackStorage` when the postgres flag is on and no connection string is set. The guide now says that. `template-smoke` runs on `ubuntu-latest` on every event, which matches the Testcontainers requirement the feedback runfiles added, and the green-key comment names that job beside `ci (integration)` as hosted-only. M1 still holds. No new findings.

## Issues

<!-- No new issues. M1 stays fixed; see merged.md. -->
