# Upstream Avatar initials circle from Crunchit portal

## Description

Port Crunchit's initials avatar into the template as a native `Avatar` element, with a pure
helper that works out the initials. The avatar is a circle in one fixed brand colour showing one
or two letters of a person's name.

**Decision (Steve 2026-10-22): port the native Avatar.** The template keeps `FluentAvatar` where
it already shows user photos: the shell `Profile.razor`, `ProfilePage.razor` and
`HomePage.razor`. Do not change those pages. The new `Avatar` is the initials-only circle
themed with our tokens. The shell user card (379-015) may use it later.

Crunchit's version is a plain `<span>` with one fixed fill, no image, and no size parameter. Its
initials take the first character of the first and last word, which splits surrogate pairs. It
has no ARIA, so a screen reader reads out a stray "BP".

No template page has an honest place for initials today, so the style guide is the only
adopter. The generated code contains no client name.

Follow the conventions of 379-001 through 379-005:

- A native root with a `twe-` class.
- A `Class` parameter and `CaptureUnmatchedValues` `AdditionalAttributes`, as `Card.razor` does.
- No `DisplayComponent` or `CssBuilder`.
- Isolated `.razor.css` with `--twe-*` tokens.
- Purpose and Design regions.
- A pure helper `.cs` with unit tests.
- Static `HtmlRenderer` render tests.

## Checklist

- [ ] `components/elements/Avatar.razor` and `.razor.css`:
  - Root: `<span class="twe-avatar …">`.
  - Parameters: `Name` (required), `Size` (`InitialsAvatarSize`: `Small` 24, `Medium` 32 by
    default, `Large` 56), `AriaHidden` (bool, default true), `AriaLabel`, `Class`, and
    `AdditionalAttributes`.
  - The enum is named `InitialsAvatarSize` so it does not clash with FluentUI's `AvatarSize`.
- [ ] `components/elements/avatar-initials.cs` provides `AvatarInitials.From(string? name)`:
  - It takes the first text element of the first and last word, uppercased.
  - A single word gives one letter. Null, empty or whitespace gives `""`.
  - It uses `StringInfo` or `Rune` so a surrogate pair is never split.
  - It does not special-case email-style names.
- [ ] Helper unit tests (Jaribu and Shouldly), one per rule:
  - Two words: "Ada Lovelace" gives "AL".
  - Three words use the first and last.
  - A single word gives one letter.
  - Lowercase input is uppercased.
  - Extra spaces and tabs are ignored.
  - Null, empty and whitespace give an empty string.
  - "𠮷野" keeps the whole code point.
  - "a@b.com" gives "A".
- [ ] ARIA:
  - By default the avatar is `aria-hidden="true"` with no role, because the name is usually
    shown beside it.
  - When `AriaHidden` is false, it renders `role="img"` with `aria-label`, using `AriaLabel` or
    `Name`.
  - Empty initials render an empty circle that is always `aria-hidden`.
- [ ] CSS:
  - `inline-flex`, centred, `border-radius: 50%`, `flex: none`.
  - `background: var(--twe-primary)`, `color: var(--twe-on-primary)` (or the existing
    equivalent), `font-size: var(--twe-text-helper)`, weight 600.
  - `.twe-avatar--small` and `.twe-avatar--large` set width, height and font-size.
  - Add `--twe-avatar-size-*` tokens to `tokens.css` only if they are needed.
  - No hex values, no `<style>`.
- [ ] Use one fixed fill, as Crunchit does. Do not pick a colour by hashing the name, and add no
      `ImageUrl`; photos stay `FluentAvatar`. Record both choices in the Design region.
- [ ] Add an "Avatars" card to the style guide, after "Segmented Control". Show:
  - all three sizes;
  - a single-word name, a two-word name, a surrogate-pair name and a very long name;
  - one avatar beside a name in a row.

  Do not extend the StyleGuidePage contract (379-017).
- [ ] Add a row to `components/overview.md` if it lists components.
- [ ] Render tests in `tests/container-apps/web/web-spa-integration-tests/components/avatar-render-tests.cs`:
  - Renders the initials.
  - The default size has no modifier class.
  - `Large` adds its modifier class.
  - `Class` and the additional attributes are merged.
  - An empty name renders an empty circle with `aria-hidden`.
  - The decorative mode is `aria-hidden` with no role.
  - The labelled mode has `role="img"` and an `aria-label`.
  - The root carries `twe-avatar`.
- [ ] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

## Notes

- Crunchit source is under
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`:
  - `components/Avatar.razor(.css)`.
  - Usages: `layout/NavMenu.razor` (the sidebar user card), `features/staff/pages/StaffPage.razor`
    (the table and the drawer header), and the style guide.
- Template targets:
  - `source/container-apps/web/projects/web-spa/components/elements/`
  - `features/style-guide/pages/StyleGuidePage.razor`
  - `wwwroot/css/tokens.css`
- Defaults chosen when the spec was written (2026-10-22):
  - One fixed fill.
  - No image.
  - First character only for non-letter names.
  - The `InitialsAvatarSize` name.
- Out of scope: the shell user card (379-015), RoleBadge and RoleCard (379-013), and 379-017.
