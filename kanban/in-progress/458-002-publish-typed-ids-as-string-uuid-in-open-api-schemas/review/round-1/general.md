# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/458-002-publish-typed-ids-as-string-uuid-in-open-api` vs `master`; `OpenApiTypedId`, its registration in `CommonServerModule.AddOpenApi`, and `open-api-typed-id-tests.cs`. Checked against the task requirements and `## Goal (implementer restatement)`.

## Summary

`OpenApiTypedId` rewrites each generator-stamped typed id to `type: string, format: uuid` and `Nullable<T>` of one to null-or-string with format `uuid`, and it is registered before `OpenApiOneOf` so the schema walk still sees response members. The document test rejects an untyped component schema and pins `TimeWarpIdentityPrincipalId`, `TimeWarpIdentityCredentialId`, and `SystemNullableOfTimeWarpIdentityPrincipalId`. The restatement matches the task: the Guid `Value` check is the generator's type shape, and the three pinned components are the schemas the requirements name. The published snapshot has exactly those three untyped component schemas. Recorded build, the new tests (4/4), the OneOf and problem-details suites, and `ganda repo audit` cover the change. No product defect found.

## Issues

<!-- None. -->
