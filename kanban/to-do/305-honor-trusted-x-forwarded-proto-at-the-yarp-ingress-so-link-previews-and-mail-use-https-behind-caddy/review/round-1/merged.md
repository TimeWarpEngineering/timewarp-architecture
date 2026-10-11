# Round 1 — merged findings
**Date:** 2026-10-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/yarp/appsettings.Development.json:10
- Description: Run mode now loads ingress routes from the yarp project's Development config, which had no template conditionals. A generated app with `--api false` or `--grpc false` routed `/api/**` and `/grpc/**` to services that do not exist (502), where master's AppHost gated those routes per flag.
- Suggestion: gate the Api/Grpc/Web routes and clusters with `//#if (api)` / `(grpc)` / `(web)`.
- Source: general
- Disposition notes: Fixed. The routes and clusters are wrapped in `//#if (api|web|grpc)`. A scratch template hive confirmed the gating gives ApiRoute/GrpcRoute only (`--web false`), WebRoute/WebSwaggerRoute only (`--api false --grpc false`), and all four by default, and that all three outputs parse as configuration. Trailing commas are allowed. `dev template-smoke` SmokeNoApi output has no ApiRoute or Api.Server. A yarp program.cs Design line records why.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/yarp/trusted-proxy-forwarded-headers.cs:37
- Description: `Exists()` reports a JSON `[]` as absent. So both lists empty fell back to trusting loopback instead of skipping the middleware, which contradicts the appsettings comment, Design region, runbook and tw-deploy.
- Suggestion: detect the declared key and read an empty array as empty.
- Source: general
- Disposition notes: Fixed with `ReadList`, which finds the declared child key, rejects a scalar value and returns its elements. Design, XML docs and the appsettings comment now say "declared" rather than "absent section". New `trusted-proxy-configuration-tests.cs` (5 tests, JSON-loaded, TestServer) covers: no lists declared trusts loopback; both empty ignores the headers; network-only replaces loopback; scalar and malformed CIDR fail startup.

### M3 — Severity: suggestion — Status: fixed
- File: skills/tw-deploy/references/azure-container-apps.md:124
- Description: The docs and Results claimed ACA's external ingress keeps the client scheme on the published AddYarp ingress. This is unverified and likely false, because ACA terminates TLS and the hop to the container is http.
- Suggestion: state that task 305 does not change the published path, and name the workaround.
- Source: general
- Disposition notes: Fixed in the ACA reference, the AppHost Design region and task Results. They now say the published path may forward http and is unverified, and point to `SiteMetadata:BaseUrl` / `PublicOrigin`; mail has no override. The publish-mode scheme is out of scope for task 305, which is about the run-mode path behind Caddy.

## Duplicates / conflicts

- None.
