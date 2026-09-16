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

- [x] Settings rebuilt on shared elements + FluentButton; `twe-settings__*` vocabulary deleted
- [x] Shared credential list component used by Settings and PasskeysPage
- [x] Style guide has the danger button example; Settings uses it
- [x] tw-blazor rule replaced; raw `<button>` guard test added and green
- [x] 229/230 behaviours preserved (tests updated to data-qa selectors)
- [x] `dev build` 0/0; `ganda repo audit` clean; SPA + prerender suites green
- [x] Results and How to validate (before/after screenshot steps)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok (2026-09-17) — task-233 worktree

## Notes

- Inventory 2026-09-16: raw `<button>` only in `SettingsPage.razor` (6). `FluentButton` used by
  StyleGuidePage (9), ChooseMicrosoft365Page, LoginPage, PasskeysPage, AddPasskeyPrompt,
  AgentLinksPage, TimeWarpPage, TodoItemFormContainer, CounterPage, TestPage.
- Shared: `components/elements/{Card,StatusBadge,Text,TimeWarpNavLink}.razor`,
  `components/forms/FormContainer.razor`, `components/composites/AuthorizedFluentNavLink.razor`.
- Prior: 219-003 (AddPasskeyPrompt), 219-006/225/227 (Settings trimmed), 229 (card rules),
  230 (Add an existing passkey), 231 (Authentication page EditForm — its skill rule is superseded here).

## Results

Settings is rebuilt on shared `Card` + `CredentialList` + `FluentButton`. The page-local
`twe-settings__*` vocabulary and all six raw `<button>` elements are gone. Actions use
style-guide appearances: Primary for "Create a passkey" and "Link Microsoft 365", Outline
for "Add an existing passkey", Outline + global `twe-button-danger` (`--twe-danger`) for
Delete / Unlink. StatusBadge marks active credentials. 229/230 behaviour is unchanged
(Link hidden when linked, Unlink disabled with "Add a passkey first" when last credential,
card title = account label when one Entra account is linked).

**Shared list:** `features/identity/components/CredentialList.razor` is used by Settings
(passkeys + Microsoft 365) and PasskeysPage (passkeys on this account).

**PasskeysPage is not fully redundant.** `/Passkeys` remains the Developer-gated WebAuthn
ceremony playground (register / sign-in via `PasskeyCeremonyClient`). Settings is the
product account page (passkeys + Microsoft 365 link/unlink). Propose a later task to drop
the list from PasskeysPage *or* retire `/Passkeys` if operators only need Settings — do
not remove it on this id.

**Style guide:** Buttons card now includes Outline + `Class="twe-button-danger"` (data-qa
`DangerOutline`). The class lives in `wwwroot/css/app.css` (FluentButton shadow DOM /
Exception A) so Settings does not invent a page-local class.

**Skill:** `skills/tw-blazor/SKILL.md` 231 "link-styled buttons are for inline row
actions" rule replaced with the FluentButton / FluentAnchor / EditForm rule; reference is
`StyleGuidePage.razor`.

**Guard:** `tests/.../razor-raw-button-guard-tests.cs` fails if any `.razor` under
`web-spa/features` contains `<button`. `components/` is outside the scan. Allow-list is
empty.

**data-qa:** `CreatePasskey` (was `CreatePasskeyOnAccount`), `AddExistingPasskey`,
`LinkMicrosoft365`, `Unlink` (was `UnlinkMicrosoft365`), `DeletePasskey`. Hint remains
`UnlinkMicrosoft365Hint`.

**Not in this task:** Admin `/Admin/Authentication` still uses a `twe-settings__*`
section vocabulary (no raw buttons). Out of scope.

### Files changed

- `source/container-apps/web/projects/web-spa/features/identity/components/CredentialList.razor` (+ `.razor.css`)
- `source/container-apps/web/projects/web-spa/features/application/pages/SettingsPage.razor` (+ `.razor.cs`)
- `source/container-apps/web/projects/web-spa/features/identity/pages/passkeys-page/PasskeysPage.razor` (+ `.razor.cs`)
- `source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor`
- `source/container-apps/web/projects/web-spa/components/elements/Card.razor` (`TitleDataQa`, unmatched `data-qa`)
- `source/container-apps/web/projects/web-spa/wwwroot/css/app.css` (`.twe-button-danger`)
- `skills/tw-blazor/SKILL.md`
- `tests/container-apps/web/web-spa-integration-tests/features/application/razor-raw-button-guard-tests.cs`
- `tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs`

### Test outcomes

- `./bin/dev build` — 0 Warning(s), 0 Error(s)
- `ganda repo audit` — pass; 2 pre-existing advisory warnings (memsearch-scaffold, vscode-window-icon)
- `web-spa-integration-tests` — 35 passed, 1 skipped (pre-existing weather quarantine)
- `ProtectedPageDeepLink` prerender — 12 passed (229/230 HTML + new data-qa)

Live browser screenshots were not taken in this session (no Aspire `dev run`). Prerender HTML
is the automated proof; manual UI steps are below.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class RazorRawButtonGuard
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class ProtectedPageDeepLink
./bin/dev run
```

Then, signed in as a member with a passkey:

1. Open `/Settings`.
2. Open `/StyleGuide` (Developer) and compare the Buttons row, including Danger.
3. Open `/Passkeys` (Developer) and confirm the same credential list + Delete.

**Expect**

- Guard test: 1 passed. Features `.razor` files contain no raw `<button`.
- Prerender: 12 passed. `/Settings` HTML contains `data-qa="CreatePasskey"` and
  `data-qa="AddExistingPasskey"`. When Microsoft 365 is offered and unlinked:
  `LinkMicrosoft365` present. When linked with a remaining passkey: `Unlink` present and
  not disabled, no `UnlinkMicrosoft365Hint`, card title is the account label. When Entra
  is the last credential: `Unlink` disabled and `UnlinkMicrosoft365Hint` / "Add a passkey first".
- `/Settings` UI: "Create a passkey" is a Primary FluentButton (not link-styled text).
  "Add an existing passkey" is Outline. Delete / Unlink are Outline in the danger color
  (`--twe-danger` / `#c2342a`), matching Style Guide → Buttons → Danger.
- `/Passkeys` still has Register / Sign in ceremony buttons plus the shared list.

**Automated gate**

```bash
./bin/dev build
# expect: 0 Warning(s), 0 Error(s)
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release
# expect: 35 passed, 1 skipped
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class ProtectedPageDeepLink
# expect: 12 passed
ganda repo audit
# expect: Repository passes (advisory warnings only)
```

**Depends on:** `./bin/dev run` (Aspire) for the manual UI steps; prerender tests use
in-proc `HostGraphFactory` and do not need Aspire.

**Not in scope:** live WebAuthn create/delete in a real authenticator; retiring `/Passkeys`.

