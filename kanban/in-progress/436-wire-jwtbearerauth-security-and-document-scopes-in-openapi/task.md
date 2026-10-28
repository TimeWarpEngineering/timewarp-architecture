# Wire JWTBearerAuth security and document scopes in OpenAPI

## Description

`components.securitySchemes.JWTBearerAuth` (Bearer/JWT) is defined but no
global `security` and zero per-operation `security` entries exist across all
60 paths. Scope requirements (`identity:read`, `credential:manage`,
`demo:invoke`) live only in `auth.md`/`llms.txt`, not the spec.

## Checklist

- [ ] Add global or per-operation security referencing JWTBearerAuth
- [ ] Annotate which ops need which scopes (agent/me=identity:read, credentials*=credential:manage, metered=demo:invoke)
- [ ] Confirm Scalar shows Authorize
- [ ] Document 401/403 + WWW-Authenticate problem+json behavior

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
No per-operation security anywhere; global security absent.
