# Round 1 — general
**Date:** 2026-10-16
**Scope reviewed:** commit 158f3b147 (AppHost program.cs, service-defaults extensions.cs, yarp appsettings.Development.json, HealthEndpointPaths / IngressPaths, aspire-tests and yarp-integration-tests additions)

## Summary

The ingress probe now targets `IngressPaths.WebServerAlivePath` (`/web-server/alive`), routed by a
literal YARP route (run-mode JSON `WebAliveRoute`, publish-mode `AddRoute(...).WithTransformPathSet`)
to web-server's `/alive`. `/alive` is mapped in every environment, anonymous, live-tagged checks only,
default writer (status text only). `/health` stays Development-only. All edits sit inside existing
`#if web` / `//#if (web)` regions. The new yarp test is excluded in `!web`/`!yarp` by the existing
`tests/container-apps/yarp/**` template excludes, and the aspire model test tolerates flags at runtime.
Two literals (JSON route and C# constant) are pinned by an agreement test, which follows the repo's
"two things must agree" rule.

Checked against the Goal restatement and against the original task: the restatement matches the task
(probe through YARP to web-server, no prerender, ingress-to-web-server proof kept). The Results cover
all 7 requirements, including the publish-output investigation (WithHttpHealthCheck emits no
EndpointProbeAnnotation, so the publish suites have no deployed probe to assert; the publish models are
asserted at the model level instead).

Re-verified independently: `./bin/dev build` succeeded; yarp-integration-tests 17/17; aspire-tests 62/62
(including IngressHealthCheck_Given_ ×4 and WebServerAliveThroughIngress_Should_ReturnHealthy).

## Issues

None.
