# Document feedback attachment upload and download as binary in OpenAPI

## Description

`POST /api/Feedback/attachments` publishes six media types (`image/png`, `image/jpeg`, `image/gif`,
`image/webp`, `application/pdf`, `text/plain`), each carrying the JSON command
`{content, fileName, contentType}`. The endpoint reads the raw body and takes the name from the
`X-File-Name` header, which is not a parameter; its 413 is undocumented. `GET
/api/Feedback/attachments/{attachmentId}` publishes 200 as `application/json` with a response object;
the endpoint writes the raw bytes with the stored content type, `Content-Disposition`,
`X-Content-Type-Options: nosniff`, and `Cache-Control: private, no-store`. A generated client POSTs
JSON and JSON-parses an image.

## Requirements

- For `IFileUploadRequest` contracts, publish each allowed media type with `type: string,
  format: binary`, a required `X-File-Name` header parameter (with its max length), and the 413.
- For responses that carry a `Stream`, publish binary content per allowed content type with the
  `Content-Disposition` header, and no `application/json` entry.
- Detect both from the contract markers, not by route.
- Add a document test that no operation whose request or response carries a `Stream` is published
  as `application/json`. The summary `POST api/Feedback/attachments` is fixed with task 458-005.

Owner: transformer beside `source/foundation/foundation-server/open-api-one-of.cs`; hand-written upload/download endpoints.

## Goal (implementer restatement)

Make the OpenAPI document describe the two feedback file endpoints the way they behave on the wire,
so a generated client sends and reads bytes, not JSON.

Acceptance criteria:

- An operation whose request type implements `IFileUploadRequest` publishes each accepted media type
  with schema `{type: string, format: binary}`, a required `X-File-Name` header parameter that states
  the file name limit, and a 413 `application/problem+json` response.
- An operation whose response success arm carries a `Stream` publishes binary content for each allowed
  media type with a `Content-Disposition` header, and no `application/json` 2xx entry.
- Detection reads the contract markers (`IFileUploadRequest`, a `Stream` property), never the route;
  per-feature values (media types, limits) come from endpoint metadata built from `FeedbackAttachmentRules`.
- A document test fails when any endpoint whose request or response carries a `Stream` publishes JSON
  on that side. The upload summary is out of scope (458-005).

## Checklist

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Review

- Round 1: Done

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-04 (Grok G-003, Fable F-004, F-005) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.
- Grooming note: `DownloadFeedbackAttachmentEndpoint` currently registers only `WithTags("Feedback")`, so declare `FeedbackAttachmentRules.AllowedContentTypes` metadata there for a generic `foundation-server` transformer to read.

## Results

- `source/foundation/foundation-server/open-api-binary-content.cs` (new): operation transformer, registered
  after `OpenApiOneOf`. `IFileUploadRequest` request: binary schema per accepted media type (the accepts
  metadata, which is also the host's 415 check), required `X-File-Name` header (limit in the description,
  not `maxLength`, because the header is percent-encoded and the limit applies after decoding), 413
  problem via `OpenApiProblemDetails.ReferenceAsync`. An upload still accepting JSON throws, naming the
  endpoint. Stream response: each 2xx gets binary content per `FileContentMetadata.ContentTypes`
  (octet-stream fallback) and a required `Content-Disposition` header. A document transformer drops the
  components that only described a Stream (`SystemIOStream`, the upload command, the download response)
  when no operation still reaches them.
- `source/foundation/foundation-server/file-content-metadata.cs` (new): endpoint metadata record
  (media types, decoded file name limit, max bytes); foundation cannot reference the feature rules.
- Upload and download endpoints add `FileContentMetadata` from `FeedbackAttachmentRules`.
- `OpenApiProblemDetails.CollectReferences` / `CollectOperationReferences` are internal for the prune.
- Test: `tests/container-apps/web/web-server-integration-tests/open-api-binary-content-tests.cs` (new,
  6 tests): pins both feedback operations and scans every endpoint whose request or response carries a
  Stream for JSON bodies, plus no component describing a Stream.

### Gates

- `./bin/dev build`: passed, 0 warnings, 0 errors.
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-binary-content-tests.cs`: passed 6/6.
- `./bin/dev test .../open-api-problem-details-tests.cs`: passed 5/5 (covers the visibility change).
- `./bin/dev test .../open-api-one-of-tests.cs`: passed 9/9 (transformer order).
- `./bin/dev test .../open-api-operation-tags-tests.cs`: passed 2/2 (download/upload Description edits).
- `./bin/dev test .../open-api-operation-security-tests.cs`: passed 6/6.
- `./bin/dev test .../features/feedback/upload-feedback-attachment-endpoint-tests.cs`: passed 4/4 (upload endpoint runtime).
- These test files are project-hosted Jaribu classes, not standalone runfiles (no `#if !JARIBU_MULTI`
  guard), so they ran through `./bin/dev test <file>`; CI runs the full suite.
- `ganda repo audit`: passed with one advisory (vscode-window-icon: `.vscode/settings.json` missing). `ganda repo audit --fix` wrote the gitignored local file; rerun passes all checks. No tracked change.

### How to validate

Smoke: `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-binary-content-tests.cs`,
or run the app and fetch `/openapi/v1.json`.

Expect: `POST /api/feedback/attachments` requestBody has six media types, each `{type: string,
format: binary}`; parameters include required header `X-File-Name` ("At most 200 characters after
decoding"); responses include `413` with `application/problem+json` -> SharedProblemDetails.
`GET /api/feedback/attachments/{attachmentId}` 200 has the six media types as binary, no
`application/json`, and a required `Content-Disposition` header. No `SystemIOStream` component.
After deploy, re-fetch `https://arch.timewarp.work/openapi/v1.json` and confirm the same.

Review round 1 (2026-11-26), roster general, effort 2:

- Counts: bug 0 open, 0 fixed, 0 wontfix. suggestion 0. nit 0.
- Disposition: clean. Final open count 0.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- The implementer restatement matches the original requirements. No misread. No `## Learnings` entry.
- Gates were not re-run. The implementer record above covers build, the new document tests, the neighboring OpenAPI tests, the upload endpoint tests, and `ganda repo audit`.

## Session

- Created: 2227132 (2026-11-24)
- 2026-11-26 implement: binary upload/download OpenAPI transformer, metadata, document test.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session cd1fe30a-c096-4dd5-b901-1b70e43761d1, max-turns 500, Done — 2026-11-26T01:32:51Z
- Review oracle: general (grok, vendor xai, model grok-4.7), session 01a2157c-0602-7722-a2a4-1f339912d66b, max-turns 120, effort 2, clean — 2026-11-26
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a2157c-0602-7722-a2a4-1f339912d66b, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-26T01:41:31Z
