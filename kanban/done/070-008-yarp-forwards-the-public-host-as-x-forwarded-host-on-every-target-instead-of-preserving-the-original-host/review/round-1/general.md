# Round 1 — general
**Date:** 2026-10-08
**Scope reviewed:** full branch diff vs origin/master (18 files)

## Summary

Swaps YARP's original-Host transform for an X-Forwarded Set transform on every Web.Server route
(AppHost + standalone yarp, config and in-memory routes), and teaches `HttpRequestHostAccessor`
(plus the circuit-host forwarding handler, the only other host reader) to read the first
`X-Forwarded-Host` value, port stripped, falling back to `Request.Host`. The four-point security
argument from the task Decision is in the accessor's Design region, and the other regions/skill
are reconciled. Checked: the loopback circuit rule is unchanged and still checked first; an
unapproved forwarded host is not replaced by Request.Host (fail-closed); no `UseForwardedHeaders`;
api/grpc routes untouched; WebAuthn origin validation takes origins from options/clientData, not
the request host, so it is unaffected. `grep` finds no remaining `WithTransformUseOriginalHostHeader`
/ `RequestHeaderOriginalHost` in source. Re-ran gates independently: yarp-integration 6/6,
web-server-integration `GetRequestHost_Should` 13/13, `Returns_` 136/136, `Copies_` 7/7,
aspire-tests `IngressSmoke` 9/9 (incl. both forged-XFH ingress cases). Risk low.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/yarp/appsettings.Development.json:16; tests/container-apps/yarp/yarp-integration-tests/standalone-yarp-ingress-smoke-tests.cs:15
- Description: Reflowed comment lines run to ~140 columns, out of line with the surrounding ~100-column comment wrap.
- Suggestion: Re-wrap the two comment lines.
- Status: open
