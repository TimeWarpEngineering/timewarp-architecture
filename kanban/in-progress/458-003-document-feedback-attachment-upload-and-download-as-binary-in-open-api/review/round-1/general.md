# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/458-003-document-feedback-attachment-upload-and-download` vs `master` (merge-base `ae39ecfaf`). `open-api-binary-content.cs`, `file-content-metadata.cs`, `open-api-problem-details.cs` visibility, `common-server-module.cs` registration, upload and download endpoint metadata, `open-api-binary-content-tests.cs`. Checked against the task requirements and `## Goal (implementer restatement)`.

## Summary

`OpenApiBinaryContent` runs after `OpenApiOneOf`. An `IFileUploadRequest` operation gets one `type: string, format: binary` body per accepts media type, a required `X-File-Name` header, and a 413 `application/problem+json` response that points at `SharedProblemDetails`. A success arm with a `Stream` property loses its JSON 2xx content and gains one binary media type per `FileContentMetadata.ContentTypes` plus a required `Content-Disposition` header. Detection uses the contract markers. The feedback endpoints supply the allow-list and the limits from `FeedbackAttachmentRules`. A document transformer drops Stream-carrying components that no operation still references.

The recorded gates cover the change: `./bin/dev build` passed with 0 warnings; the new document tests passed 6/6; problem-details, one-of, tags, security, and the upload endpoint tests passed. `ganda repo audit` passed with the existing vscode-window-icon advisory. Those gates were not re-run.

The restatement matches the task. "States the file name limit" is the task's "with its max length": the header is percent-encoded and `FeedbackAttachmentRules.MaxFileNameLength` applies after decoding, so the limit is the description "At most 200 characters after decoding" (`open-api-binary-content.cs` around the `X-File-Name` parameter) rather than schema `maxLength`, which would reject a legal name whose encoding is longer than 200. Narrowing "no application/json entry" to the 2xx success arm matches the wire: failures stay `application/problem+json`. Upload media types stay on the accepts metadata (the host's 415 check); download media types and both limits ride on `FileContentMetadata`, which is the grooming note's split. The upload summary stays with task 458-005. No misread.

`X-Content-Type-Options` and `Cache-Control` stay off the document. The requirements name `Content-Disposition` as the response header to publish.

## Issues

<!-- None. -->
