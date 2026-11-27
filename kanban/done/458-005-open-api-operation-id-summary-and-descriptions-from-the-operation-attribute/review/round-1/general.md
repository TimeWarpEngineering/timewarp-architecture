# Round 1 — general
**Date:** 2026-11-27
**Scope reviewed:** branch `task/458-005-open-api-operation-id-summary-and-descriptions` vs `master` (product commit `bc0c7a3c6`)

## Summary

The change makes `[Operation]` with a non-whitespace `Description` mandatory on hosted contracts (TWA0035 warning), writes OpenAPI `operationId` and `summary` from the generated `OperationName` and `OperationDescription` constants, and fills empty parameter and schema-property descriptions from member XML, including `I*Details` and generated route members. Hand-written endpoints (`entra.challenge`, `browser-session.sign-out`, `browser-session.get-sign-out-antiforgery-token`) and the health and `/api` alias operations use the same `<resource>.<action>` ids. The `JWTBearerAuth` guide and the four ceremony remarks quote those ids from the contract constants.

The implementer restatement matches the original task. Recorded gates cover the analyzer, the generator, the OpenAPI suites, skill lint, and repo audit. The runtime document check fails a repeated id and a backtick-quoted operation id that is not in the document. Requiring a summary and the id shape on every operation is a test of the web-server document, which the Design region and `## Results` explain: other test hosts publish probe endpoints that have no contract. That is the production document this task is about, and the recorded OpenAPI tests passed. Schema component ids are unchanged. No issues.

## Issues
