# Fix DELETE Roles roleId required requestBody

## Description

`DELETE /api/Roles/{roleId}` declares `requestBody.required: true` with `*/*`
+ `application/json` DeleteRole_Command. DELETE-with-required-body is
unusual, breaks many clients/codegen, and no other DELETE in the spec does
this.

## Checklist

- [ ] Confirm whether the command body is actually required
- [ ] If not, make it optional or drop it (roleId path param suffices)
- [ ] If required, document why and give an example body

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
