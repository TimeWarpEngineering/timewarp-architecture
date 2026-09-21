# Round 1 — general
**Date:** 2026-09-21
**Scope reviewed:** branch task/120-add-runtime-smoke-coverage-for-standalone-yarp-gen vs origin/master (product files; kanban Results already present)

## Summary

Adds the missing request-level smoke for standalone YARP’s generated `WebServerApiRoutePrefixes` path (107 G3): `CreateWebYarpAsync` boots Web + YARP in-proc, pins the Development `Web.Server` cluster onto http `:7001` via `YarpWebClusterHttpAddressFilter`, and asserts Hello / identity / Roles-401 / foreign-Host through the gateway. Template excludes, slnx `#if (yarp)`/`#if (web)`, suite `global.json` SDK pin, and fixed-port docs (`web-http=7001`) line up with the factory guards; `CreateWebApiYarpAsync` still compiles with optional Api. Overall risk is low — deliberate SSOT choice to bind `:7001` on every Web in-proc host, pre-existing unread Web/Api fields remain suppressed, coverage matches the brief.

## Issues
