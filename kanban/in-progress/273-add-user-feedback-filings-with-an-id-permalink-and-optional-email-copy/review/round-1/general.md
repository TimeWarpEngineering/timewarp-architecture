# Round 1 — general
**Date:** 2026-10-09
**Scope reviewed:** branch vs master

## Summary
The core filing path is sound. The owner comes from `ICurrentPrincipalAccessor`. Reads return 404 for another owner or a missing id. All three contracts carry `[EndpointAuthorize(Policy = feedback.file.self)]`. The migration seeds the permission with the correct `RoleIds` GUIDs. Sign-out clears `FeedbackState`. Submit is not on the read-only allow-list, so it stays approval-gated. Five defects remain: (1) the emailed absolute permalink is wrong behind the YARP ingress; (2) a mail failure after the row is saved drops the receipt; (3) the WebMCP "receipt" proof never runs the real dispatcher path; (4) the development sender runs in every environment and reports `EmailCopySent=true` without delivering anything; (5) a submit made by an agent does not refresh the filer's list. There are also smaller items about stale regions and hardening.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/platform/mail/http-app-base-url-accessor-server.cs:29
- Description: The base URL is built from `request.Scheme` and `request.Host`. The repo's own records say what that yields behind the ingress. `http-request-host-accessor-server.cs` (Design) says YARP sends the *destination* host as `Host` and puts the browser host in `X-Forwarded-Host`. `entra-authentication-registration-server.cs` (Design) says the hop to Web.Server is plain http, with no `UseForwardedHeaders`. So on every ingress target (run with YARP, Compose, Kubernetes, ACA) the emailed "Permalink:" is something like `http://web-server.internal.../Feedback/{id}`. That link does not work for the recipient. The test uses a fixed `FixedBaseUrl("https://app.example")`, so it never covers this. A Host-trust note: the only recipient is the filer's own address, so this is not a phishing vector today. It is still a wrong link in the one artifact the email exists to carry.
- Suggestion: Take the public origin from configuration, for example `Mail:PublicBaseUrl`, the same way Entra uses `PublicOrigin`. Another option is the public host from `HttpRequestHostAccessor.GetPublicHost` with an explicit https scheme. When neither is available, fall back to the relative path. Record the choice in the Design region.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/features/feedback/submit-feedback/submit-feedback-handler-application.cs:72-88
- Description: `FeedbackStore.AddAsync` commits before `EmailSender.SendAsync`. `DevelopmentEmailSender` deliberately lets IO failures through ("IO failures propagate"). A real provider will throw on transport errors. If sending throws, or the request is cancelled after the save, the item is already stored but the caller gets a 500 with no id. The filer then retries and creates a duplicate. This is the exact failure the task exists to close: the acceptance bar is the receipt.
- Suggestion: Make the mail best-effort after the save. Catch exceptions other than cancellation, log them, and return the receipt with `EmailCopySent = false`. Pass `CancellationToken.None` to the send once the row is committed, or treat cancellation the same way. Add a test with a throwing `IEmailSender` that asserts the id and permalink still come back.
- Status: open

### Issue 3 — Severity: bug
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:543 (and source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:65)
- Description: The `WEBMCP-PROOF` receipt in Results is built by passing a hand-made anonymous object `new { Id = itemId, Permalink = ... }` to `WebMcpDispatcher.FormatCompleted`. That is a public static added only for this test. `InvokeTool` does not use it; it serializes its own `WebMcpCompleted`. The test therefore never shows that `AgentCallOutcome` is set by `SubmitFeedbackActionSet.Handler` and read back after `Execute`. It shows the same for neither the in-app `CatalogAgentFunction` path nor the WebMCP path. The quoted shape is also wrong: the real payload is `SubmitFeedback.Response`, which serializes `feedbackItemId`, `permalink`, `kind`, `title`, `body`, and `emailCopySent`, not `id`. Steve's direction says "the agent gets the id and permalink back in the result", and Results claims "An approved submit returns the id and permalink in the tool result". No test backs either statement.
- Suggestion: Remove `FormatCompleted`. Drive `WebMcpDispatcher.InvokeTool("Feedback.OpenFeedback" or "Feedback.ListMyFeedback", ...)`, which need no approval, through the SPA test scope with the mock API factory. Assert that the returned JSON carries `result.feedbackItemId` and `result.permalink`. If the gate can be answered in-test, cover Submit too. Do the same once for the `CatalogAgentFunctions` AIFunction result. Then correct the proof line in Results.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/platform/mail/mail-module-server.cs:20 (with development-email-sender-server.cs:38)
- Description: `MailModule` registers `DevelopmentEmailSender` in every environment. In a deployed app, a filer with a profile email sees "Email me a copy", ticks it, and gets `EmailCopySent = true`. Nothing is delivered. Meanwhile the recipient address and the full feedback body are logged at Information level and reach production telemetry.
- Suggestion: Register the development sender only in Development and Testing. Otherwise register a no-op or absent sender and make `EmailCopyAvailable` (and the `EmailCopySent` path) depend on a real sender being present. Separately, do not log the body; log the id and recipient at most.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/feedback/feedback-state.submit-feedback.cs:72-77 (with FeedbackListPage.razor:24-35)
- Description: Only the page's `SubmitAsync` dispatches `ListMyFeedback` after a successful submit. When the in-app assistant or a WebMCP agent submits on `/Feedback`, `LastReceipt` updates but "Your filings" stays stale until something reloads it. The human path and the agent path therefore show different results for the same action. Steve's direction says the two should drive the same functionality.
- Suggestion: Update the list in the submit handler, either by inserting a `ListMyFeedback.Item` built from the response at the top of `Items` or by dispatching the list refresh from the handler. Then remove the relist from the page. Reconcile the Design line "The page prepends nothing itself — it re-lists after success."
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/platform/mail/email-message-application.cs:14-21 (with development-email-sender-server.cs:49-55)
- Description: `To` comes from `Profile.Email`. The only check on that value, in both `ProfileDetailsValidator.EmailAddress()` and `Profile.SetEmail`, is "contains @, not first or last". A value with CR/LF passes, for example `a@b\r\nBcc: x@y`. It is written straight into the `.eml` header block, so the pickup file gets extra headers. This is the seam a real provider will use. Related point for the Results note on real providers: `Profile.Email` is unverified. "Email me a copy" therefore lets any member have the app send text they wrote to any address they choose, with no rate limit.
- Suggestion: Reject CR or LF in `To` and `Subject` in the `EmailMessage` constructor. Add to the real-provider notes in Results that the provider needs a verified address, or a rate limit, before it goes live.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/platform/mail/email-message-application.cs:6-7
- Description: The Design region says "The development sender supplies a fixed from-line in the pickup file". `DevelopmentEmailSender` writes only `To:` and `Subject:`; there is no `From:` line. The Results paragraph in task.md repeats the same claim.
- Suggestion: Either add a fixed `From:` line, such as `noreply@localhost`, to the `.eml`, or correct both texts.
- Status: open

### Issue 8 — Severity: nit
- File: source/container-apps/web/platform/postgres/migrations/20261009001245_AddFeedbackItems.cs:9
- Description: The migration class is `_20261009001245_AddFeedbackItems`. Sibling migrations are `AddCredentialAccountHint` and similar. The migration also uses file-scoped namespaces, and the regenerated snapshot rewrote the whole file: block to file-scoped namespace, BOM dropped, `ProductVersion` 10.0.11 to 11.0.0-rc. Behavior is correct because the `[Migration]` id is right, but the snapshot diff (about 369 lines) hides the real model change.
- Suggestion: Rename the class to `AddFeedbackItems` to match its siblings. Keeping the snapshot reformat is acceptable, but mention it in the PR body so reviewers know.
- Status: open
