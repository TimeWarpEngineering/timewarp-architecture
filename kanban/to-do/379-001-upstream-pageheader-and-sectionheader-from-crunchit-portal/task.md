# Upstream PageHeader and SectionHeader from Crunchit portal

## Description

The Crunchit portal web-spa has two display components. Both are markup plus isolated CSS, with
no state and no code-behind.

- **PageHeader**: an h1 and an optional description on the left, and an `Actions` slot on the
  right. Parameters: `Title`, `Description?`, `Actions?`.
- **SectionHeader**: an h2 and an optional subtitle, and an `Aside` slot on the right (a hint or
  an action). Parameters: `Title`, `Subtitle?`, `Aside?`.

The template's page shell `components/TimeWarpPage.razor` already renders the page h1
(`.twe-page__title`) and the breadcrumb. A straight PageHeader port would give a page two h1s.

**Decision (Steve 2026-10-22):** there is no PageHeader component. `TimeWarpPage` gets optional
`Description` and `Actions` slots, so the title, description, and buttons render together in
the shell header:

```razor
<TimeWarpPage Title="Staff">
  <Description>Manage your team.</Description>
  <Actions><FluentButton …>Add staff</FluentButton></Actions>
  …page body…
</TimeWarpPage>
```

SectionHeader is ported as a component in
`source/container-apps/web/projects/web-spa/components/composites/`, next to `Section.razor`,
with namespace `TimeWarp.Architecture.Components`.

Generated code carries no client name. "Crunchit" or "crunchit" never appears in the diff. Use
the `twe-` CSS prefix and neutral sample text.

## Checklist

- [ ] `TimeWarpPage`: add optional `Description` and `Actions` `RenderFragment` parameters. Lay
      out the title and description on the left and the actions (wrapping) on the right, as in
      Crunchit's `PageHeader.razor.css`. Nothing changes when both slots are empty. Use isolated
      CSS in `TimeWarpPage.razor.css`, and the existing `--twe-*` tokens (`--twe-text-helper`,
      `--twe-space-*`, ink and muted).
- [ ] `SectionHeader.razor` and `SectionHeader.razor.css` in `components/composites/`. The root
      is a native `div class="twe-section-header"` with BEM children. It takes
      `[Parameter(CaptureUnmatchedValues = true)] AdditionalAttributes`, as `Card.razor` does.
      Do not port Crunchit's `DisplayComponent` or `CssBuilder`. Replace `--crunchit-*` tokens
      with `--twe-*` ones, and add `--twe-text-section-title` to `wwwroot/css/tokens.css` if
      nothing fits. Check hard-coded px values against the spacing tokens.
- [ ] Use CSS isolation only. No `<style>` element and no Tailwind (`tw-blazor-css-strategy`).
      Neither component has FluentUI children, so no `::deep` is needed.
- [ ] The components wire no events. Callers pass buttons, and the buttons dispatch
      TimeWarp.State actions (`tw-blazor`). Use FluentUI v5 `FluentButton` in examples. Do not
      port Crunchit's custom `Button Variant=`.
- [ ] Add Purpose and Design regions (TWA0004) to every new or changed file that needs them.
      Any `.cs` file is kebab-case (TW0001).
- [ ] Adopt the slots in `features/to-do/pages/TodoItemsPage.razor` and
      `features/admin/feedback/pages/FeedbackAdminListPage.razor`. Replace any ad-hoc
      description paragraph or action row those pages have now. Use SectionHeader where a Card
      or Section body has a hand-built sub-heading.
- [ ] Style guide: in `features/style-guide/pages/StyleGuidePage.razor`, demo the page
      description and actions and add a "Section Header" `<Card>` entry. Use generic text.
- [ ] Render tests: static `HtmlRenderer` Jaribu tests modelled on
      `tests/container-apps/web/web-spa-integration-tests/features/identity/credential-list-render-tests.cs`,
      under `…/web-spa-integration-tests/components/`. Cases:
      - TimeWarpPage with neither slot renders exactly what it renders today.
      - Description shown or hidden.
      - Actions present or absent.
      - SectionHeader title only, subtitle, aside, and extra-attribute passthrough.
      - The page has exactly one `h1`.
- [ ] Gates:
      - `./bin/dev build`: 0 warnings, 0 errors.
      - `./bin/dev template-smoke`.
      - `ganda repo audit`.
      - `git diff origin/master | rg -i crunchit` is empty.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

## Notes

- Crunchit source:
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`
  - Components: `components/PageHeader.razor(.css)` and `components/SectionHeader.razor(.css)`.
  - Usage: `features/staff/pages/StaffPage.razor`, `features/clients/pages/ClientsPage.razor`,
    `features/dashboard/pages/DashboardPage.razor`, and
    `features/style-guide/pages/StyleGuidePage.razor` (lines 91-109).
  - Crunchit's shell (`layout/CrunchitPage.razor`) has no visible h1, which is why its
    PageHeader owns the h1.
- Template neighbours: `components/composites/Section.razor`, `components/elements/Card.razor`
  (native root and AdditionalAttributes), `wwwroot/css/tokens.css`, and
  `components/TimeWarpPage.razor.css` (`.twe-page__title`).
- 379-017 extends the StyleGuidePage contract for every upstreamed part. This task only adds its
  own entries.
