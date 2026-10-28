# Fix OpenAPI servers URL (localhost leak)

## Description

Exposed OpenAPI at https://arch.timewarp.work/openapi/v1.json declares
`"servers": [{"url": "http://localhost:63621/"}]`.
Public consumers and codegen get a localhost base URL.

## Checklist

- [ ] Set servers URL to https://arch.timewarp.work (or make it environment-derived)
- [ ] Verify /openapi/v1.json shows the public origin
- [ ] Check Scalar UI base URL too

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)

## Notes

Evidence: fetched 2026-10-28, openapi 3.1.1, title
TimeWarp.Architecture Web.Server API v1, 60 paths.
