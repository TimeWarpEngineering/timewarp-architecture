# Replace OneOf wrapper schemas with clean oneOf

## Description

71 of 238 schemas are `OneOfOneOfOf...` wrappers leaking FastEndpoints
internals (`value/index/isT0/isT1/asT0/asT1`). Works but noisy for codegen
and agent clients.

## Checklist

- [ ] Emit clean `oneOf: [Response, SharedProblemDetails]` (+ problem+json content type where apt) instead of wrapper objects
- [ ] Verify Scalar + generated clients still work
- [ ] Wrapper count goes toward zero

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 71 OneOf wrappers of
238 schemas, 2026-10-28.
