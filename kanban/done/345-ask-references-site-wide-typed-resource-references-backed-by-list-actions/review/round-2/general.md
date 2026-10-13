# Round 2 — general
**Date:** 2026-10-13
**Scope reviewed:** fix commit 4181c4291 (+ round-1 IDs)

## Summary
The fix commit addresses every finding it set out to fix (1, 2, 4, 5, 6, 7, 8, 9). The new TWA0033 checks do not produce false positives on the five real referenceable contracts (credential, feedback, my-feedback, principal, role). All five carry a Policy, and their Query types reach IApiRequest through IQueryStringRouteProvider. The registry generated after the shape change (`artifacts/generated/web-spa/.../GeneratedResourceReferenceTypes.g.cs`) still contains all five entries, with their policies and the stamped UserId for the three IAuthApiRequest queries. None of the ListPrincipals, GetRoles or GetCredentials handlers read `request.UserId`, so the Issue 8 design note holds today.

SessionChanged runs on the initial auth read (`OnInitializedAsync`) and on every change, so the "same principal keeps references" path cannot clear a live conversation after startup. The trailing-space insertion is correct, and the Playwright evaluate test covers it.

The fix delta adds three small edges, all nits:
- Search can still throw on one HttpApiService path.
- The async `@` word-start check depends on timing.
- Open has no guard against a principal change that arrives while it is in flight.

## Prior findings
| Round-1 issue | Verified status (fixed / still open / wontfix-intended) | Note |
|---|---|---|
| 1 — analyzer accepts shapes the generator cannot compile | fixed | `ReferenceableResourceShape.Read` now requires: public Query/Response/item (containing types and generic args included), IApiRequest, a public parameterless ctor (or a struct), no `required` member besides Search/UserId, a reference-type Response, and a public UserId setter. Tests were added for struct Response, an extra required member, no IApiRequest, no parameterless ctor, and an internal Response. No test for a non-public UserId setter or an internal item type (coverage only). |
| 2 — Roles-only authorize ignored by the SPA filter | fixed | Policy is required (`IsNullOrWhiteSpace` gives TWA0033). There is a `Given_Roles_Only_EndpointAuthorize_Flags_TWA0033` test. Skill note corrected. |
| 3 — cross-assembly duplicate prefix silently dropped | wontfix-intended | Unchanged by the delta. |
| 4 — transport/JSON exception escapes search; null items | fixed (one residual path, see Issue 1 below) | `HttpRequestException` and `JsonException` are caught. Null items are treated as `[]`. |
| 5 — picker state survives sign-out / sign-in | fixed | `SessionChangedActionSet` is keyed on `PrincipalIdOf` (sub, then oid, then NameIdentifier, then `name:`). It clears Types/Chosen/IsOpen and bumps SearchGeneration. The listener dispatches it on the initial read and on every change. An integration test covers same-principal, sign-out and other-principal. Narrow residual race with Open: Issue 3 below. |
| 6 — mid-word @ opens the picker and steals focus | fixed (timing caveat, see Issue 2 below) | `AtStartsWord` JS check plus a Playwright `ada@example.com` case. |
| 7 — no trailing space after the inserted token | fixed | Adds a space unless whitespace follows. Both Playwright tests were updated. |
| 8 — stamped random UserId | fixed (documented) | Shape/generator Design and the skill say the handler must not read UserId. Verified that no current referenceable handler reads it. |
| 9 — EF vs in-memory case folding | fixed (accepted, documented) | Recorded in the EF store Design region. |
| 10 — race-guard test; missing analyzer cases | race half wontfix-intended; analyzer half partially addressed | Six new TWA0033 cases. Still no cases for a missing Query/Response, a bad ItemsProperty, or a missing [ApiEndpoint]. These were not in the fix scope. |

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/resource-reference-type.cs:92
- Description: The search lambda catches `HttpRequestException` and `JsonException`. `HttpApiService.ReadFromJson` (source/foundation/foundation-contracts/services/http-api-service.cs:272) also throws `InvalidOperationException("The response is null.")` when a 2xx body deserializes to null (for example a literal `null` body). `HandleSuccessResponse` does not catch it, so it still escapes `SearchAsync` with `IsSearching = true`. That contradicts the new Design claim that "the picker's searching flag always clears". `HandleProblemResponse` in the same service already treats `InvalidOperationException` as a parse failure. The case is unlikely for these list endpoints.
- Suggestion: Add `InvalidOperationException` to the caught set, mapping it to "The list answer could not be read.", or soften the Design sentence.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:327
- Description: `OnComposerKeyDownAsync` now waits for two JS interop round trips (`import`, then `AtStartsWord`) before it dispatches Open, and `AtStartsWord` reads the textarea whenever that completes. The JS tries to handle both "@ not yet inserted" and "@ already inserted", but the check is still timing-dependent:
  - **Fast typing:** if a key after `@` lands before the check runs (likely with server interactivity, where each call is a network round trip), the character before the caret is that key, not `@`. `AtStartsWord` returns false and the picker never opens for a deliberate `@ro`. Before the fix, Open was dispatched unconditionally on the keydown.
  - **Repeated @:** with an existing `@` before the caret, the result depends on whether the new `@` has landed yet. Typing `@@` opens the picker under one timing and not under the other.
- Suggestion: Make the decision synchronously in the browser. For example, the module attaches a keydown listener to the textarea that records `atStartsWord` from the caret *before* insertion (no skip-one heuristic), and C# reads that flag. Or check it from the `input` event using the known position of the inserted `@` (`selectionStart - 1`). Either way the answer no longer depends on how many round trips happen.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/ask-reference-state/ask-reference-state.open.cs:20
- Description: Open awaits `GetAuthenticationStateAsync` and `PermittedAsync`, then writes `Types`, `IsOpen = true` and bumps SearchGeneration. It captures no generation or principal before awaiting. If SessionChanged is handled while an Open is in flight, Open resumes afterwards and writes back the previous principal's permitted types with the picker open. That is the stale-type-list case Issue 5 closed. The window is small (both awaits are local), and it depends on whether TimeWarp.State serializes handlers.
- Suggestion: Capture `SessionPrincipalId` (or a session counter that SessionChanged bumps) before the awaits in Open, and drop the result if it changed. Or compare `PrincipalIdOf(authenticationState.User)` with `SessionPrincipalId` before applying.
- Status: open
