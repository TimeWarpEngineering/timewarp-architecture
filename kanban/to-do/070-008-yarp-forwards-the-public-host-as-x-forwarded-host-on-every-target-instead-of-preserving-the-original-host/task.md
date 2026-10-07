# YARP forwards the public host as `X-Forwarded-Host` on every target instead of preserving the original `Host`

Child of [[070-wire-aspire-publish-for-portable-deploy-compose-kubernetes]]. **Blocks 070-007**
(PR #441, the Azure Container Apps target): rebase #441 onto this before merging it.

## Problem

Today the AppHost's YARP routes to web-server chain `WithTransformUseOriginalHostHeader(true)`
(task 104-031). The browser's public `Host` reaches web-server, and `HttpRequestHostAccessor`
uses it to select the WebAuthn RP ID from `AllowedRpIds`.

On Azure Container Apps that breaks **every** web route. App-to-app traffic goes through ACA's
internal ingress, which routes by `Host` and upgrades the hop to HTTPS
(`https://web-server.internal.<domain>`). A public `Host` matches no internal app, and the TLS name
does not match. `WithHttpsUpgrade(false)` does not help: ACA redirects the internal plain-HTTP call,
and YARP passes the redirect to the browser. (Found by the 070-007 worker; recorded in #441's
AppHost Open Questions region.)

## Decision (Steve, 2026-10-08): option 2, one rule on every target

On run, Compose, Kubernetes and ACA alike, YARP sends the **destination** host as `Host` (YARP's
default) and carries the browser's public host in **`X-Forwarded-Host`**. web-server reads the
public host from `X-Forwarded-Host`. There is no target-specific forwarding behaviour.

**This reverses part of 104-031's stance** ("no spoofable `X-Forwarded-Host` is consumed"). The
security argument that replaces it must hold, and it must be written into the Design regions:

1. **YARP overwrites, never appends.** YARP must *Set* `X-Forwarded-Host` (and drop any
   client-supplied value) on web-server routes. Prove it with a test that sends a forged
   `X-Forwarded-Host` through the ingress and checks web-server sees the real public host.
2. **Selection only, never expansion.** The forwarded host still only *selects* among the
   pre-approved `AllowedRpIds`, exactly as `Host` does today. A forged value can never mint a
   credential for an RP ID the operator did not approve. Keep the fail-closed behaviour: an absent
   or unapproved host means "host not allowed".
3. **Narrow consumption.** Read `X-Forwarded-Host` in `HttpRequestHostAccessor` only. Do **not** turn
   on global `UseForwardedHeaders`, which would also rewrite scheme and remote IP. Entra keeps
   `PublicOrigin` and its pinned Secure cookies as they are. If anything else reads the host for
   security decisions (for example `identity-session-cookie-forwarding-server.cs` and the circuit
   loopback header), reconcile it with the same rule.
4. **Posture equals today's.** In run mode web-server is directly reachable, so a client can send
   its own `X-Forwarded-Host`, just as it can send its own `Host` today. Point 2 is what makes both
   safe. State this explicitly.

## Requirements

- AppHost YARP web-server routes:
  - remove `WithTransformUseOriginalHostHeader(true)`;
  - add the transform that *sets* `X-Forwarded-Host` to the original request host, using YARP's
    forwarded-header transforms with action Set;
  - the api/grpc routes are unchanged;
  - the web hop scheme stays as is for run, Compose and Kubernetes (plain HTTP).
- Standalone `source/container-apps/yarp` project (`program.cs`, `appsettings.Development.json`): the
  same change, replacing `RequestHeaderOriginalHost`.
- `HttpRequestHostAccessor`:
  - public path: `X-Forwarded-Host` (port stripped, first value only), falling back to `Request.Host`
    when it is absent;
  - keep the `X-TimeWarp-Circuit-Host` loopback rule exactly as it is.
- Tests:
  - accessor unit cases covering forwarded, absent, forged-but-unapproved, multi-value and the
    loopback circuit header;
  - update `aspire-tests/ingress-smoke-tests.cs` (task 117) to the new shape: a foreign client `Host`
    plus a forged `X-Forwarded-Host` through the ingress, and a web route still answering with the
    correct RP-ID selection;
  - keep passkey ceremony suites green.
- Reconcile every Design region that states the old rule:
  - `aspire-app-host/program.cs` (the 104-031 block);
  - `http-request-host-accessor-server.cs`;
  - `web-authn-options-application.cs`;
  - `entra-authentication-registration-server.cs`;
  - `entra-authentication-options-application.cs`;
  - `identity-session-cookie-forwarding-server.cs`;
  - `mock-authentication-defaults.cs`;
  - the yarp project;
  - `skills/tw-deploy` (ingress topology), and any other skill text describing original-Host
    forwarding.
- Do not touch the ACA target itself; that is 070-007's PR. Once this merges, #441 is rebased and its
  Open Question (the aca web-route host strategy) is closed by deleting it.

## Checklist

- [ ] YARP (AppHost + standalone) sets `X-Forwarded-Host`, no original-Host transform on web routes
- [ ] `HttpRequestHostAccessor` reads it; selection-only, fail-closed
- [ ] Forged-header ingress test + accessor tests; ingress smoke updated
- [ ] Design regions + skills reconciled (104-031 stance replaced with the argument above)
- [ ] Gates: `dev build` 0/0, `dev test` (incl. aspire-tests ingress smoke, identity/passkey suites),
      `dev template-smoke`, `ganda repo audit`

## Notes

- Workers never start the maintainer's AppHost. aspire-tests boots its own test AppHosts, which is
  fine.
- Wording rule: the forwarded host only selects among approved RP IDs. Never describe it as trusted
  input.

## Session

- Created: 2026-10-08 (cockpit, from the 070-007 open question; Steve chose option 2)
