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
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-22T03:21:25Z
- Review oracle round 1: general reviewer (claude, claude-opus-5-5), effort 2, session not
  reported — 2026-10-22. 4 open (1 bug, 1 suggestion, 2 nits); fix pass requested.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-22T03:24:19Z
- Fix pass (implement-fix-2): implementer-cursor (cursor, grok-4.7), 2026-10-22. Addressed M1–M4.

## Review

- Round 1: Blocked — open M1, M2, M3, M4
- Fix pass: M1–M4 are addressed on this branch. Round 2 has not run.

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

## Goal (implementer restatement)

The page shell already owns the only h1, so this task does not add a second title component.
`TimeWarpPage` takes optional `Description` and `Actions` fragments. When both are empty, the
title and breadcrumb markup stays what it is today. When either is set, the title and
description sit on the left and the actions wrap on the right, in that same header. The title
markup itself is one fragment, so the empty-slot path and the header path cannot drift.
`SectionHeader` is a composite next to `Section`: an h2, an optional subtitle, an optional
aside, and unmatched attributes on a native `twe-section-header` root. Styling is isolated CSS
and the existing `--twe-*` tokens. These pieces dispatch nothing; a caller passes the buttons.
The style guide demos the page slots and a section-header card. The todo list and the feedback
review page use the description slot, and the todo copy says what that sample page is for.
Render tests cover the empty shell, each slot, a single h1, and the section header's title,
subtitle, aside, and attribute passthrough. The page render harness passes its parameter
dictionary through `AddMultipleAttributes` with a literal sequence.

Acceptance: `./bin/dev build` is clean, `./bin/dev template-smoke` passes, `ganda repo audit`
passes, and the product diff does not introduce the source product's name.

## Results

There is no separate page-title component. `TimeWarpPage` takes optional `Description` and
`Actions` render fragments. Both empty keeps today's title and breadcrumb markup. Either one
set wraps the title and description on the left and the actions on the right. Razor will not
mix a named slot with implicit body content, so a page that sets either slot wraps its body in
`ChildContent`. Pages that set neither slot are unchanged.

`SectionHeader` lives in `components/composites/` (`TimeWarp.Architecture.Components`). The root
is `div.twe-section-header`. `--twe-text-section-title` was already in `tokens.css`, so no token
was added. Row and action gaps use `--twe-space-3` and `--twe-space-2` (the spacing scale has
no 16px step). The subtitle's 2px offset stays a hairline under the title.

The style guide puts its intro in `Description`, dispatches Outline and Primary through
`NotificationState`, and shows a Section Header card (Reports, a subtitle, Details). Todo Items
and Feedback review had no loose intro paragraph, action row, or hand-built sub-heading, so
they adopt `Description` only. Todo Items says "Browse the sample todo list." The list
component only renders the words "Todo Item List", so the description does not claim that a
person can mark items done.

Fix pass for round 1: the page render harness calls `AddMultipleAttributes` with sequence `1`
and no `#pragma`. The dictionary is `Dictionary<string, object>` because that method takes
non-null values. The document title and the h1 are one `TitleBlock` fragment, rendered from
both header branches. The feedback page body is indented one level inside `ChildContent`.

### How to validate

Smoke:

- `./bin/dev build` — 0 warnings, 0 errors (Release, this fix pass).
- `./bin/dev template-smoke` — SmokeDefault OK; generated Jaribu aggregators 405/405, 9/9, and 14/14. Template smoke succeeded.
- `ganda repo audit` — 32 passed, 0 failed, 1 skipped.
- From `tests/container-apps/web/web-spa-integration-tests`: `dotnet test -c Debug -- --filter-class TimeWarpPage_Should_` (4 passed) and `--filter-class SectionHeader_Should_` (4 passed).
- `git diff origin/master -- source tests | rg -i crunchit` — no match. The kitchen path contains the task folder name, so a whole-tree diff still matches that path.

Expect:

- Style Guide shows one h1, the brand sentence under it, and Outline / Primary on the right. The Section Header card shows an h2 "Reports", the period subtitle, and Details.
- Todo Items shows "Browse the sample todo list." under the title and no action cluster.
- Feedback review shows "Review filings. Filter by status, kind, and severity." under the title. The filter and grid are unchanged. The body inside `ChildContent` is indented one level.
- Any other page that does not set the slots still renders only the title and breadcrumb.
- A page with either slot still has exactly one h1, and that h1 is the same markup as the empty-slot page.

A live browser pass of the running app was not done in this walk. The static renderer covers the slot markup, including one h1 on both header paths.

### Review

- Round 1 (general reviewer, effort 2): 1 bug, 1 suggestion, and 2 nits. The reviewer re-ran
  `./bin/dev build` 0/0, `ganda repo audit`, and both new test classes 4/4. The reviewer did
  not re-run template-smoke.
- M1: the test now uses `AddMultipleAttributes(1, PageParameters)`. M2: `TitleBlock` is the
  one title fragment. M3: the todo description is "Browse the sample todo list." M4: the
  feedback body is indented inside `ChildContent`.
- Disposition: addressed in this fix pass. Round 2 has not run.
- Paths: `review/review-framework.md` and `review/round-1/merged.md`.
