# Convert AuthenticationPage and SiteSettingsState

## Goal (implementer restatement)

Admin → Authentication should edit one command copied from the site-settings response, and the store should keep that response as one object. `SiteSettingsState` drops the separate policy, name, mode, version, configuration, and accepted-tenant fields. It keeps `GetSiteSettings.Response? SiteSettings`. Fetch assigns that response. A successful authentication, assistant-name, or key-policy save replaces it. Readers (the passkey gate, agent page facts, and the AI page's host-managed mode line) read the response.

The authentication form does not copy fields one by one. `UpdateSiteSettings.Command.From(SiteSettings)` runs when the page loads, when Cancel is clicked, and again after a successful save. It does not run on every render. `Version` stays off `ISiteSettingsDetails`; `From` copies it through `CopyBeyondDetails`. `AssistantName`, `XaiByokMode`, and `Configuration*` stay on the response and off the command. A later response while the page is open is reconciled in `ShouldReRender`. An unedited command is replaced. An edited command stays, and takes the new `Version` when the policy fields it edits are unchanged.

Accepted tenants are the command's list, not a store draft. Add and remove change that list on the page. Domain lookup stays `ResolveEntraTenant`. The action records the tenant or the error. The page applies that result to its command. Cancel discards the command edits, including tenant adds.

Acceptance:

- The store has no flattened response fields and no `DraftAcceptedTenants`.
- Opening Admin → Authentication fills the form from `Command.From`, including `Version` and the saved tenant list.
- Cancel restores that snapshot. Save sends the command and the form shows the saved policy and version.
- An assistant-name or key-policy save while an authentication edit is open keeps the edit and moves its `Version` forward when the policy fields did not change. When the stored policy fields also change, the edit and its old `Version` stay.
- Look up, Add, and Remove change the command's tenant list. The saved response stays unchanged until Save.
- The passkey gate, drift line, and agent facts read the stored response.

## Description

Hardest conversion; do last.

- `SiteSettingsState` holds `GetSiteSettings.Response` whole instead of ~15 flattened fields.
- Remove `DraftAcceptedTenants` from the store. Tenant add/remove edit
  `Command.EntraAcceptedTenants` on the working copy; tenant lookup/resolution stays an action
  whose result the page applies to its copy.
- `Version`, `AssistantName`, `XaiByokMode`, `Configuration*` are not on
  `ISiteSettingsDetails` — handle per the decision made in the generator task.
- Replace `SyncDraftFromState()` and the repeated `Command.EntraAcceptedTenants = [..]` lines.
- Consider adding Cancel now that reset is cheap.

## Depends on

- 489-004

## Checklist

- [x] State holds whole Response; update all readers
- [x] Tenant draft out of store
- [x] Page conversion
- [x] Tests

## Results

`SiteSettingsState` stores `GetSiteSettings.Response`. Fetch assigns it. A successful authentication, assistant-name, or key-policy save replaces it through `ReviseSiteSettings`, because `Version` is get-only and `Configuration*` stay on the response. `DraftAcceptedTenants` and the add/remove actions are gone. Add and remove edit `Command.EntraAcceptedTenants` on the page. `ResolveEntraTenant` records `ResolvedTenant` or `AcceptedTenantsMessage` and increments `TenantLookupCount`. The page applies that result once.

`UpdateSiteSettings.Command.From` runs on load, Cancel, and after the page's own successful save. `ShouldReRender` replaces an unedited command. An edited command stays and takes the new `Version` when the policy fields are unchanged. A lookup does not replace the command. The passkey gate, the drift line, and agent page facts read the stored response. A missing snapshot writes JSON nulls, not the type defaults.

### How to validate

Smoke: open Admin → Authentication. The form shows the saved policy, version, and tenant list. Change a checkbox or add a tenant, then Cancel. The form matches the snapshot again. Save. The form shows the saved policy and the new version. With an unsaved checkbox change, save the assistant name on Admin → AI. The checkbox stays and the authentication version moves. Look up a domain, add a tenant id, and remove a row. Those edits stay on the form until Save.

Expect: the store's response tenant list does not change until Save. Cancel drops the lookup, the added id, and the checkbox edit. A missing snapshot does not report version 0, Soft, Restricted, Akili, or Allow.

Gates:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/authentication-page-draft-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file. CI runs the project. From that project directory: `dotnet test -c Release --no-build --no-restore -- --filter-class AuthenticationDraft_Should_` — passed, 7 tests.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/authentication-tenant-lookup-tests.cs` — not a standalone runfile. Same invocation with `--filter-class ResolveEntraTenant_Action_Should_` — passed, 4 tests.
- `tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs` — not a standalone runfile. Same invocation with `--filter-class AiPage_Should_` — passed, 9 tests.
- `tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-relay-tests.cs` — not a standalone runfile. The class is partial and boots Aspire, so the run filtered the edited method: `dotnet test -c Release --no-build --no-restore -- --filter-method Page_Context_Carries_Profile_And_Site_Settings_Records` — passed, 1 test.
- Covering page renders, not edited: `--filter-class AssistantSettingsPlacement_Should_` — passed, 2 tests. `--filter-class AuthenticationPage_Should_` — passed, 3 tests.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). `analyzer-suppressions` passed: 843 suppressions, none above the baseline.

### Review disposition

- Rounds: 1. Effort 3 (by-diff budget, 1301 lines). Roster: general.
- Final counts: bug 0, suggestion 0, nit 1 (wontfix). Open: 0.
- Disposition: **accepted-exceptions**. M1 (nit): the agent-facts JSON is built in two steps and writes `[]` tenants with no snapshot. Closed wontfix because the behavior meets acceptance and `hasSnapshot: false` disambiguates; it can ride along with the next edit to `site-settings-state.agent-page-facts.cs`.
- Gates were not re-run in review. The recorded build, filtered test runs, and audit above cover the change.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Implementation: Grok 4.7, 2026-11-23. The store holds the fetched response. The authentication page copies it with `From`, reconciles a later response in `ShouldReRender`, and applies lookup results to the command. Cancel restores the snapshot.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20904-d9f3-7000-ab92-51bfd4a49d31, max-turns 500, Done — 2026-11-23T15:53:52Z
- Review oracle: Claude Opus 5.5 (claude-opus-5-5), headless `ganda task work` review node, effort 3, roster general, turn cap 200. Round 1: 0 bugs, 0 suggestions, 1 nit (wontfix). Disposition accepted-exceptions — 2026-11-23.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session d397954f-7753-45c4-b6fe-81c2e69cb29c, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-23T15:56:53Z

## Parent

- 489

## Review

- Round 1: Done
