# Move razor.cs members into @code; drop leftover mixin wording

## Description

Moxy mixins are gone (task 053). Stale "mixin" wording in agent skills still
claims `Page.mixin`. Hand-written members in `.razor.cs` belong in `@code`;
only `[Page]` / `[Authorize]` / `[CrossSliceReference]` stay in the `.cs` file
because `PageSourceGenerator` does not run on `.razor`.

## Requirements

- Fix `tw-blazor-layout` (and other live skill wording) so it names the Page
  source generator, not Moxy
- Move hand-written `.razor.cs` members into the paired `.razor` `@code`
- Leave `[Page]`, `[Authorize]`, `[CrossSliceReference]` on the `.cs` partial
- Delete `.razor.cs` that have no remaining class-level attributes
- Drop leftover `@page` on UserClaimsPage
- No behavior change

## Checklist

- [x] Skill/docs mixin wording
- [x] Move members on the 13 pages with fat code-behind
- [x] Fold non-page `.cs` into `@code` (or thin `[CrossSliceReference]` only)
- [x] Drop UserClaimsPage `@page`
- [x] Commit

## Results

`.razor.cs` files now hold only generator/analyzer class attributes. Hand-written
members live in `@code`. Deleted `SideNavigation`, `SideNavigationLink`, and
`ModalContainer` code-behind. Skills no longer say Moxy/`Page.mixin`.
`page-mixin.md` renamed to `page-attribute.md`. `web-spa` Release build 0/0.

### How to validate

```bash
# Remaining .razor.cs must be [Page]/[Authorize]/[CrossSliceReference] only
rg -l 'private |protected override|public async' \
  source/container-apps/web/projects/web-spa --glob '*.razor.cs'
# Expect: no matches

rg -n 'Moxy|Page\.mixin' skills/tw-blazor-layout/SKILL.md skills/tw-blazor/SKILL.md
# Expect: no matches

dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Release
# Expect: 0/0
```

## Session

- Implementation: grok 2026-08-13
