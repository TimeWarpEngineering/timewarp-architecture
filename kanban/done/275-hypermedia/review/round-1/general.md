# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** `git diff master...HEAD` excluding kanban: lab slice (contracts, handlers, snapshot, state, rows, page, context source), Ctrl-K hook (command-palette-context/runner/row/open, CommandPalette.razor), ContextualActionArguments binder, CredentialRules reuse in RevokeCredential.Handler and EntraTicketProcessor, program.cs/NavMenu wiring. Requirements 1-8 checked against code.

## Summary
The server side and the CredentialRules reuse are behavior-preserving (`active.Count <= 1` equals `!CanRevoke`; the Entra check is the same predicate). The binder, runner gate and C same-origin guard look correct. One real bug: the palette list uses a `@key` that is not unique for the lab's contextual rows, which will throw when the palette renders them. One suggestion on stale cross-tab payloads, one nit on a stale Design region.

## Issues
### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/command-palette/CommandPalette.razor:242
- Description: Rows are keyed `$"{row.Kind}:{row.Target}"`. That was unique for Page (URL) and Command (catalog name) rows, but contextual rows share Targets: every approach C row has Target `HypermediaLab.FollowCommand` (so with 1 credential, Rename + Revoke + Link already collide), and approach B Revoke/Rename rows repeat `Credentials.RevokeCredential` per credential. Blazor throws "More than one sibling ... has the same key value" on duplicate @key in a loop. Headless tests cover the roster, not the rendered list, so nothing catches it.
- Suggestion: Make the key unique per row, e.g. include Name (and ArgumentsJson for contextual rows), or use `@key=row` (record equality is already total over the fields) / the row index plus Kind.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/hypermedia-lab/pages/hypermedia-lab-page/HypermediaLabPage.razor:39
- Description: B and C payloads refresh independently. A B action refreshes only Offers (FollowUpTarget FetchCredentialOffers); a C action refreshes only Commands (Self). Both payloads are loaded on init and both approaches' rows stay in Ctrl-K, so after a B revoke the C tab and the "Lab C: Revoke" rows still show the old credential list and a Revoke that the server would now refuse (409 LastCredential), and vice versa. Requirement 3 says only the latest payload's actions are shown; the check IsOffered passes because it compares against the stale payload.
- Suggestion: Refetch the other payload after an action (e.g. a second follow-up, or refetch on tab select), or document that the lab's two payloads are independent snapshots.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-ranker.cs:11
- Description: The Design region says an empty query orders "pages first, then commands", but `CommandPaletteRowKind.Contextual` was added as the first enum member, so contextual rows now sort first (the row Design says so). Region disagrees with behavior; reordering also changes the enum's default value from Page to Contextual.
- Suggestion: Update the ranker Design line (and optionally append Contextual as the last member and rank it explicitly).
- Status: open
