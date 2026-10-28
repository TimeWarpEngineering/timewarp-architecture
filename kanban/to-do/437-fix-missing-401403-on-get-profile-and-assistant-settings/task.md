# Fix missing 401/403 on GET profile and assistant settings

## Description

`GET /api/Users/Current/Profile` documents only 200 while `PUT` documents
401+403. Same for `GET /api/settings/assistant` (only 200) vs `PUT`
(401+403). Either the GETs are intentionally anonymous (surprising for
Current/Profile) or the spec is missing responses.

## Checklist

- [ ] Decide anon-vs-authed for both GETs
- [ ] Add 401/403 responses if authed, or document anonymous intent if not
- [ ] Audit remaining GETs without 401 (Hello, entra/challenge, entra/choice, session, sign-in-methods, tip, antiforgery-token)

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
