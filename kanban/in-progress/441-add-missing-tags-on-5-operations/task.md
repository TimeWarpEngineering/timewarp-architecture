# Add missing tags on 5 operations

## Description

These ops have no `tags` (breaks Scalar grouping/codegen):
`POST /api/Feedback/attachments`,
`GET /api/Feedback/attachments/{attachmentId}`,
`GET /api/identity/entra/challenge`, `POST /identity/sign-out`,
`GET /identity/sign-out/antiforgery-token`.

## Checklist

- [ ] Add tags (Feedback, Identity, or appropriate group)
- [ ] Verify Scalar groups render
- [ ] Check for other tag gaps

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
