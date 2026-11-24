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

## Checklist

- [ ] `references/mutability.md` states the five points with the one example
- [ ] Stale passages in `mutability.md` and `SKILL.md` revised against the guide
- [ ] `SKILL.md` links the section and names TWA0047 to TWA0049
- [ ] `skills/tw-blazor/SKILL.md` checked; fixed or recorded as correct
- [ ] Present tense, no client names, no history
- [ ] `./bin/dev lint-skills` passes

## Session

- Created: 9e8b77bb-fa9d-4469-930c-9999fd3c4875 (2026-11-24), Claude cockpit in timewarp-flow

## Notes

- Replaces timewarp-flow task 228 (archived 2026-11-24). 489's checklist said to file the
  follow-up in timewarp-flow and not edit the skill from this repo. That was wrong: flow's
  `claude/skills/tw-web-api-contracts/` is a gitignored synced copy of
  `skills/tw-web-api-contracts/` here, and `dev lint-skills` exists only in this repo.
- Source material: `documentation/developer/guides/edit-form-details-copy.md`,
  `source/analyzers/shared/details-copy-classification.cs`, and the `kanban/done/489-*` Results.
