# Prefill every edit form: wire Role, AI, Authentication, and Role create into the 490 prefill tools and collapse the per-form boilerplate

## Description

Task 490's spec: "For each page-scoped submit action in `[Page(AgentActions=...)]` whose page has
an edit form, generate a `Prefill.<Action>` agent tool." Profile was named only as "first
consumer". What landed (PR #665) is a generic tool generator (`form-prefill.cs` builds a tool for
any catalog entry carrying `[EditFormSubmit]`) with exactly one opt-in,
`Profile.UpdateProfile`. The narrowing was not recorded in Results, review, or a follow-up.

Each further form currently needs three hand-written pieces: the attribute on its submit action,
its own suggestion action set (set / confirm / clear), and apply, dirty-check, and Agent-badge
code in the page (`ProfilePage.razor`). That boilerplate is why only one form was wired. This
task wires the rest and shrinks the per-form cost so the next form is one attribute plus one
shared page component.

## Requirements

### Collapse the boilerplate first

- One generic suggestion store keyed by the submit action type (or the page type) replaces the
  per-slice `Set/Confirm/ClearProfileSuggestion` trio. Profile migrates onto it; its tests keep
  passing.
- One shared page component (or a base the 489 working-copy pages already share) owns apply,
  dirty-check dialog ("An agent drafted new … values. Applying them replaces unsaved changes on
  this form." / "Apply draft" / "Keep mine"), and the `Agent` info badge per field, so a page
  opts in by rendering it with its working copy and submit action.
- The `[EditFormSubmit]` opt-in stays. If the generator can instead derive the opt-in from a
  page-scoped submit action whose Command implements an `I*Details` interface, prefer that and
  delete the attribute; record the decision in the Design region of `form-prefill.cs`.

### Wire every edit form

| Page | Submit action | Prefill tool |
|---|---|---|
| `RoleDetailPage` (`RoleEditForm`) | `Role.UpdateRole` | `Prefill.Role.UpdateRole` |
| `RolePage` (`RoleForm`, create) | `Role.CreateRole` | `Prefill.Role.CreateRole` |
| `AiPage` | `SiteSettings.UpdateAssistantName`, `SiteSettings.UpdateXaiByokMode` | one tool each |
| `AuthenticationPage` | `SiteSettings.UpdateSiteSettings` | `Prefill.SiteSettings.UpdateSiteSettings` |
| `PasskeysPage` rename | check: if it is a page-scoped submit with a Command, wire it; else record why not |

A new edit form added later must get its tool by rendering the shared component; add a
convention check (analyzer or test over the page registry) that fails when a page has a
page-scoped submit action with an `I*Details` Command and no prefill opt-in.

### Evidence

- For each wired page: a screenshot in `evidence/` of the agent-filled field with the `Agent`
  badge, taken from the running app via Ask AI, signed in with the right permissions.
- Results list every `Prefill.*` tool the catalog now exposes (`web-mcp` listing output pasted).

## Checklist

- [ ] Generic suggestion store + shared page component; Profile migrated, 490 tests green
- [ ] Role update, Role create, AI (two), Authentication wired; Passkeys decided
- [ ] Convention check fails on an unwired edit form
- [ ] Screenshots per page in `evidence/`; tool listing in Results
- [ ] `dev build` 0/0, web-spa integration tests, catalog-agent-webmcp tests, `ganda repo audit` green

## Depends on

- 490
- 507

## Session

- Created: 2026-11-24. 490 scope was generic; the implementer narrowed it to Profile without
  recording the deferral. Investigation table above is from the 2026-11-24 scope check.
