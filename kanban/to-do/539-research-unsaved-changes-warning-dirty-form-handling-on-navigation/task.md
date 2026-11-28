# Research: unsaved-changes warning / dirty form handling on navigation

## Description

Research only. Stop forms from silently losing data when the user edits and then navigates away.

Track dirty state per form (edited vs. pristine, and reset after a save). When the form is dirty, show a clear confirmation on in-app navigation: "You have unsaved changes. Leave without saving?" Use the browser's `beforeunload` as a fallback for tab close, reload, and external navigation. Prefer auto-save or drafts where possible, so the prompt is rarely needed. Avoid silent data loss.

Filed 2026-11-29 at Steven's request (via Amina). Not launched.

## Requirements

1. **Hook Blazor in-app navigation.** Document how to hook `NavigationManager.RegisterLocationChangingHandler` and `NavigationLock` with `ConfirmExternalNavigation`.
2. **Dirty state and form components.** Document how dirty state integrates with `EditContext` and our form components.
3. **Confirm UX vs `beforeunload`.** Compare the FluentUI confirm-dialog pattern with native `beforeunload` limits (browser-controlled generic text).
4. **Auto-save or drafts.** Cover where auto-save or drafts fit (local storage versus server drafts), so the prompt is rarely needed.
5. **Reusable design plus follow-ups.** Propose a reusable component or service, plus follow-up implementation tasks (proposed, not filed).
6. **Report, do not implement.** The deliverable is a findings report in the task folder plus the proposed follow-up tasks. Do not implement the warning, dirty tracking, auto-save, or drafts in this task.

## Out of scope

- Implementation of the unsaved-changes warning, dirty tracking, auto-save, or drafts
- Filing the follow-up implementation tasks (propose them only)

## Checklist

- [ ] Document how to hook Blazor in-app navigation (`NavigationManager.RegisterLocationChangingHandler` / `NavigationLock` with `ConfirmExternalNavigation`)
- [ ] Document how dirty state integrates with `EditContext` and our form components
- [ ] Compare the FluentUI confirm-dialog pattern vs native `beforeunload` limits
- [ ] Recommend where auto-save or drafts fit (local storage vs server drafts)
- [ ] Propose a reusable component or service plus follow-up implementation tasks (proposed, not filed)
- [ ] Write the findings report in the task folder — no implementation

## Session

- Created: 2624530 (2026-11-29)

## Notes

- Preferred in-app confirm copy: "You have unsaved changes. Leave without saving?"
- `beforeunload` is a fallback for tab close, reload, and external navigation (browser-controlled generic text)
- Prefer auto-save or drafts so the prompt is rarely needed
- Filed only; not launched. Do not start until Steven says so.
