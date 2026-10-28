# Cover health, api-alias, and well-known discovery in OpenAPI story

## Description

`llms.txt` advertises `GET /api/health`, `GET|POST /api` (tip alias), and
`/.well-known/*` (mcp/server-card.json, agent-skills/index.json,
agent-card.json, webmcp.json). None appear in `/openapi/v1.json` (expected —
Web.Server-only doc — but agents infer not-in-spec = not-exist).

## Checklist

- [ ] Decide include-vs-exclude for each path
- [ ] Either add them to OpenAPI or add an explicit documented exclusion note + link from Scalar/llms.txt
- [ ] Verify /api/health exists and is documented somewhere machine-readable

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json (60 paths, no
health/api-alias/well-known) + https://arch.timewarp.work/llms.txt,
2026-10-28.
