# Settings page: rebuild on the shared Fluent components, delete hand-rolled markup, and make actions-are-FluentButton the rule

## Description

Steve, 2026-09-16 (screenshot of `/Settings`): "Delete", "Create a passkey", "Add an existing
passkey", and "Link Microsoft 365" are link-styled text. The rest of the SPA uses
`FluentButton`. The Settings page is the **only** page in `web-spa` with raw `<button>` elements
(6) and it carries its own `twe-settings__*` CSS vocabulary (muted, link, field, cred-list,
actions, …) instead of the shared components. There is a style guide page
(`features/style-guide/pages/StyleGuidePage.razor`) with the canonical button appearances
(Primary, Outline, Subtle, Transparent) and a `--twe-danger` token, shared elements under
`components/elements` (`Card`, `StatusBadge`, `Text`), and a Fluent-based
`features/identity/pages/passkeys-page/PasskeysPage.razor` that already renders a passkey list
with `FluentButton` actions. Settings duplicated that by hand.

The rule task 231 wrote into `skills/tw-blazor/SKILL.md` ("link-styled buttons are for inline row
actions") sanctioned this. That rule is **wrong** and is replaced by this task.

**Standard (locked):** every action is a `FluentButton` with a style-guide appearance; anchors /
`FluentAnchor` are for navigation only; no raw `<button>`, no page-local CSS vocabularies for
things the shared components already do. Reuse, don't hand-write.

## Requirements

- Rebuild `features/application/pages/SettingsPage.razor` (+ `.razor.cs`, and delete its
  `.razor.css` if it only serves the removed classes) using:
  - shared `Card` (components/elements) or the same list structure `PasskeysPage` uses for each
    credential; `Text` / Fluent typography for labels and muted text; `StatusBadge` where a state
    is shown.
  - `FluentButton` for every action: primary action of a section = `Appearance.Primary`
    ("Create a passkey", "Link Microsoft 365"), secondary = `Outline` ("Add an existing passkey"),
    destructive row actions ("Delete", "Unlink") = `Outline` styled with the `--twe-danger` token
    exactly as the style guide shows (add a danger example to the style guide if it lacks one —
    do not invent a page-local class).
  - The 229 and 230 behaviours stay: Link hidden when linked, Unlink disabled with hint when last
    credential, card title = account label, "Add an existing passkey" merge CTA.
- Extract the credential list + row actions into a shared component
  (`features/identity/components/CredentialList.razor` or similar) and use it from **both**
  `SettingsPage` and `PasskeysPage` so there is one implementation. If `PasskeysPage` is now
  fully redundant with Settings, say so in Results and propose removal (do not remove in this
  task).
- `skills/tw-blazor/SKILL.md`: replace the 231 "form vs inline action" rule with:
  "Actions are `FluentButton` with a style-guide appearance (Primary / Outline / Subtle /
  Transparent; danger via `--twe-danger`). Navigation is `FluentAnchor` / `TimeWarpNavLink`.
  Persisting a model is `EditForm` + `FluentButton` submit (RoleForm). Never raw `<button>` or
  page-local button/link classes; reuse `components/elements` and existing feature components
  before writing markup." Point at `StyleGuidePage.razor` as the reference.
- Add an analyzer or test guard so this cannot regress silently: a Jaribu test (or the existing
  page-prerender suite) that fails if any `.razor` under `web-spa/features` contains a raw
  `<button` (allow-list only `components/` internals if a shared element legitimately wraps one).
- Tests: update `settings-page-microsoft-365-tests.cs`, `protected-page-deep-link-tests.cs`,
  and SPA suites that select the old classes; select by `data-qa` (`CreatePasskey`,
  `AddExistingPasskey`, `LinkMicrosoft365`, `Unlink`, `DeletePasskey`).

## Checklist

- [ ] Settings rebuilt on shared elements + FluentButton; `twe-settings__*` vocabulary deleted
- [ ] Shared credential list component used by Settings and PasskeysPage
- [ ] Style guide has the danger button example; Settings uses it
- [ ] tw-blazor rule replaced; raw `<button>` guard test added and green
- [ ] 229/230 behaviours preserved (tests updated to data-qa selectors)
- [ ] `dev build` 0/0; `ganda repo audit` clean; SPA + prerender suites green
- [ ] Results and How to validate (before/after screenshot steps)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Inventory 2026-09-16: raw `<button>` only in `SettingsPage.razor` (6). `FluentButton` used by
  StyleGuidePage (9), ChooseMicrosoft365Page, LoginPage, PasskeysPage, AddPasskeyPrompt,
  AgentLinksPage, TimeWarpPage, TodoItemFormContainer, CounterPage, TestPage.
- Shared: `components/elements/{Card,StatusBadge,Text,TimeWarpNavLink}.razor`,
  `components/forms/FormContainer.razor`, `components/composites/AuthorizedFluentNavLink.razor`.
- Prior: 219-003 (AddPasskeyPrompt), 219-006/225/227 (Settings trimmed), 229 (card rules),
  230 (Add an existing passkey), 231 (Authentication page EditForm — its skill rule is superseded here).

## Results

_Pending._

### How to validate

_Pending._
