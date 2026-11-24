# tw-web-api-contracts skill: store snapshot, generated Clone and From, working copy

## Description

Task 489 (children 489-001 to 489-005, feature PR #662, merged 2026-11-23) settled how edit
forms relate to contracts. The `tw-web-api-contracts` skill does not say any of it, and parts
of it still say the older thing.

The skill is authored in this repo at `skills/tw-web-api-contracts/`. Other repos, including
timewarp-flow, hold synced copies (`.ganda-source` points here). So the edit belongs here.

What the skill must state, from the 489-001 Results and
`documentation/developer/guides/edit-form-details-copy.md`:

1. **The store holds the whole fetched `Get*.Response`.** No flattened mirror properties, and
   no unsaved form drafts in a store.
2. **A mutable details Response gets a real `Clone`.** TimeWarp.State's transaction behavior
   clones state before every handler. `Clone() => this` is legal only for an immutable
   contract. The generated `Clone` deep-copies lists, sets, dictionaries, and rank-1 arrays.
   Analyzers: TWA0047 (mutable details response whose `Clone` returns `this`), TWA0048
   (command or mutable response not `partial`), TWA0049 (partial command with no
   parameterless constructor).
3. **`I*Details` drives a generated `Update*.Command.From(I*Details)`.** The form edits that
   command as a working copy outside the store. The copy runs on load, on Cancel, and after
   the page's own successful save, not on every `OnParametersSet`. A later store response is
   reconciled in `ShouldReRender`: an unedited command is replaced, an edited one keeps its
   edits and takes the new `Version` when its own fields are unchanged.
4. **`Version` stays off the `I*Details` interface.** The generator declares
   `static partial void CopyBeyondDetails(Command, I*Details)` and the contract copies
   `Version` (or `RoleId`, and so on) from the concrete Response there.
5. View-only display does not bind the live store object to inputs.

## Requirements

- The guide and the code are the authority. Where this card and
  `documentation/developer/guides/edit-form-details-copy.md`,
  `source/analyzers/shared/details-copy-classification.cs`, or a converted form (for example
  the role edit form from 489-003) disagree, follow the guide and code and record the
  difference in `## Notes`.
- `skills/tw-web-api-contracts/references/mutability.md`: add the five points with one short
  example (a Response with `ICloneable`, a Command with `From`, and `CopyBeyondDetails`).
- Revise, do not only append. These passages predate 489. Read each against the guide and
  rewrite any that now misleads. A reader must not be able to conclude that an `EditForm`
  binds the store's Response object:
  - `mutability.md` "Why this matters": "Blazor `EditForm` binds directly; same type goes
    back on Create/Update".
  - `mutability.md` "Mutable (bindable / edit)": "`Get*` Response implements `I*Details` when
    loading data into an edit form" and "Blazor components bind against `I*Details`, not the
    concrete Response/Command type".
  - `mutability.md` "Binding the interface in Blazor".
  - `SKILL.md` overview paragraph ("mutable members on `I*Details` interfaces bind in
    `EditForm` without a separate view model").
  - `SKILL.md` interface steps 5 and 6 and the sentence under them that sends the reader to
    `mutability.md`.
  - `SKILL.md` "Response patterns" row "Editable load".
  - `SKILL.md` review checklist and the mistakes row "Separate Blazor view model".
  A passage that is still correct stays as it is.
- `SKILL.md` links the new `mutability.md` section where edit forms or `I*Details` are
  described, and names TWA0047, TWA0048, and TWA0049 where the other analyzer ids appear.
- Check `skills/tw-blazor/SKILL.md` for text that contradicts point 1 or point 3 (its
  `[ContractState]` `DraftRequests` example uses the role-state slice that 489-003 changed).
  Fix a contradiction if there is one. If it is still correct, say so in `## Results`.
- Skill text is present tense, with no client names and no history. Skills are public.
- `./bin/dev lint-skills` passes.
- Do not edit synced copies in other repos. After merge, a consumer runs
  `ganda skills update tw-web-api-contracts`; name that in `## Results`.

## Goal (implementer restatement)

Bring the `tw-web-api-contracts` skill in line with how edit forms work after 489: the store
keeps the whole fetched Response, a mutable details Response gets a generated deep `Clone`, the
form edits an `Update*.Command` built by the generated `From(I*Details)`, and `Version`-like
fields travel through `CopyBeyondDetails`, not the interface.

Done when:

- `references/mutability.md` states the five points with one example (Response, Command with
  `From`, `CopyBeyondDetails`), and its older passages no longer say the form binds the store's
  Response.
- `SKILL.md` overview, step 3 items 5 and 6, the sentence under them, the "Editable load" row,
  the checklist, and the "Separate Blazor view model" pitfall are revised; it links the new
  section and names TWA0047, TWA0048, TWA0049.
- `skills/tw-blazor/SKILL.md` no longer shows a form draft kept in a store (its
  `[ContractState]` example still listed `DraftRequests` on the role state).
- Present tense, no client names, no history; `./bin/dev lint-skills` passes.

## Checklist

- [x] `references/mutability.md` states the five points with the one example
- [x] Stale passages in `mutability.md` and `SKILL.md` revised against the guide
- [x] `SKILL.md` links the section and names TWA0047 to TWA0049
- [x] `skills/tw-blazor/SKILL.md` checked; fixed or recorded as correct
- [x] Present tense, no client names, no history
- [x] `./bin/dev lint-skills` passes

## Session

- Created: 9e8b77bb-fa9d-4469-930c-9999fd3c4875 (2026-11-24), Claude cockpit in timewarp-flow
- Implemented 2026-11-25 by the ganda task-work implement oracle (Claude Opus 5.5).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session a5e5f07c-992b-42d1-9ffd-3f485ecc0563, max-turns 500, Done — 2026-11-24T19:12:40Z
- Reviewer: Grok session 01a20ef9-9c19-71e3-894d-b01849941f8f (2026-11-25). Effort 2, roster general, round 1. Open findings M1, M2, M3 in `review/round-1/merged.md`.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20ef9-9c19-71e3-894d-b01849941f8f, max-turns 120, artifacts general.md, merged.md — 2026-11-24T19:20:58Z

## Review

- Round 1: Blocked — open M1, M2, M3
- Fix pass for round 1 (implement-fix-2, 2026-11-25): M1, M2, M3 fixed; see `### Fix pass (round 1 findings)`.

## Notes

- Replaces timewarp-flow task 228 (archived 2026-11-24). 489's checklist said to file the
  follow-up in timewarp-flow and not edit the skill from this repo. That was wrong: flow's
  `claude/skills/tw-web-api-contracts/` is a gitignored synced copy of
  `skills/tw-web-api-contracts/` here, and `dev lint-skills` exists only in this repo.
- Source material: `documentation/developer/guides/edit-form-details-copy.md`,
  `source/analyzers/shared/details-copy-classification.cs`, and the `kanban/done/489-*` Results.
- Card vs code, followed the code:
  - Point 3 says a later store response is reconciled in `ShouldReRender`. That is true for a
    page that reads the store (`AuthenticationPage.razor`). `RoleEditForm.razor` takes
    `GetRole.Response` as a parameter and reconciles in `OnParametersSet`, copying only when
    the id, name, or description differ from the values last copied. The skill names both.
  - The card's example asks for "a Response with `ICloneable`". The contract does not write
    `ICloneable`: a `partial` Response with a mutable details interface gets
    `: global::System.ICloneable` and `Clone()` from the details-copy generator. The example
    shows a `partial` Response with a comment saying so.
- `references/examples.md` "Get for edit" Response was `sealed class` (not `partial`), which is
  TWA0048 under the new rules. Made it `sealed partial class` and linked the new section.
- `SKILL.md` hit vally's 500-line limit; additions are kept short there and the detail lives in
  `mutability.md`. Two existing lines were reflowed to fit.

## Results

Changed:

- `skills/tw-web-api-contracts/references/mutability.md`: new section "Edit forms: store
  snapshot and working copy" with the five points, one example (`GetSiteSettings.Response`
  `partial`, `UpdateSiteSettings.Command` with generated `From` and `CopyBeyondDetails`), and a
  TWA0047–TWA0049 table. "Why this matters", "Mutable (bindable / edit)", and "Binding the
  interface in Blazor" now say the form binds the Command working copy, never the stored
  Response.
- `skills/tw-web-api-contracts/SKILL.md`: overview, the endpoint-centric paragraph, step 3
  items 5 and 6 and the sentence under them, the "Editable load" row, the checklist, and the
  "Separate Blazor view model" pitfall revised; links the new section; names TWA0047–TWA0049.
- `skills/tw-web-api-contracts/references/examples.md`: "Get for edit" Response is `partial`.
- `skills/tw-blazor/SKILL.md`: contradicted point 1. Its `[ContractState]` example listed
  `DraftRequests = [typeof(SetRolePermissions)]` on `RoleState`, and the hook table said
  `On<C>Loaded` seeds a draft. `RoleState` no longer uses `DraftRequests`. The example now
  matches the role state, `On<C>Loaded` stores the whole Response, and a paragraph says the
  store holds no unsaved form edits and the form keeps its working copy.

After merge, a consumer repo runs `ganda skills update tw-web-api-contracts` (and
`ganda skills update tw-blazor`) to refresh its synced copies. Synced copies in other repos were
not edited.

### Fix pass (round 1 findings)

- M1 fixed: `mutability.md` point 3 now states two rules. A store-reading page
  (`AuthenticationPage.AcceptPolicy`, in `ShouldReRender`) replaces a clean Command with `From`
  and, for a dirty one, copies only `Version` when the new response's form fields match the
  snapshot it was copied from. A child form (`RoleEditForm.OnParametersSet`) calls `From` when
  id, name, or description differ from the values last copied, and otherwise keeps the Command
  without patching `Version`.
- M2 fixed: `tw-blazor/SKILL.md` hook table says `On<C>Loaded` stores what the page reads; the
  paragraph limits the whole-Response rule to the edit-form query (`RoleSnapshot = response`)
  and allows a list hook to store rendered rows (`OnGetRolesLoaded` sets `RoleRows`). The
  `[ContractState]` snippet now lists `SetRoleAgentScopes`, matching `role-state.cs`.
  `mutability.md` point 1 is scoped to the edit-form query for the same reason.
- M3 fixed: `mutability.md` point 2 carries the guide's collection rules (elements shared except
  non-`string` `ICloneable` references; null nullable stays null; null non-nullable becomes
  empty; other collection types assigned as-is) and says TimeWarp.State copies fields itself
  when the object does not implement `ICloneable`. Point 3 says `From` follows the same rules.

Fix-pass gates:

- `./bin/dev build`: passed (Build completed successfully).
- Test files: none. Markdown-only change under `skills/`; no product code or test file changed.
- `./bin/dev lint-skills`: passed (10 skills linted, 10 passed; eval.yaml valid).
- `ganda repo audit`: passed (Repository passes all audit checks).

### How to validate

- Smoke: `./bin/dev lint-skills`, then read
  `skills/tw-web-api-contracts/references/mutability.md#edit-forms-store-snapshot-and-working-copy`
  and grep `skills/` for `binds directly` and `DraftRequests = [`.
- Expect: lint passes for all 10 skills (tw-web-api-contracts `SKILL.md` is 499 lines); the
  section lists the five points, the example, and TWA0047–TWA0049; both greps return nothing.

### Gates

- `./bin/dev lint-skills`: passed (10 skills, client-name scan passed).
- `./bin/dev build`: passed (0 warnings, 0 errors).
- Test files: none. The change edits only `skills/` markdown and this task file; no product code
  and no test file changed, so no runfile covers it.

- `ganda repo audit`: passed, 1 advisory warning (`vscode-window-icon`: the gitignored
  `.vscode/settings.json` was missing in this worktree). `ganda repo audit --fix` wrote that
  local file; rerun passed all checks (43 passed, 0 failed, 3 skipped). Nothing tracked changed.
