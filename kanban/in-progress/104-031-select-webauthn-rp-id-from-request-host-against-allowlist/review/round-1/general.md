# Round 1 — General review (104-031)

Reviewer: general. Commit `337527bc` + final state of touched files.
Scope per framework: options/binder-append, selection helper, handler integration, ingress
transform, hermetic test host, test quality, convention hygiene. (Adversarial angles left to the
security reviewer.)

## Findings

### G1 — Standalone yarp original-host transform never reaches passkey traffic
- severity: minor
- status: open
- file: `source/container-apps/yarp/appsettings.Development.json:16-37` (WebRoute / WebSwaggerRoute
  transforms) vs `:10-15` (ApiRoute)
- description: The `RequestHeaderOriginalHost` transform was added to `WebRoute`
  (`{**catch-all}` → Web.Server) and `WebSwaggerRoute` (`/api/web-server/**` → Web.Server). But the
  passkey ceremony endpoints are `api/identity/passkey/*` (verified in web-contracts ApiRoutes), and
  in this config `ApiRoute` (`/api/{**catch-all}` → Api.Server, **no transform**) is a more-specific
  path match than the catch-all `WebRoute`. So through the standalone `yarp` container-app, passkey
  requests route to **Api.Server** with the cluster's `Host: "localhost"` rewrite — the original
  public host is never preserved and RP-ID selection would see `localhost`. This contradicts the
  commit message, which lists "standalone yarp RequestHeaderOriginalHost" as part of the public-host
  fix; as placed, the transform does nothing for passkey selection. The AppHost path is correct
  (it explicitly carves `/api/identity/{**catch-all}` → webServer and applies the transform there);
  only the standalone-yarp config diverges.
- blast radius (why minor, not major): the active task-112 chain uses the **Aspire AppHost + Caddy**
  path, which is correct. Committed yarp routes exist **only** in `appsettings.Development.json`
  (Production/Kubernetes_Docker/base have no routes), so the standalone yarp is a dev-only ingress,
  and passkeys-on-a-public-host through it is not an exercised scenario. In dev on localhost it works
  (host rewrites to localhost, selection picks localhost).
- suggested fix: either (a) mirror the AppHost carve-outs in the standalone yarp config — add
  explicit `Web.Server` routes for `/api/identity/**` (and the other web-owned `/api/*` paths) with
  the `RequestHeaderOriginalHost` transform, ahead of `ApiRoute`; or (b) if standalone-yarp is
  intentionally dev-localhost-only and not meant to front passkeys on a public host, drop the "yarp
  covers this" framing and note the limitation in the AppHost/options Design region so it isn't read
  as a general fix.

### G2 — Ingress host-preservation has no automated coverage
- severity: minor
- status: open
- file: `tests/container-apps/web/web-server-integration-tests/Features/Identity/Passkey_HostSelection_Tests.cs`
  (+ AppHost `program.cs:129-141`, yarp `appsettings.Development.json`)
- description: The host-selection integration tests set `HttpRequestMessage.Headers.Host` and hit
  **web-server directly** (BaseAddress of the web test host) — they exercise the selection logic
  given a Host, which is correct and valuable, but they bypass YARP entirely. The actual
  original-Host-preservation wiring (AppHost `WithTransformUseOriginalHostHeader`, standalone yarp
  `RequestHeaderOriginalHost`) — the part most likely to silently regress on a framework/Aspire bump
  — has zero automated coverage and rests on the manual live-chain confirmation the plan flagged as a
  risk. Combined with G1, nothing would catch the standalone-yarp routing gap.
- suggested fix: acceptable to land as-is given the plan explicitly deferred to live-chain
  verification, but record it as a known coverage gap (and, if cheap, a yarp/SpaTestApplication-level
  test asserting the forwarded Host survives the proxy would close both G1 and G2).

### G3 — Commit summary "97/1/0" is ambiguous; confirm the middle field before Done
- severity: nit
- status: open
- file: commit message (verification item, not a code defect)
- description: The commit reports `web-server-integration-tests 97/1/0`. Standard Fixie console
  order is passed/failed/skipped, which would read as **1 failing test**; the author frames it as
  green ("with developer RpId secret still set (now inert)"), implying the `1` is a skip. I could not
  disambiguate statically (no `[Skip]`/`Ignore` attributes exist in the suite, and running the
  fixed-port suite alongside the active build agents is unsafe). Not blocking, but the Results write-up
  should state explicitly whether that `1` is a skip (and which test/why) or a failure.
- suggested fix: rerun `dotnet fixie tests/container-apps/web/web-server-integration-tests` in
  isolation and record the passed/failed/skipped breakdown in Results.

### G4 — Host-free unit tests live in the integration-test project
- severity: nit
- status: open
- file: `.../Features/Identity/WebAuthnOptions_Validator_Tests.cs`,
  `.../Features/Identity/WebAuthnRelyingParty_Selection_Tests.cs`
- description: Both are pure/host-free (no fixture injection, no server) but sit in
  web-server-integration-tests. This follows the existing grouping precedent (WebAuthnOptions_Binding
  and the other `*_Validator_Tests` for TrackEvent/CreateRole all live here), so it's consistent —
  just noting they carry the integration project's host build weight without needing it. No action
  required unless a host-free unit project is later introduced.

## Clean areas (explicitly verified)

- **Options + binder-append**: `RpId` removed outright; `AllowedRpIds` defaults `["localhost"]`.
  Append semantics documented thoroughly in the options Design region **and** pinned by
  `WebAuthnOptions_Binding_Tests` (`ShouldBe(["localhost","webauthn-second.test"])`). Shipped
  `web-server/appsettings.json` correctly does **not** list `AllowedRpIds` (only `RpName`/
  `AllowedOrigins`), so no double-append; the no-`AllowedRpIds`-in-shipped-config invariant holds.
- **Validator**: `NotEmpty` on the list + per-entry `Uri.CheckHostName == Dns`. Edge coverage is
  honest — scheme-prefixed, port-suffixed, path, empty entry, and IP-literal are all asserted invalid;
  `localhost`/`arch.timewarp.work` valid. Rationale for no duplicate-entry rule (append can yield
  dupes; harmless, first-match-wins) is documented. (Trailing-dot/punycode/unicode edge behavior is
  security-reviewer scope.)
- **Selection helper**: pure `static Select(string?, WebAuthnOptions) → OneOf<WebAuthnRelyingParty,
  SharedProblemDetails>`; case-insensitive match returns the **allowlist entry's** canonical casing
  (not the request's — verified by `Return_Canonical_Allowlist_Entry...` asserting `"WebAuthn-Second.Test"`);
  fail-closed 400 with no host echo (`Detail.ShouldNotContain("not-allowed.example")`); no fallback
  RP ID. Namespace/region conventions correct; OneOf usage matches repo handler patterns.
- **Handler integration**: all five handlers (Start/Complete Registration, Start/Complete
  Authentication, AddPasskey) select **before** any `ChallengeStore.Issue`/consume — verified line by
  line; no duplicated selection logic (all delegate to `WebAuthnRelyingPartySelection.Select`). Sync
  Start handlers correctly wrap the problem branch in `Task.FromResult`. AddPasskey's auth-guard-first
  deviation (select runs after the 401 guard, still before challenge consume) is accurately documented
  in its Design region and the inline comment. Every touched handler's Design region was reconciled to
  describe the new selection-first ordering (TWA0004/region-maintenance satisfied).
- **Port pattern**: `IRequestHostAccessor` sits in `web-application/abstractions` under namespace
  `TimeWarp.Architecture.Abstractions`, exactly mirroring `ICurrentPrincipalAccessor`; impl
  `HttpRequestHostAccessor` in web-server reads `HttpContext.Request.Host.Host` (port stripped),
  null-safe (no HttpContext → null → fail-closed). Registered scoped in program.cs next to the other
  HTTP accessors. New source files are kebab-case with Purpose+Design regions.
- **Hermetic test host**: strips every `JsonConfigurationSource` with `Path == "secrets.json"`;
  backwards iteration during in-place removal is correct; matches only user-secrets (appsettings use
  `appsettings*.json`, never `secrets.json`). Env vars deliberately preserved for CI. **Blast radius
  checked**: no test source in the repo depends on user secrets (only build artifacts reference the
  UserSecrets package), so stripping across all suites (api/yarp/spa/web all share
  `WebApplicationHost`) is safe. Pinned by `NoUserSecrets_Source_Given_HermeticHost`, whose assertion
  (`ShouldNotEndWith("secrets.json")`) is consistent with the strip's exact-match. Design region present.
- **Test quality/honesty**: `Ok_Register_And_Authenticate_Under_Second_Allowed_Host` genuinely
  exercises origin validation off the **selected** RP ID — authenticatorData hashes `webauthn-second.test`
  and clientDataJSON origin is `https://webauthn-second.test`, so the empty-AllowedOrigins
  `host == selected-RP-ID` rule is what accepts it (a broken selection would fail rpIdHash/origin,
  not silently pass). X-Forwarded-Host test asserts `rp.id` stays `localhost`. The
  `DangerousAcceptAnyServerCertificateValidator` + SNI workaround is contained to the test's local
  `Post` helper (a throwaway `HttpClientHandler`), justified (setting `Headers.Host` changes SNI so the
  localhost dev cert no longer name-matches) and does not touch the product or the shared test client;
  `CheckCertificateRevocationList` stays true. Naming follows repo Fixie conventions.
- **Stale references**: repo-wide `RpId` grep (kanban excluded) shows only legitimate hits —
  `RpIdHash` (WebAuthn protocol field in timewarp-identity), and test-local `const RpId = "localhost"`
  in the pre-existing Passkey_Registration/Authentication tests. No lingering `WebAuthnOptions.RpId`.
  No other appsettings (api/grpc) reference WebAuthnOptions. PasskeysPage carries the RP-ID
  credential-scoping note (requirement satisfied). Task-112 runbook workaround retired.

## Summary

- critical: 0
- major: 0
- minor: 2 (G1, G2)
- nit: 2 (G3, G4)

No blocking issues. Core change (options, selection helper, five-handler integration, hermetic host,
tests) is correct, well-documented, and convention-clean. The two minors both concern the ingress
edge: the standalone-yarp transform is placed where passkey traffic doesn't flow (G1), and the
host-preservation wiring has no automated test (G2) — both scoped to a non-active deployment path but
worth recording so "yarp covers this" isn't over-claimed.
