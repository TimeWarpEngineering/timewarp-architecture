# Round 1 — security
**Date:** 2026-10-05
**Scope reviewed:** `git diff master...HEAD` excluding kanban. Approach C (`app-relative-href.cs`, `followed-link-request.cs`, `hypermedia-lab-state.follow-command.cs`), approach B binder (`contextual-action-arguments.cs`), contextual row gate (`command-palette-context.cs`, `command-palette-runner.cs`, `hypermedia-lab-rows.cs`), server endpoints under `web/features/hypermedia-lab/**`, and the `credential-rules-application.cs` / `revoke-credential-handler-application.cs` / `entra-ticket-processor-application.cs` changes.

## Summary
No exploitable bugs found. Verified against code:
- C same-origin guard: `AppRelativeHref` rejects empty, non-"/" start, "//", "/\", any backslash, whitespace/control chars, and absolute URIs. FollowCommand checks the href before use, requires method+href to match a command in the CURRENT payload, requires fields to equal the command's Fields, and refuses methods other than POST/NAVIGATE. `Self` is re-validated before the refresh GET. POST goes through `IWebServerApiService` (same bearer pipeline); no second HTTP path. Server-supplied Body keys cannot be overridden by user fields (field names must equal server-declared Fields, and they are the server's own names).
- B binder: names not declared by the entry are rejected, missing required params rejected, no holes, deserialization failures fail closed. Runner: `IsOffered` (record equality over the contributed rows) gates every contextual run; user input can only fill unbound params (`TryAdd`), never replace server-bound args.
- Server: both endpoints carry `[ApiEndpoint]` + `[EndpointAuthorize(Policy = CredentialManageSelf, Identity session + AgentToken)]`, matching GetCredentials. Caller id comes only from `ICurrentPrincipalAccessor`; data read via the real handlers (IDOR scoping stays there). Responses contain only the caller's own data (userId in Body is the caller's own id); no extra leakage.
- Credential rules: `CanRevoke(count) = count > 1` is identical to the old `active.Count <= 1` negation; `HoldsMicrosoft365` is identical to the old `Any(Type == EntraAccount)`. Enforcement is not weakened vs master (still inside the retry loop / same call site).

Only minor hardening suggestions below.

## Issues
### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/contextual-action-arguments.cs:88
- Description: A JSON `null` argument for a required reference-typed parameter deserializes to `null` and is passed positionally, so "needs '<name>'" is satisfied by an explicit null. Required-ness is only checked for key presence.
- Suggestion: Treat `JsonValueKind.Null` (or a null result) for a required parameter as a binding failure.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-runner.cs:84
- Description: Contextual rows skip catalog Permissions/Visibility (documented in the Design region), and the client has no allow-list of offerable names: any catalog name in a payload (including non-Human / agent-only entries) with bindable args would run. This relies entirely on the server being trusted. Acceptable for the lab; matters for adoption where a compromised or buggy payload could drive any catalog action.
- Suggestion: On adoption, restrict contextual targets to a client-known allow-list (or require Visibility Human and the entry's Permissions to be held).
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/hypermedia-lab/app-relative-href.cs:17
- Description: App-relative is not the same as endpoint-scoped: a payload could name any same-origin path (e.g. "/api/other") for POST with the user's bearer token, or for NAVIGATE. Within threat model (server is trusted), but the check does not constrain to the "/api/" prefix the Design comment mentions.
- Suggestion: Optionally require the "/api/" prefix for POST and a known challenge prefix for NAVIGATE.
- Status: open
