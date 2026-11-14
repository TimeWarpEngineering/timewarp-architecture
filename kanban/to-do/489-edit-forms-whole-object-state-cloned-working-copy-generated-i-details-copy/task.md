# Edit forms: whole-object state, cloned working copy, generated I*Details copy

## Description

Edit forms drift from the contracts convention (`tw-web-api-contracts` → `references/mutability.md`).
Stores flatten the fetched `Get*.Response` into separate properties, and pages rebuild the
`Command` with hand-written sync methods (`SyncCommandFromState`, `SyncDraftFromState`,
`SyncAssistantDraft`, `SyncByokDraft`). Some pages keep the unsaved working copy inside the
store (`SiteSettingsState.DraftAcceptedTenants`, `RoleState.DraftPermissionIds` / `DraftAgentScopes`,
`PrincipalState` role/scope drafts).

Target pattern (decided 2026-11-14, research session; Copic `ModuleForm.razor` is prior art):

1. **State holds the whole fetched object.** E.g. `ProfileState.Profile : GetProfile.Response`,
   set only inside the fetch / update action handlers. No flattened mirror properties.
2. **Mutable Responses get a real clone.** TimeWarp.State's transaction behavior clones state
   before every handler for rollback. `ICloneable => this` is only safe for immutable contracts;
   a Response with `I*Details` setters held in a store must deep-copy.
3. **The form edits a working copy outside the store.** On entering edit (or when the id
   changes, not on every `OnParametersSet`), the page builds the Command with one generated
   copy: `UpdateX.Command.From(IXDetails)`. The copy lives in the component or inside a pending
   Action (Copic style). Keystrokes never touch store state — mutating state outside an action
   is a violation.
4. **Cancel** re-copies from the store. **Submit** dispatches the Command; the handler refreshes
   the stored Response.
5. View-only display must not bind the live store object to inputs either (Copic's View mode
   relied on `ReadOnly`; don't).

Children implement this in order. Prefill for WebMCP is a separate task that depends on the
Profile conversion.

## Requirements

- No store flattens a `Get*.Response` that an edit form reads.
- No hand-written field-by-field sync methods between store and Command.
- No unsaved form drafts held in stores.
- Clone and `From(I*Details)` are generated, not hand-written.

## Checklist

- [ ] 001 generator for clone + `From(I*Details)` copy, plus pattern doc
- [ ] 002 Profile reference conversion
- [ ] 003 Role edit form + permission/scope drafts out of store
- [ ] 004 AiPage assistant-name and BYOK forms
- [ ] 005 AuthenticationPage + SiteSettingsState
- [ ] Follow-up in timewarp-flow: update `tw-web-api-contracts` `references/mutability.md` with
      the store/clone/working-copy rules (skill source lives there, not in this repo)

## Notes

Drift survey 2026-11-14 (paths under `source/container-apps/web/projects/web-spa/features`):

| Form | Status |
|---|---|
| `profiles/pages/ProfilePage.razor:101` | drift — flattened `ProfileState`, `SyncCommandFromState` |
| `admin/site-settings/pages/AuthenticationPage.razor:173` | drift — flattened `SiteSettingsState`, `SyncDraftFromState`, `DraftAcceptedTenants` in store, `Version` copied by hand |
| `admin/ai/pages/AiPage.razor:274` | drift — `SyncAssistantDraft` + shadow `Synced*` dirty tracking, mutates field during render |
| `admin/ai/pages/AiPage.razor:400` | drift — `SyncByokDraft`, same shape |
| `admin/roles/pages/RoleDetailPage.razor`, `components/RolePermissionEditor.razor`, `admin/principals/pages/PrincipalDetailPage.razor` | draft-in-store via actions |
| `admin/roles/components/RoleForm.razor` | conforms (create) — reference |
| `feedback/pages/FeedbackListPage.razor:349` | conforms; action takes loose args instead of the Command (minor) |
| xAI key forms, `to-do/components/TodoItemForm.razor` | n/a |

No form binds a store object directly today. No Mapperly/AutoMapper/clone helper exists in
`source/`. `contract-agreement-analyzer.cs` in `timewarp-architecture-convention-analyzers` is a
candidate home for the generator (unverified).
