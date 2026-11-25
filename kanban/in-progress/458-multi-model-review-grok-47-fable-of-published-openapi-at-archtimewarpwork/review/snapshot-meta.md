# Snapshot metadata — `openapi-v1.snapshot.json`

| Field | Value |
|-------|-------|
| Source URL | https://arch.timewarp.work/openapi/v1.json |
| Fetched (UTC) | 2026-11-24T20:15:44Z (`date` header `Tue, 24 Nov 2026 20:15:44 GMT`) |
| HTTP | 200, `content-type: application/json;charset=utf-8`, `server: Kestrel`, `via: 1.1 Caddy` |
| Bytes | 267859 |
| SHA-256 | `496a050641bc45f42502794fbf6a404ca85aac1043cecb9519591bdacc1e8b98` |
| `openapi` | `3.2.0` (the task text says 3.1.1; the deployed host has moved to 3.2.0 since task creation) |
| `info.title` / `info.version` | `TimeWarp.Architecture Web.Server API v1` / `v1` |
| Paths / operations | 64 paths, 79 operations (30 GET, 32 POST, 13 PUT, 4 DELETE) |
| `components.schemas` | 175 |
| Security schemes | `JWTBearerAuth` |
| Deployed version | Not exposed by the document or `/api/health` (plain `Healthy`). The static MCP server card (`/.well-known/mcp/server-card.json`, `last-modified: Sat, 24 Oct 2026 08:37:04 GMT`) reports `serverInfo.version` `2.0.0-beta.14`. That file is static and may lag the deployed build. No commit SHA is exposed. |
| Repo `origin/master` at review time | `eb767f583` (for source cross-reference only; the deployed build may differ) |

Both reviewers read this exact file. Do not refetch it into this path.
