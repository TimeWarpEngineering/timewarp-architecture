# Round 1 — tests
**Date:** 2026-10-05
**Scope reviewed:** `git diff master...HEAD` (excluding kanban/) test surface:
- `tests/.../web-spa-integration-tests/features/identity/credential-offers-tests.cs` (new)
- `tests/.../web-spa-integration-tests/features/identity/credentials-spa-test-application.cs` (scripted BFF)
- `tests/.../web-server-integration-tests/features/identity/credential-offers-tests.cs` (new)
- `tests/.../web-contracts-tests/features/identity/identity-contracts-serialization-tests.cs`
- `protected-page-deep-link-tests.cs`, `action-catalog-tests.cs`, `command-palette-tests.cs`,
  `settings-page-microsoft-365-tests.cs`, `credential-list-render-tests.cs`, `credential-last-used-tests.cs`
- deleted: `hypermedia-lab-tests.cs`, `hypermedia-lab-endpoint-tests.cs`,
  `hypermedia-lab-contracts-serialization-tests.cs`, `credentials-state-revoke-guard-tests.cs`
- product code the tests target: `CredentialOffers`, `EntraSignInOffer`, `GetCredentials.Handler`,
  `OfferedAction`/`OfferedActionNames`, `CredentialOfferRows`, `CredentialsContextSource`,
  `CommandPaletteContext.RefusalAsync`, `CommandPaletteRunner`.

Test runs (this worktree, serial, foreground):
- web-spa-integration-tests `--filter-class CredentialOffers`: 15/15 passed
- web-server-integration-tests `--filter-class Offers`: 9/9 passed
- web-contracts-tests `--filter-class GetCredentials`: 4/4 passed

## Summary

The required test list is covered and the assertions are real, not vacuous:
- Server rule table host-free (`CredentialOffers_For_`): one credential → no Revoke, two of any
  kind → Revoke on both, revoked rows ignored, Microsoft 365 offered/linked/revoked-link/not-offered,
  and emitted names == `OfferedActionNames.All`. End to end over HTTP: one/two credentials, a real
  revoke drops Revoke, Entra off → no Link.
- Name resolution: SPA `Name_Only_Catalog_Entries_A_Person_May_Run` resolves every
  `OfferedActionNames.All` entry in the real `IActionCatalog`, checks Human/Both visibility and the
  `credentialId` / `nickname` parameter names; combined with the server's `Name_Only_The_Declared_Vocabulary`
  this pins "every emitted name resolves".
- Running an offer runs the real action (request sent, real notification, follow-up fetch, offers
  refresh); non-offered / forged / stale-page rows refused with nothing sent; unknown name and
  bad-binding refused; M4 Agent-only entry and unpermitted principal refused (real
  `IAuthorizationService` over the app's policies).
- Contract round-trip includes offers (credential-bound and page-level) and the mock factory
  carries offers and round-trips.
- Deleted coverage is replaced: the revoke-guard and CanUnlink/CanLinkMicrosoft365 formula tests
  covered client rule copies that no longer exist; their behavior now lives in the server rule
  tests plus the unchanged prerender facts (Revoke/Unlink disabled + hint, Link shown/hidden), which
  now exercise the server offers end to end. The lab's B tests were ported (binder failure cases
  minus `[1,2]`, which no longer type-checks against `IReadOnlyDictionary` Arguments). The
  context-hook tests (Open append, contextual rows head the list, static roster still present,
  RequiresInput rows excluded, rows vanish off-page) are re-expressed on `CredentialsContextSource`.
- Conventions: Jaribu + Shouldly only; no xUnit/Fixie/FluentAssertions; C-create per test
  (SPA) and per class (server). FakeItEasy only fakes `IJSRuntime` (an externality).

No bugs found. The issues below are coverage-strength suggestions and nits.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credentials-spa-test-application.cs:115
- Description: `ScriptedCredentialsApiService.Offers()` hand-copies the server offer rule (Rename
  per active row, Revoke while `active.Length > 1`, Link while offered and no active Entra). That is
  a test-side copy of `CredentialOffers.For` — the same kind of duplicate rule this task removes from
  the SPA. If the server rule changes (e.g. Rename limited to passkeys, or a new offer), the SPA
  tests keep passing against a stale script. The test project already references `web-server`
  (and so Identity Application — the deleted Settings test used `IdentityProblems` from it), so the
  real function is reachable.
- Suggestion: `List<OfferedAction> offers = [.. CredentialOffers.For([.. Credentials], Microsoft365Offered)];`
  then append `ExtraOffers`; update the doc comment ("the offers the server would make") accordingly.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-server-integration-tests/features/identity/credential-offers-tests.cs:178
- Description: The test comment, the Design region (lines 8–10) and task.md's "Microsoft 365
  coverage gap" decision all say the "Link offered" side is unreachable end to end because the
  in-proc host runs with Entra off. That is not accurate: `protected-page-deep-link-tests.cs`
  already flips it in-proc (`options.Enabled = true` + `SiteSettings.ReplacePolicy(true, …)`, and
  the `EnableMicrosoft365OfferedAsync` helper), and `Settings_Microsoft365_Section_Should_Follow_Server_Offered_Flag`
  asserts `data-qa="LinkMicrosoft365"` renders — which since this task comes from the GetCredentials
  Link offer. So the offered side is in fact covered end to end via prerender HTML, and could be
  pinned directly at the API level too. As written, the GetCredentials handler's wiring of
  `EntraSignInOffer` → `CredentialOffers` (the `true` path, and "linked Entra → no Link") is only
  observed indirectly.
- Suggestion: Add `Link_Microsoft365_Given_Entra_Offered_And_Not_Linked` (and optionally the
  linked case) to `GetCredentialsOffers_Returns_` using the same toggle-and-restore pattern
  (this class owns its own host graph, so mutating options is isolated); correct the
  comment/Design region and the task.md decision text to reference the prerender coverage.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-offers-tests.cs:72
- Description: `Hold_The_Servers_Offers_Not_A_Client_Rule` claims to prove the client shows "only
  what the server offers" regardless of what it could count, but the scripted server applies the
  very count rule a client would (Issue 1), so every scenario in the test yields identical results
  under "follow the server" and "compute the rule client-side". The test cannot fail for the
  regression its name describes (e.g. a page reintroducing a count-based predicate).
- Suggestion: Make the server diverge from the count: e.g. two active passkeys but the script
  offers no Revoke (or a single credential with an injected Revoke via `ExtraOffers`), then assert
  `IsOffered` / the contributed rows / `CredentialOfferRows.RunAsync` follow the offer, not the count.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-context.cs:79
- Description: The M4 gate's unauthenticated branch (`"sign in first"`) has no test; all
  `CredentialOffers_Should_` cases use a signed-in principal. It is fail-closed code on a security
  gate.
- Suggestion: Add a case with an anonymous `ClaimsPrincipal` (OffersSpa constructor variant) that
  runs an offered row and asserts the refusal warning and no request.
- Status: open

### Issue 5 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/identity/credential-offers-tests.cs:282
- Description: "A row copied on Settings cannot be run from another page" asserts only that no
  `RevokeCredential.Command` was sent. It does not assert the refusal warning or that the
  follow-up `GetCredentials` was not sent either, unlike the other fail-closed cases
  (`spa.Api.Requests.Count.ShouldBe(before)` + warning title).
- Suggestion: Capture `before = spa.Api.Requests.Count` and assert unchanged plus the
  "is not offered here now." warning.
- Status: open

### Issue 6 — Severity: nit
- File: tests/container-apps/web/web-contracts-tests/features/identity/identity-contracts-serialization-tests.cs:521
- Description: `SerializeAndDeserialize_Mock_Response_With_Offers` indexes
  `Arguments[CredentialIdArgument]` on every mock offer, so adding a page-level offer to the mock
  factory (e.g. Link Microsoft 365) would throw `KeyNotFoundException` rather than fail a clear
  assertion; it also compares Name/Subject/credentialId but not `Label`.
- Suggestion: Compare `Label` and the full `Arguments` key set per offer (e.g. keys equal, then
  values by key) instead of assuming `credentialId` is present.
- Status: open
