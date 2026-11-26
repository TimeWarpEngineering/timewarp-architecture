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

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass

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

## Session

- Created: 2227132 (2026-11-24)
