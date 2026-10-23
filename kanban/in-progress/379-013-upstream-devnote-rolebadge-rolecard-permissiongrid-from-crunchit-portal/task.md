# Upstream DevNote RoleBadge RoleCard PermissionGrid from Crunchit portal

## Description

A generated app should be able to show a developer callout, a role name on a status pill, one option in a role picker, and a granted-or-denied capability list, without each page inventing that chrome. The four components are markup plus isolated CSS. Callers pass the words. None of them knows a product's role names, and the generated code carries no client name.

## Checklist

- [x] `DevNote` callout: optional eyebrow, optional title, body slot, code chips in the body
- [x] `RoleBadge` renders the caller's role text on `StatusBadge`. The caller passes `BadgeStatus`. No built-in role-name map
- [x] `RoleCard` is one radio option. The parent owns selection and `OnSelect`
- [x] `PermissionGrid` renders caller-supplied groups. Granted and denied are not colour alone
- [x] Isolated CSS, `--twe-*` tokens, no `<style>`, no client name. Text on paper clears 4.5:1
- [x] Purpose and Design regions. Rows in `components/overview.md`. Style-guide cards. HtmlRenderer tests, including an encoded `<script>` label
- [x] `./bin/dev build` is 0 warnings and 0 errors. `./bin/dev template-smoke` passes. `ganda repo audit` passes. `git diff origin/master -- source tests` has no case-insensitive client-name match
- [x] Style-guide `sg-follow` sits on a native wrapper for the permission grid and the dev note
- [x] RoleCard Design and the overview row name the parent's roving tabindex and arrow keys. The sample sets tabindex 0 and -1
- [x] Granted text is pinned as `>Granted<`. The hidden state span is `twe-perm-grid__state`

## Goal (implementer restatement)

Bring four display components into the template. Generated code, including copy, comments, sample data, and class names, carries no source-product name.

`DevNote` is a native `div.twe-devnote` with `role="note"`. An icon tile shows `</>` and is hidden from assistive tech. `Eyebrow` and `Title` render only when they have text. `ChildContent` is the body and renders only when the caller passes it. A `code` element in that body gets a paper chip through `::deep`, because the caller authors the code element. A caller class is appended. Other unmatched attributes splat onto the root and can override `role`. The component wires no events.

`RoleBadge` is not a second pill. It renders `StatusBadge` and passes the role string through as the label, unchanged. A blank role passes an empty label so the badge does not fall back to the status name. `Status` chooses the colour and defaults to `Inactive`. There is no map from Owner, Admin, or Staff. `StatusBadge` gains unmatched-attribute passthrough so a caller `class` and `data-qa` land on the pill.

`RoleCard` is a native `button` with `type="button"` and `role="radio"`. `Selected` sets `aria-checked` and the selected modifier. The selected state shows a decorative check. `Description` renders only when it has text. `OnSelect` fires when the button activates. The component does not know its siblings and does not move selection. The parent supplies `role="radiogroup"` when the cards are a group, sets `Selected`, and handles `OnSelect`. The parent also owns the keyboard: a roving `tabindex` (`0` on the selected card, `-1` on the others) passed through unmatched attributes, and the arrow keys. `RovingSelection` and `SegmentedControl` are that pattern. A caller class is appended. Other unmatched attributes splat onto the button.

`PermissionGrid` is a native `div.twe-perm-grid`. `Groups` is a list of `PermissionGroup` (a label plus `PermissionItem` rows). A blank group label is omitted. Each row shows a decorative check or minus and the words Granted or Not granted, visually hidden, so the state is not colour alone. Denied rows use `is-denied`. An empty or null group list renders the root and no rows. A caller class is appended. Other unmatched attributes splat onto the root. The component wires no events and does not decide who is granted what.

Tokens: `--twe-on-positive` (`#14784a`). `--twe-positive` is 4.44:1 on white, so the granted mark cannot use it as text. Soft fills are an 8% mix of `--twe-purple` into `--twe-paper`. Denied text and the dev-note body use `--twe-ink-2`, because `--twe-muted` is under 4.5:1 on paper. Eyebrow and the selected check use `--twe-purple-deep`.

The style guide, after Status Badges, shows three role badges (Owner / Warning, Admin / Info, Staff / Inactive), three role cards with Owner selected, a two-group permission grid, and a dev note whose body contains `inline code`. The page contract is unchanged. The samples do not store a choice. Render tests pin the slots, the modifiers, attribute passthrough, and an encoded `<script>` label.

Acceptance: `./bin/dev build` is clean, `./bin/dev template-smoke` passes, `ganda repo audit` passes, and `git diff origin/master -- source tests` contains no case-insensitive match for the source product name.

Review round 1 left four open findings. This pass closes them. The permission grid and the dev note get their style-guide top margin from a native wrapper the page authors, because a page class on a child root does not match under CSS isolation. `RoleCard`'s Design region and the overview row name the parent's roving tab stop and arrow keys, and they point at `RovingSelection` and `SegmentedControl`. The style-guide sample sets `tabindex` `0` on the selected card and `-1` on the others, and it still does not move selection. The granted-row test pins the word Granted apart from Not granted. The visually hidden state span is `twe-perm-grid__state`. The capability text stays `twe-perm-grid__label`.

## Results

`DevNote` is a native `div.twe-devnote` with `role="note"`. The icon tile shows `</>` and is `aria-hidden`. `Eyebrow` and `Title` render only when they have text. `ChildContent` is the body. A `code` element in that body is styled with `::deep`. A caller class is appended. Other unmatched attributes splat onto the root and can override `role`.

`RoleBadge` renders `StatusBadge`. The label is the caller's role text, unchanged. A blank role passes an empty label, so the pill does not fall back to the status name. `Status` defaults to `Inactive`. There is no Owner / Admin / Staff map. `StatusBadge` now splats unmatched attributes onto its root, so `class` and `data-qa` land on the pill.

`RoleCard` is a native `button` (`type="button"`, `role="radio"`). `Selected` sets `aria-checked` and `is-selected`, and shows a decorative check. `Description` renders only when it has text. `OnSelect` runs when the button activates. The parent owns the group, the selected item, the roving tabindex (`0` / `-1` through unmatched attributes), and the arrow keys. `RovingSelection` and `SegmentedControl` are that pattern. The card does not know its siblings.

`PermissionGrid` renders `PermissionGroup` rows. A null or empty list renders the root and no rows. A blank group label is omitted. Each row has a decorative check or minus and the words Granted or Not granted in `twe-perm-grid__state`, visually hidden. The capability text is `twe-perm-grid__label`. Denied rows use `is-denied`.

New token: `--twe-on-positive` (`#14784a`). `--twe-positive` is 4.44:1 on white, so the granted mark uses `--twe-on-positive`. Denied text, the dev-note body, and the role-card description use `--twe-ink-2`. Eyebrows and the selected check use `--twe-purple-deep`. Soft fills are an 8% mix of `--twe-purple` into `--twe-paper`.

The style guide, after Status Badges, shows role badges Owner / Warning, Admin / Info, and Staff / Inactive (`data-qa="StyleGuideRoleBadges"`), three role cards with Owner selected (`data-qa="StyleGuideRoleCards"`, `tabindex="0"` on Owner and `tabindex="-1"` on the others), a two-group permission grid, and a dev note whose body contains `inline code`. `data-qa="StyleGuidePermissionGrid"` and `data-qa="StyleGuideDevNote"` sit on native `div.sg-follow` wrappers the page authors, so the top margin matches under CSS isolation. The samples do not store a choice and do not move selection. The StyleGuidePage contract is unchanged.

HtmlRenderer tests pin the slots, the modifiers, attribute passthrough, and an encoded `<script>` label. They do not see colour or contrast. A signed-in Style Guide was not opened in a browser.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class DevNote_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class RoleBadge_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class RoleCard_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class PermissionGrid_Should_
```

**Expect:** `DevNote_Should_` 4 passed, 0 failed. A note with an eyebrow, a title, and a body renders `twe-devnote`, `role="note"`, the icon tile, the three slots, and a `code` element, and it has no `<style`. A whitespace eyebrow and title omit those slots and the body. `caller-mark` and `data-qa="note-mark"` land on the root, and `role="note"` stays. A title of `<script>` stays encoded. `RoleBadge_Should_` 5 passed, 0 failed. Owner with Warning renders `twe-badge--warning` and the word Owner, and the word Warning is absent under a case-sensitive check. `API Admin` stays `API Admin`. A blank role uses `twe-badge--inactive` and does not show the word Inactive. Staff with `caller-mark` and `data-qa="role-mark"` puts both on the pill. A role of `<script>` stays encoded. `RoleCard_Should_` 4 passed, 0 failed. A selected Owner is a `type="button"` radio, `aria-checked="true"`, `is-selected`, with the description and `twe-role-card__check`. An unselected Staff with a blank description is `aria-checked="false"`, omits the check and the description, and has no `is-selected`. `caller-mark`, `data-qa="card-mark"`, and `tabindex="0"` land on the button. A title of `<script>` stays encoded. `PermissionGrid_Should_` 5 passed, 0 failed. The sample shows Work and Administration, View assigned work, Delete records, the element text `>Granted<`, Not granted, `twe-perm-grid__state`, `is-denied`, and an `aria-hidden` mark, and it has no `<style>` and no `twe-perm-grid__name`. An empty list and a null list render the root and no rows. A blank group label is omitted and the row Alpha still shows `>Granted<`. `caller-mark` and `data-qa="grid-mark"` land on the root. A label of `<script>` stays encoded and the row says Not granted.

**Automated gate**

```bash
./bin/dev build
./bin/dev template-smoke
ganda repo audit
git diff origin/master -- source tests | rg -i crunchit || echo NO_CLIENT_NAME
```

`dev build`: 0 warnings, 0 errors (00:00:07.59). `dev template-smoke`: `Template smoke SUCCEEDED`. SmokeDefault OK. create-role 5/5, weather 5/5, hello 2/2, web-jaribu-tests 405/405, api-jaribu-tests 9/9, timewarp-testing-tests 14/14. `ganda repo audit`: Passed 33, Failed 0, Skipped 1 (self-hosted, public repository). The client-name search prints `NO_CLIENT_NAME`.

### Review

Round 1 ran at effort 3 with one general reviewer. Counts at merge: bug 1 open, suggestion 1 open, nit 2 open. This fix pass addresses all four. Round 2 has not run, so disposition stays with the host.

- M1 (bug): `sg-follow` is on a native wrapper for the permission grid and the dev note. The page scope `.sg-follow[b-iy1milohjd]` can match those divs. `data-qa` moved with the class.
- M2 (suggestion): the RoleCard Design region and the overview row name the parent's roving tabindex and arrow keys, and they point at `RovingSelection` and `SegmentedControl`. The style-guide sample sets `tabindex="0"` on the selected card and `tabindex="-1"` on the others. The sample still does not handle arrow keys or store a choice.
- M3 (nit): the granted assertions use `>Granted<`, which does not match `Not granted`.
- M4 (nit): the hidden state span is `twe-perm-grid__state` in the Razor file and the CSS file. The render test requires that class and rejects `twe-perm-grid__name`.

The four render classes were re-run after the fix (18/18 passed). `dev build`, `template-smoke`, `ganda repo audit`, and the client-name search were re-run and passed. A signed-in Style Guide was not opened in a browser.

Operator note: the task branch was reset from `origin/feature/379-portal-rest` to `origin/master`. It shares hunks with that feature branch (StatusBadge, StyleGuidePage, tokens.css, overview.md), so the PR target is an operator call.

Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Implementation: implementer-grok (2026-10-24). The claim worktree started at `origin/feature/379-portal-rest` (the 379-012 merge, five commits that are not on `origin/master`). Those commits stay on that feature branch. This task branch was reset to `origin/master` before the product commits so the pull request contains only 379-013.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-23T20:35:02Z
- Review oracle round 1: claude (claude-opus-5-5), general reviewer, effort 3, session not reported, 2026-10-24. 4 findings open (M1–M4); fix pass requested
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-23T20:39:13Z
- Fix pass: implementer-grok (grok, model grok-4.7), session not reported, 2026-10-24. Addressed M1–M4. Build, template-smoke, audit, and the four render classes are green. Round 2 has not run.

## Review

- Round 1: Blocked — open M1, M2, M3, M4
- Fix pass 2: addressed M1, M2, M3, and M4. Round 2 has not run.

## Notes

- Source (read-only): the portal's `DevNote`, `RoleBadge`, `RoleCard`, `PermissionGrid`, and `PermissionGroup` / `PermissionItem` under the portal web-spa `components/` folder.
- The portal badge mapped owner, admin, and staff to warning, info, and inactive, and it title-cased the word. This template does neither. The style guide passes those three words with an explicit status so the sample still reads, and a generated app's own role names are not guessed.
- The portal card and grid used a third-party icon set and a CSS builder on a display base class. Those are not ported. Marks are inline SVG. Selection is an `EventCallback` the page handles.
- Do not extend the StyleGuidePage contract. That is 379-017.
- Out of scope: the shell user card (379-015) and white-label tokens (379-014).
