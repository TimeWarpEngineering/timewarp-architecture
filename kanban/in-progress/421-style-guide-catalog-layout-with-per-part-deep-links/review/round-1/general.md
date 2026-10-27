# Round 1 — general
**Date:** 2026-10-27
**Scope reviewed:** `e7f0fa292..964b41571` (style-guide feature, contract/registry/nav/palette/agent tests, Playwright tests). The audit-fix changes (`Directory.Packages.props` Nuru beta.6 pin, `.githooks/pre-push.cs`) were noted and not reviewed as findings, per policy.

## Summary

The change turns the 29-card style guide into a catalog. It adds a `StyleGuideParts` table (slug, title, group, usage, sample type) and 29 sample components that All and the single-part view both render. It adds a native `<nav>` part list with a filter, `/StyleGuide/{Part}` as a `[Route]`-only alias, an unknown-slug state, and `data-token` rules in place of the two inline `style=` attributes. I checked it against the task and the implementer restatement, and the restatement matches the task. All order matches the base page's 29 Card/Section titles. The usage snippets use the same component parameters as the samples. The alias stays out of the registry, nav, palette, and agent, and tests assert this. I re-ran the gates in this worktree: `dev build` succeeded, `dev test -g integration` passed 8 of 8 projects, and `dev test -g playwright` passed (web-spa-playwright-tests, 259 s). No correctness bugs were found. The findings below cover accessibility semantics, a test that skips more than it needs to, and rationale comments lost in the split.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/style-guide/components/StyleGuideCatalog.razor:94
- Description: The group headings (Foundations, Forms, Layout, Data, Feedback, Identity) are `<p class="sg-catalog__heading">`, and nothing ties a heading to its `<ul class="sg-catalog__parts">`. They look like headings but carry no heading or group semantics. A screen reader user hears 30 links with loose text in between, which fails WCAG 1.3.1 (Info and Relationships). The task asks for "grouped headings".
- Suggestion: Make each label a real heading (for example `<h2>`, styled the same, since the stage cards use `h2` titles), or give the label an id and set `aria-labelledby` on the nested `<ul>`. Keep the current look.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs:247
- Description: `Keep_Stored_Choices_On_The_Form_Fields_Only` skips `StyleGuideCatalog.razor` with `continue`. The skip is meant to exempt the catalog's filter `ValueChanged`, which tw-blazor allows as component-local input text. Because of the `continue`, the catalog also skips the source-product-name check on line 255, so this one file is never checked for that name. `dev check-client-names` still scans it, but the contract's own guard misses one file.
- Suggestion: Exempt only the `Changed="` and `ValueChanged` assertions for the catalog, and run the `SourceProductName()` check on every feature file.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/style-guide/samples/NotificationsSample.razor:5
- Description: The base page explained why these demos are built the way they are. The bars go through NotificationState, not a direct `INotificationService` call. The exception demo follows the chain "handler throws → StateTransactionBehavior publishes ExceptionNotification → message bar". It dispatches through the ActionSet because TWA0022 bans a direct mediator Send. The split dropped those comments. The page's `#region Design` keeps only the general "demos go through production code paths" rule, so the next editor of this sample loses the why.
- Suggestion: Move the two short rationale comments into `NotificationsSample.razor`, as an `@* … *@` note like the one on `StyleGuideCatalog.razor`, or add them to the page Design region next to the NotificationsSample line.
- Status: open
