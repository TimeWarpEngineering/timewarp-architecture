# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** branch vs origin/master (commit 3999e734, 21 files) plus call sites of the former port constants

## Summary

The change replaces four fixed port constants with one lazily resolved set (`InProcTestPorts`)
driven by `TIMEWARP_TEST_PORT_BASE`, aliases the old `*TestServerApplication` members onto it,
and has template-smoke tiers 2–3 run generated apps on base 17000. Risk is low: defaults are
unchanged, every listen URL and the YARP cluster rewrite go through the resolved set, and the
literal grep leaves only comments, the single default constant, host-free WebAuthn fixture
origins, and Guid fixtures. Deleted `appsettings.json` sections were verified dead (only YARP's
own program calls `AddReverseProxy`; `ServiceCollectionOptions` has no binder; `ServiceUriHelper`
reads `services__*` env vars). Smoke matrix entries run serially, so sharing 17000 within smoke
is safe. Gates re-run by the reviewer: `dev build` 0/0; yarp suite under
`TIMEWARP_TEST_PORT_BASE=17000` 4/4 with hosts on 17000/17001/18443 and proxy to 17001;
invalid value raises the teaching error; 7255-listener proof fails bare and passes on 17000.

## Issues

### Issue 1 — Severity: nit
- File: tools/dev-cli/services/template-smoke-harness.cs:716
- Description: The env var name `TIMEWARP_TEST_PORT_BASE` is a second string literal in dev-cli,
  duplicating `InProcTestPorts.EnvironmentVariableName`. Two names that must agree by memory.
- Suggestion: dev-cli cannot reference the testing library (it would pull the server graphs into
  the tool). The cheapest build-time agreement would be a harness assertion that the generated
  app's `in-proc-test-ports.cs` contains the literal; both sites already document each other.
- Status: open
