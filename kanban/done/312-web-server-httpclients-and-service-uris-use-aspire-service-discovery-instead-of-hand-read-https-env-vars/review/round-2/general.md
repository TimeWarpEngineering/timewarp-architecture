# Round 2 — general
**Date:** 2026-10-12

## Summary

I re-reviewed fix commit 140ab824f against round-1 findings M1–M7. M4 was skipped because the orchestrator marked it wontfix. M1, M2, M3, M5, M6 and M7 are all fixed. I found no new defects in the fix delta.

These targeted tests pass:
- `Copies_` in web-server-integration-tests: 9/9.
- `Channel_` in web-spa-integration-tests: 3/3.
- `DevelopmentConfig_` in yarp-integration-tests: 1/1.

I did not run `dev build` or `dev template-smoke`.

## Prior findings

| ID | Status | Evidence |
|----|--------|----------|
| M1 | fixed | `ForwardPublicOrigin` (identity-session-cookie-forwarding-server.cs:84) now takes `HttpAppBaseUrlAccessor.GetRequestOrigin`, the same policy that mail permalinks and LinkPreview read. It sets `X-Forwarded-Host = origin.Authority` (port kept) and `X-Forwarded-Proto = origin.Scheme`. Each is guarded by its own `Contains` check, so neither is double-set or overwritten. Test `Self_Call_Headers_Give_Rp_Id_Without_Port_And_Mail_Origin_With_Port` replays the forwarded headers into a second request. On that request `GetPublicHost` returns `arch.timewarp.work` and `GetRequestOrigin` returns `https://arch.timewarp.work:8443/`. That is the M1 regression for both run mode and published. The false "only reader" claim is corrected in http-request-host-accessor-server.cs, Design point 3, which now names three readers. The handler's Design region and the program.cs Design note are also updated. |
| M2 | fixed | New superhero-grpc-service-provider-tests.cs adds three tests. The WASM-shape test checks that the `/grpc/` ingress prefix is kept on the rebased URI. The server-shape test checks that the captured URI still has scheme `https+http` and host `grpc-server`, which proves the rebasing sits outside the discovery pipeline. It also checks `Version20` with `RequestVersionExact`. The third test checks that an unregistered client throws `InvalidOperationException` naming `grpc-server`. All three run host-free with a capturing primary handler. |
| M3 | fixed | New grpc-route-ingress-path-tests.cs loads yarp `appsettings.Development.json` through the configuration JSON provider. It asserts that `Match:Path == $"{IngressPaths.GrpcPathPrefix}/{{**catch-all}}"` and that the only `PathRemovePrefix` is `IngressPaths.GrpcPathPrefix`. The ingress-paths.cs Design comment now names this test as the agreement check rather than claiming the values "cannot drift". |
| M5 | fixed | Every site listed in round-1 general Issue 5 now says "self-call". The remaining "loopback" mentions refer to real loopback: TrustedProxy, Caddy, the dev cert bypass, the localhost RP-ID note in web-authn-options-application.cs, and "not loopback" in the host-accessor Design. One test comment, webauthn-authentication-tests.cs:126 ("InteractiveServer loopback Host is localhost"), was not in the M5 list. It describes the task 212 run-mode scenario, where the self-call host really is localhost, so it is accurate. |
| M6 | fixed | web-server.csproj:18 is now `$(DefineConstants);postgres;grpc`. Nothing under source/container-apps/web uses `#if DEBUG` or `#if TRACE`, so re-adding DEBUG/TRACE changes no compiled code. web-spa.csproj:11 (`grpc;api;web`) still overwrites the same way, but that line predates this task and this branch does not touch it. It is out of scope and not raised. |
| M7 | fixed (documented) | The program.cs Design note now explains three things. With no discovery config, the pass-through resolver sends the request to `https://web-server` over DNS. TLS name validation is the guard, and an internal CA that issues that name would defeat it. An explicit http override sends the cookie in cleartext. Documenting the behaviour was one of the remedies M7 offered. |
| M4 | wontfix (orchestrator) | Not re-reviewed. |

## New issues

None.

Checks run on the fix delta:

- **Header double-set or overwrite:** `X-Forwarded-Host` and `X-Forwarded-Proto` each have their own `Contains` guard. Two tests cover this: `Public_Proto_Already_Set_Is_Not_Overwritten` and the existing host-already-set test. HTTP Host stays unset, so TLS SNI still validates the discovered host.
- **X-Forwarded-Proto changing Request.Scheme on the self-call:**
  - In run mode, Web.Server has no forwarded-headers middleware, so the scheme does not change.
  - In published mode, `ASPNETCORE_FORWARDEDHEADERS_ENABLED` processes For and Proto with known proxies cleared. A proto with no X-Forwarded-For is still applied, so Request.Scheme becomes the public scheme. The middleware then consumes the header. `GetRequestOrigin` falls back to `Request.Scheme`, which is already the public scheme, so the result is the same.
  - Cookie Secure flags are unaffected because the identity-session cookie and the Entra correlation/nonce cookies use `CookieSecurePolicy.Always`. In any case, Set-Cookie from the self-call goes to HttpClient, not to the browser.
  - Web.Server does not call `UseHttpsRedirection`, so no redirect can be triggered.
  - The passkey origin check (`WebAuthnOrigin.IsAllowed`) compares clientDataJSON's origin against configured `AllowedOrigins` or the RP id. It never reads `Request.Scheme`, so it is unaffected.
  - The self-call now sees the same scheme as the ingress → web-server circuit request, which is the intended symmetry.
- **template.json `!grpc` excludes:**
  - `tests/container-apps/web/web-spa-integration-tests/features/superhero/**` matches the new test's directory, which holds only that file.
  - `tests/container-apps/yarp/yarp-integration-tests/grpc-route-ingress-path-tests.cs` matches the new file exactly.
  - The yarp `GrpcRoute` JSON block sits inside `//#if grpc ... //#endif`, so excluding the test under `!grpc` (with yarp on) is correct.
  - Under `!yarp`, `tests/container-apps/yarp/**` already covers the test.
- **Test correctness:** All new and changed tests pass. The assertions check the regressions they claim to: the port-kept authority, the proto value, the `https+http` scheme proving the pipeline order, the HTTP/2 exact policy, and the literal/constant agreement. In `Self_Call_Headers_...` the circuit context leaves Scheme at its default. That is harmless because the forwarded proto takes precedence in `GetRequestOrigin`.
