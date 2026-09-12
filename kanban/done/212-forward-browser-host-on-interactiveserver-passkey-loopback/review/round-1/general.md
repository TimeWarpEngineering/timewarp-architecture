# Round 1 — general
**Date:** 2026-09-12
**Scope reviewed:** branch task/212-forward-browser-host-on-interactiveserver-passkey vs origin/master (product files)

## Summary

InteractiveServer/Auto named `WebService` loopback now copies the circuit request Host (port stripped via `HostString.Host`) through `IdentitySessionCookieForwardingHandler`, so `HttpRequestHostAccessor` / `WebAuthnRelyingPartySelection` select the YARP-preserved browser host instead of `localhost`. Authenticate verify failures stay a generic 400 body while `FailureReason` and selected RP ID are logged at Information. Risk is low: the change reuses the existing cookie-forwarding path, still rejects `X-Forwarded-Host`, and is covered by Host-copy plus ceremony OriginMismatch/match tests (verified passing).

## Issues
