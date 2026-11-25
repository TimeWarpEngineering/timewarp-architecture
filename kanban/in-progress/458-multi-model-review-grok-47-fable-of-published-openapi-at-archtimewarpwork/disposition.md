# Disposition — task 458, published OpenAPI review (round 1)

- **Date:** 2026-11-24
- **Inputs:** `review/findings.md` (merged and verified), `review/round-1/grok-4.7.md`,
  `review/round-1/fable.md`
- **Decided by:** the implement oracle under `ganda task work 458`. Steven may overturn any row.
  Each follow-on task's spec says what it covers; implementation happens on that task, not here.
- **Outcome:** 16 accepted (M-01 resolved by operator action and task 524; 11 follow-on tasks,
  458-001…458-011, cover M-02…M-16), 1 rejected (M-17), 1 linter note recorded. No finding deferred.

| ID | Severity | Decision | Follow-on task |
|----|----------|----------|----------------|
| M-01 | blocker | **Accept** — resolved: operator stopped the host and rotated credentials; route removed by task 524 (PR #690). No duplicate task filed. | [524](../../done/524-remove-api-debug-config-endpoint-from-common-server-module/task.md) |
| M-02 | major | **Accept** | [458-001](../../to-do/458-001-publish-session-cookie-and-per-operation-security-requirements-in-open-api/task.md) |
| M-03 | major | **Accept** | [458-002](../../to-do/458-002-publish-typed-ids-as-string-uuid-in-open-api-schemas/task.md) |
| M-04 | major | **Accept** | [458-003](../../to-do/458-003-document-feedback-attachment-upload-and-download-as-binary-in-open-api/task.md) |
| M-05 | major | **Accept** | [458-004](../../to-do/458-004-one-problem-details-shape-on-the-wire-and-in-open-api-including-validation-400/task.md) |
| M-06 | major | **Accept** | [458-005](../../to-do/458-005-open-api-operation-id-summary-and-descriptions-from-the-operation-attribute/task.md) |
| M-07 | major | **Accept** | [458-006](../../to-do/458-006-publish-required-request-members-from-validator-rules-in-open-api/task.md) |
| M-08 | major | **Accept** (document or exclude; the task decides per route) | [458-007](../../to-do/458-007-document-or-exclude-browser-navigation-routes-and-liveness-in-open-api/task.md) |
| M-09 | major | **Accept** | [458-008](../../to-do/458-008-stop-publishing-ignored-user-id-and-o-data-query-parameters-on-admin-queries/task.md) |
| M-10 | major | **Accept** | [458-009](../../to-do/458-009-complete-payment-documentation-for-the-tip-alias-and-payment-headers-in-open-api/task.md) |
| M-11 | minor | **Accept** (with M-08) | [458-007](../../to-do/458-007-document-or-exclude-browser-navigation-routes-and-liveness-in-open-api/task.md) |
| M-12 | minor | **Accept** (with M-10) | [458-009](../../to-do/458-009-complete-payment-documentation-for-the-tip-alias-and-payment-headers-in-open-api/task.md) |
| M-13 | minor | **Accept** (breaking route renames; its own task) | [458-010](../../to-do/458-010-one-route-grammar-for-api-routes-enforced-by-an-analyzer/task.md) |
| M-14 | minor | **Accept** | [458-011](../../to-do/458-011-open-api-root-tags-contact-license-and-server-url-without-trailing-slash/task.md) |
| M-15 | minor | **Accept** (with M-14) | [458-011](../../to-do/458-011-open-api-root-tags-contact-license-and-server-url-without-trailing-slash/task.md) |
| M-16 | minor | **Accept** (with M-06) | [458-005](../../to-do/458-005-open-api-operation-id-summary-and-descriptions-from-the-operation-attribute/task.md) |
| M-17 | nit | **Reject** — one schema per contract is the contract-per-endpoint design (`tw-web-api-contracts`); the reviewer proposed no fix. Schema-name prefix stripping is optional scope in 458-005. | — |
| L-01 | — | **Record** — Spectral `oas3-operation-security-defined` and Redocly `no-ambiguous-paths` are false positives for this document; Redocly `security-defined` clears with 458-001. | — |

## Grouping

Follow-on tasks are grouped by the owner that has to change, so one change fixes each group:

- security transformer: 458-001
- schema pipeline: 458-002 (typed ids), 458-006 (`required`)
- binary bodies: 458-003
- problem details and FastEndpoints error writer: 458-004
- source generator metadata: 458-005
- discovery coverage and hand-written browser endpoints: 458-007
- contracts: 458-008, 458-010
- payment transformers: 458-009
- document-level metadata: 458-011

No dependency order is required between them. 458-010 renames routes; 458-007 coordinates the
sign-out routes with it.

## M-01 (resolved)

M-01 was about the live host, not the document. While it was open, the reviewer's text stayed out
of this public repository. It is resolved:

- **Operator (2026-11-25):** the Development host behind `arch.timewarp.work` was stopped (the
  route answers `502`, checked again on resume with one status-only `GET`), and every exposed
  rotatable credential was rotated.
- **Structural fix:** task 524 (PR #690, merged on `master`) removed `/api/debug-config` from
  `CommonServerModule.ConfigureEndpoints` in every environment and added the `Development`-host
  regression test `common-server-module-endpoints-tests.cs` asserting `404 Not Found`. That covers
  the follow-on this disposition promised, so no second task was filed.
- **Restored:** the reviewer's F-001 text in `review/round-1/fable.md`, the G-009 sentence in
  `review/round-1/grok-4.7.md`, and M-01 in `review/findings.md`. None of them holds a credential
  value; F-001 names configuration keys only.
