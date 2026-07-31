# Pin distinct Aspire AppHost ports so TWA can run beside Crunchit

## Description

TWA and Crunchit both inherited the Aspire starter template AppHost control-plane
ports (`applicationUrl` 17204/15117, OTLP 21030, resource service 22024). Running
`dev run` while Crunchit is up fails with `address already in use` on 22024.

Give TWA a stable, distinct port block so both AppHosts can run side by side.
Leave Crunchit on the original template ports (already running / product repo).

## Requirements

- TWA AppHost dashboard + resource-service + OTLP ports must not collide with
  Crunchit's current launchSettings block
- Service/ingress ports (TWA `636xx`, Crunchit `5280`) already differ — do not
  reassign those unless a collision is found
- Keep ports fixed (not `--isolated` randomization) so dashboard URLs stay stable
  for docs, WSL/Caddy, and E2E

## Checklist

- [x] Confirm collision is AppHost control plane (22024), not ingress/services
- [x] Assign TWA-only port block in AppHost `launchSettings.json` (+100 offset)
- [x] Leave Crunchit ports unchanged
- [x] Verify `aspire run --detach` starts while Crunchit still holds 22024
      (Dashboard https://localhost:17304; smoke AppHost stopped after verify)
- [x] Commit + mark done

## Notes

### Collision (2026-07-30)

- Holder: Crunchit `aspire-app-host` PID, `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL=https://localhost:22024`
- Identical blocks in both repos' AppHost `Properties/launchSettings.json`

### Port map

| Role | Crunchit (unchanged) | TWA (this task) |
|------|----------------------|-----------------|
| Dashboard HTTPS | 17204 | **17304** |
| Dashboard HTTP | 15117 | **15217** |
| OTLP HTTPS | 21030 | **21130** |
| Resource service HTTPS | 22024 | **22124** |
| OTLP HTTP | 19036 | **19136** |
| Resource service HTTP | 20267 | **20367** |
| App ingress | 5280 | 63610/63620 (already distinct) |

Offset is +100 on every AppHost control-plane port from the shared template defaults.

## Session

- Implementation: current session (2026-07-30)

## Results

- Changed TWA AppHost `Properties/launchSettings.json` control-plane ports by +100
  vs the shared Aspire template / Crunchit block (dashboard **17304**, resource **22124**,
  OTLP **21130**, etc.).
- Verified: with Crunchit still bound to 17204/22024/21030, `aspire run --detach` for TWA
  started successfully and reported Dashboard `https://localhost:17304/...`.
- Smoke AppHost stopped after verify; Crunchit left running.
- Service/ingress ports (`636xx`) already distinct from Crunchit (`5280`) — unchanged.
