# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** branch vs master

## Summary
The implementation matches the brief. The marker, shared shape reader, analyzer (TWA0033/TWA0034), generated registry, Search on the four list queries, picker state, and page_context `references` are consistent. The claimed tests exist (9 analyzer, 3 generator, 8 SPA state). The server filters and the policies hold: every search runs through the list endpoint as the caller, the EF filter is a parameterized `lower(title) LIKE`, and the in-memory store and mocks behave the same way. There are no blocking correctness bugs. The findings are gaps between what the analyzer accepts and what the generator can compile, state that survives a sign-out or sign-in, an unhandled transport exception in the debounced search, and a few UX and consistency edges.

## Issues

### Issue 1 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-analyzers/generators/resource-reference-registry-generator.cs:83
- Description: The design region says a contract whose shape is wrong is "absent from the registry, never a broken generated call". That is not fully true. `ReferenceableResourceShape.Read` does not check several things the generated `ResourceReferenceType.Create<TQuery, TResponse, TItem>(… static search => new TQuery { Search = search … })` needs:
  - `Query` implements `IApiRequest` (the `where TQuery : IApiRequest` constraint);
  - `Response` is a reference type (`where TResponse : class`). A `record struct Response` passes TWA0033;
  - `Query` has an accessible parameterless constructor and no other `required` members. A positional record `Query`, or a `required` property besides Search, passes TWA0033;
  - `Query`, `Response` and the item type are accessible from the SPA (public);
  - for `QuerySetsUserId`, `UserId` has a *public* setter (`SetMethod: not null` also accepts a private or protected setter).

  Any of these passes the analyzer and then breaks the SPA build with a CS error in `GeneratedResourceReferenceTypes.g.cs`, not with TWA0033 on the contract.
- Suggestion: Add these checks to `ReferenceableResourceShape.Read`, so the analyzer reports them and the generator skips the contract. Add analyzer tests for a struct Response and for a Query with a `required` member.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/analyzers/shared/referenceable-resource-shape.cs:135
- Description: Only `[EndpointAuthorize(Policy = …)]` feeds the SPA type filter. `Roles = "…"` is ignored. A contract gated by Roles only (with Policy null) becomes "any signed-in user" in `ResourceReferenceRegistry.PermittedAsync`. A Member is then offered the type and gets a 403 Problem on choosing it. The server still enforces access, but the type list leaks the type's name and existence. The skill note says the same "Policy decides who sees the type", which is wrong for a Roles-gated contract.
- Suggestion: Pick one:
  - Require Policy for referenceable contracts, so TWA0033 fires when only Roles is set.
  - Carry Roles into the registry and check it with `user.IsInRole`.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/referenceable-resource-analyzer.cs:94
- Description: TWA0034 is per compilation. The generator unions every marked hosted-contract assembly plus the SPA compilation (`HostedRouteDiscovery.GetMarkedReferencedAssemblies`). If two contract assemblies each declare `@feedback`, neither analyzer run reports it. The generator then quietly keeps the first one by `ContractType` order (`if (!seen.Add(shape.Prefix)) continue;`), and one resource type disappears with no diagnostic.
- Suggestion: Have the generator report a diagnostic, or have the analyzer run on the SPA and check across referenced assemblies, when it drops a duplicate prefix from another assembly.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/ask-reference-state/ask-reference-state.cs:176
- Description: `HttpApiService.GetResponse` catches only `OperationCanceledException`. A transport failure (`HttpRequestException`, for example when offline) or a JSON failure propagates out of `SearchAsync`. That exception reaches the Filter or ChooseType handler after `BeginSearch` has already set `IsSearching = true`. Unless the pipeline rolls the state back, the picker stays on "Searching…" until the next keystroke, and the exception reaches the action pipeline or error UI from a keystroke. There is also a smaller gap: `items(response)` is not null-guarded in `ResourceReferenceType.Found` (resource-reference-type.cs:113), so a response whose Items deserializes to null throws a NullReferenceException.
- Suggestion: In `ResourceReferenceType.Create`'s search lambda, catch `HttpRequestException` and `JsonException` and return `ResourceReferenceSearch.Failed(...)`. Treat a null items enumerable as empty.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/ask-reference-state/ask-reference-state.cs:79
- Description: AskReferenceState does not follow the session. Neither `AuthenticationStateListener` nor anything else clears `Chosen`, `Types` or `IsOpen` on sign-out or sign-in, and sign-out does not bump `ConversationGeneration`. Two cases:
  - **Leaked references:** an Administrator chooses `@principal:…` ("Ada Lovelace") and signs out. Another person signs in on the same tab. That person's first page_context still lists the earlier references, with labels drawn from data the new user may not be allowed to read.
  - **Stale type list:** the picker is open when the session ends. It is hidden in the Unauthenticated branch, but `IsOpen` stays true. When the chat branch renders again, the picker reappears with the earlier user's types. Open is not re-run, so its "rebuilt on every open" guarantee does not apply.

  The server re-authorizes every call, so this is not an escalation. It is a cross-user state and label leak, and it contradicts the Design claim.
- Suggestion: On sign-out and sign-in, have the listener dispatch AskReferenceState Close plus a clear-chosen action (or a Reset action). Alternatively, key `Chosen` by principal as well as by conversation generation.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:324
- Description: Every `@` keydown in the composer opens the picker and moves focus into it, including mid-word: `ada@example.com`, `foo@bar`, or a pasted handle. A user typing an email address has their following keystrokes go to the picker's search box instead of the draft. They must press Esc to get back and retype.
- Suggestion: Open the picker only when the `@` starts a word (the character before the caret is empty or whitespace). The check can be done in `ask-ai.ts`, or by passing the preceding character. The other option is to open without stealing focus until the next key.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/features/ask-ai.ts:27
- Description: `InsertReference` inserts `@prefix:<guid>` with no trailing space and puts the caret right after it. If the user keeps typing (`@role:<guid>please`), the next characters join the token, and the model has to separate the id from the text itself.
- Suggestion: Append a single space when the next character is not already whitespace.
- Status: open

### Issue 8 — Severity: nit
- File: source/analyzers/shared/referenceable-resource-shape.cs:144
- Description: For an `IAuthApiRequest` query, the generator stamps `UserId = Guid.NewGuid()`. The design region says this is "the mock identity signal the SPA's other handlers stamp too". That holds for roles and principals, but `CredentialsState.FetchCredentials` resolves the *real* user id. It works today because the GetCredentials handler never reads `UserId`. A future referenceable `IAuthApiRequest` whose handler does read it would silently query a random user.
- Suggestion: Record in the attribute or shape Design that a referenceable query must not rely on `UserId`, or have TWA0033 reject that. The alternative is to resolve the id from the auth state the way FetchCredentials does.
- Status: open

### Issue 9 — Severity: nit
- File: source/container-apps/web/features/feedback/ef-feedback-store-infrastructure.cs:127
- Description: Case-folding differs between the stores. EF lower-cases the term with `ToLowerInvariant()` on the client and the column with Postgres `lower()`, which uses the database collation. The in-memory store uses `OrdinalIgnoreCase`. For non-ASCII titles (for example Turkish dotted I, or ß), the two stores can return different matches. The SQL is parameterized, so there is no injection risk.
- Suggestion: Accept this and record the difference in the store Design region, or use `EF.Functions.ILike` with an escaped pattern so Postgres folds both sides.
- Status: open

### Issue 10 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-reference-tests.cs:102
- Description: The tests do not cover the race guard. No test checks that an older, slower search result is dropped when a newer term, Back or Close arrives. Every test awaits each Send to completion, so `SearchGeneration` is never exercised concurrently. The analyzer tests also have no case for a missing Query or Response, a bad ItemsProperty, or a missing [ApiEndpoint].
- Suggestion: Add one test that starts two Filter sends without awaiting the first (or uses a gated fake API) and asserts that only the newest term's items apply. Add analyzer cases for the remaining TWA0033 branches.
- Status: open
