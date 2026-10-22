# Upstream Avatar initials circle from Crunchit portal

## Goal (implementer restatement)

The template gains a native `Avatar` for initials and leaves the existing photo avatars alone.
`Profile.razor`, `ProfilePage.razor`, and `HomePage.razor` keep `FluentAvatar`. The new circle
shows one or two letters of a person's name on one brand fill. It has no image and does not pick
a colour from the name. `AvatarInitials.From` takes the first text element of the first word and,
when there is another word, of the last word, uppercased. A single word yields one letter. Null,
empty, and whitespace yield an empty string. A surrogate pair stays whole. An email-shaped name
is an ordinary single word.

The root is a `<span class="twe-avatar">` with `Class` and unmatched attributes, the same shape
as `Card`. Sizes are `InitialsAvatarSize`: Small 24, Medium 32 with no modifier class, Large 56.
By default the circle is `aria-hidden` and has no role, because the name usually sits beside it.
When `AriaHidden` is false and the initials are non-empty, it is `role="img"` and the label is
`AriaLabel` or `Name`. Empty initials stay an empty hidden circle. Styles live in the isolated
stylesheet and use `--twe-*` tokens. The style guide, after Segmented Control, shows the three
sizes, a single word, two words, a surrogate-pair name, a very long name, and one circle beside
a name. `components/overview.md` lists the element. Helper tests and `HtmlRenderer` render tests
pin those rules. The build, template smoke, and repo audit pass, and the source and test diff
does not name the portal.

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

- [x] `components/elements/Avatar.razor` and `.razor.css`:
  - Root: `<span class="twe-avatar …">`.
  - Parameters: `Name` (required), `Size` (`InitialsAvatarSize`: `Small` 24, `Medium` 32 by
    default, `Large` 56), `AriaHidden` (bool, default true), `AriaLabel`, `Class`, and
    `AdditionalAttributes`.
  - The enum is named `InitialsAvatarSize` so it does not clash with FluentUI's `AvatarSize`.
- [x] `components/elements/avatar-initials.cs` provides `AvatarInitials.From(string? name)`:
  - It takes the first text element of the first and last word, uppercased.
  - A single word gives one letter. Null, empty or whitespace gives `""`.
  - It uses `StringInfo` or `Rune` so a surrogate pair is never split.
  - It does not special-case email-style names.
- [x] Helper unit tests (Jaribu and Shouldly), one per rule:
  - Two words: "Ada Lovelace" gives "AL".
  - Three words use the first and last.
  - A single word gives one letter.
  - Lowercase input is uppercased.
  - Extra spaces and tabs are ignored.
  - Null, empty and whitespace give an empty string.
  - "𠮷野" keeps the whole code point.
  - "a@b.com" gives "A".
- [x] ARIA:
  - By default the avatar is `aria-hidden="true"` with no role, because the name is usually
    shown beside it.
  - When `AriaHidden` is false, it renders `role="img"` with `aria-label`, using `AriaLabel` or
    `Name`.
  - Empty initials render an empty circle that is always `aria-hidden`.
- [x] CSS:
  - `inline-flex`, centred, `border-radius: 50%`, `flex: none`.
  - `background: var(--twe-primary)`, `color: var(--twe-on-primary)` (or the existing
    equivalent), `font-size: var(--twe-text-helper)`, weight 600.
  - `.twe-avatar--small` and `.twe-avatar--large` set width, height and font-size.
  - Add `--twe-avatar-size-*` tokens to `tokens.css` only if they are needed.
  - No hex values, no `<style>`.
- [x] Use one fixed fill, as Crunchit does. Do not pick a colour by hashing the name, and add no
      `ImageUrl`; photos stay `FluentAvatar`. Record both choices in the Design region.
- [x] Add an "Avatars" card to the style guide, after "Segmented Control". Show:
  - all three sizes;
  - a single-word name, a two-word name, a surrogate-pair name and a very long name;
  - one avatar beside a name in a row.

  Do not extend the StyleGuidePage contract (379-017).
- [x] Add a row to `components/overview.md` if it lists components.
- [x] Render tests in `tests/container-apps/web/web-spa-integration-tests/components/avatar-render-tests.cs`:
  - Renders the initials.
  - The default size has no modifier class.
  - `Large` adds its modifier class.
  - `Class` and the additional attributes are merged.
  - An empty name renders an empty circle with `aria-hidden`.
  - The decorative mode is `aria-hidden` with no role.
  - The labelled mode has `role="img"` and an `aria-label`.
  - The root carries `twe-avatar`.
- [x] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Results

`Avatar` is a native `<span class="twe-avatar">` under `components/elements/`. `Class` and unmatched
attributes land on that root. `AvatarInitials.From` returns the first text element of the first
word and, when another word exists, of the last word, uppercased through `StringInfo`. One word
yields one letter. Null, empty, and whitespace yield an empty string. A surrogate pair stays one
code point. An email-shaped name is one word. Every name uses `--twe-primary` on `--twe-on-primary`.
Photos stay on `FluentAvatar`. Medium is 32px and adds no modifier. Small is 24px
(`twe-avatar--small`, eyebrow type) and Large is 56px (`twe-avatar--large`, page-title type).
Those diameters stay in `Avatar.razor.css`. `tokens.css` is unchanged. The circle is `aria-hidden`
and has no role unless `AriaHidden` is false and the initials are non-empty; then it is `role="img"`
and the label is `AriaLabel`, or `Name` when that label is blank. Empty initials stay a hidden
circle. The style guide shows an Avatars card after Segmented Control. `components/overview.md`
lists the element. `ganda repo audit` was already green; nothing was turned off.

### How to validate

Smoke:

- `./bin/dev build` — completed successfully, 0 warnings, 0 errors.
- `./bin/dev template-smoke` — SmokeDefault OK, 0 warnings, 0 errors. Generated Jaribu
  aggregators 405/405, 9/9, and 14/14. Template smoke succeeded.
- `ganda repo audit` — 32 passed, 0 failed, 1 skipped.
- From `tests/container-apps/web/web-spa-integration-tests`: `dotnet test -c Debug --nologo -- --filter-class AvatarInitials_Should_`
  (8 passed) and `--filter-class Avatar_Should_` (7 passed).
- `git diff origin/master -- source tests | rg -i crunchit` — no match.

### Expect

- Style Guide, after Segmented Control, shows an Avatars card. The first row is Ada Lovelace at
  small, medium, and large. The second row is Ada (A), Ada Lovelace (AL), 𠮷野 (𠮷), and a long
  name (AW). The third row places a circle beside the name Ada Lovelace. That circle is hidden
  from assistive tech.
- Profile, the profile page, and Home still show `FluentAvatar`.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)
- Implement oracle: Avatar, AvatarInitials, the style-guide card, and render tests. Build, template-smoke, and audit are green. 2026-10-22.

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
