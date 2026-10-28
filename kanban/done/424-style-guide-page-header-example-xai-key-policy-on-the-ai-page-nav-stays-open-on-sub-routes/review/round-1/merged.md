# Round 1 — merged findings
**Date:** 2026-10-28
**Sources:** general-1 (xAI policy), general-2 (nav match, palette), general-3 (page header, CSS,
breadcrumb, rename)

The review oracle re-checked M1, M2, M3, M7, M11, M12, M13, and M15 against the tree at `0d475995b`.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 3 | 0 | 0 |
| suggestion | 9 | 0 | 0 |
| nit | 7 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:162-216
- Description: the AI page's two forms (assistant name, xAI key policy) share one `Version`. An
  edited draft never takes a newer Version. If the user edits the policy, saves the name, and then
  saves the policy, the save gets a 409. It keeps getting one until the user reverts or reloads. The
  same happens the other way round.
- Suggestion: when `SiteSettingsState.Version` advances and the server value of this draft's own
  field still equals its synced value, copy the new Version into the draft and keep the edit. Do the
  same for both drafts.
- Source: general-1 #1
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: skills/tw-deploy/SKILL.md:384
- Description: this skill ships in generated apps and still says the BYOK policy is set "on Admin →
  Authentication".
- Suggestion: say Admin → AI and that it is read-only when the host key is set. Run `dev lint-skills`.
- Source: general-1 #2
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/design-system/design-system-page-contract-tests.cs:63
- Description: `ShouldContain("twe-page__actions")` passes only because `PageHeaderSample` uses the
  shell's class. The page itself passes no Actions. No test asserts the dummy header buttons are gone.
- Suggestion: assert the shell header has no actions: exactly one `twe-page__actions`, inside
  `data-qa="PageHeaderExample"`, or none when a non-header part renders.
- Source: general-3 #1
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs
- Description: the policy form has no draft-sync tests. No test reads `data-xai-byok-version` or
  `XaiByokModeSaveFailed`, and no test covers M1.
- Suggestion: add render tests in the 418 style:
  - a stale snapshot is replaced by the server Version;
  - an agent update is copied into an unedited draft;
  - a cross-form save advances the other draft's Version.
- Source: general-1 #3
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.agent-page-facts.cs:5-37
- Description: the agent can run `UpdateXaiByokMode`, but its page facts have no `xaiByokMode`, and
  the Design region lists only the other two commands.
- Suggestion: add the fact (and optionally whether the host manages the key), update the region, and
  extend the `/Admin/AI` assertions in `catalog-agent-tests.cs`.
- Source: general-1 #4
- Disposition notes:

### M6 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/navigation/navigation-match.cs:12-14
- Description: `/Admin/Roles/{id}`, `/Admin/Roles/New`, `/Admin/Principals/{id}`, and
  `/Admin/Feedback/{id}` open with the Admin group collapsed. A category opens only when it has an
  active item. Results don't mention this, and the spec asked for a sensible result on the other
  parameterized pages.
- Suggestion: open a category when the path is under one of its items' `Url + "/"` (derived from the
  registry, without making the item active), or record the collapsed group as the accepted outcome in
  Results and the Design region.
- Source: general-2 #1
- Disposition notes:

### M7 — Severity: suggestion — Status: open
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:602, 651-675
- Description: the generated `RouteTemplates` is `public { get; set; }` and is filled by a side
  dictionary and a static constructor. Any app code can change the shared registry entries.
- Suggestion: emit `{ get; init; }` and set it in the existing object initializer, then remove
  `RouteTemplatesByPage` and the static constructor.
- Source: general-2 #2
- Disposition notes:

### M8 — Severity: suggestion — Status: open
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:38-40, 53-72
- Description: the Design region still says additional routes are aliases only and never in the
  registry. It doesn't mention `RouteTemplates` or nav match.
- Suggestion: add a bullet.
- Source: general-2 #3
- Disposition notes:

### M9 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:44-45
- Description: the Design region says "LoginPage is the one edge". The new
  `[CrossSliceReference(typeof(DesignSystemParts), …)]` is a second one.
- Suggestion: name both edges and why the design system needs one.
- Source: general-2 #4
- Disposition notes:

### M10 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/design-system/samples/PageHeaderSample.razor.css:8-39
- Description: the sample copies the shell's title-bar rules under the same `twe-page__*` names,
  and they have already drifted: the description margin differs, and the shell uses a `div` where the
  sample uses a `p`. Repo rule: when two things must agree, generate one from the other or add a
  check.
- Suggestion: share one title-bar component with a heading-level parameter, or add a check and use
  `ds-` names in the sample.
- Source: general-3 #2
- Disposition notes:

### M11 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css:75-77
- Description: the change adds a new token, `--twe-space-1` (the spec says "add a token only if
  none fits"). Results don't say it is new or why. Nine CSS comments still give the scale as
  "8, 12, 24, 48".
- Suggestion: give the reason in Results (or use existing tokens), and update the comments.
- Source: general-3 #3
- Disposition notes:

### M12 — Severity: suggestion — Status: open
- File: skills/tw-blazor/SKILL.md:228, 302, 316
- Description: these references point at `DesignSystemPage.razor` for content that lives in
  `samples/SectionSample.razor`, `ButtonsSample.razor`, and `FormsSample.razor`.
- Suggestion: point each reference at its sample file.
- Source: general-3 #4
- Disposition notes:

### M13 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/components/page-breadcrumb-trail-tests.cs:49; .../features/application/command-palette-tests.cs:281; .../features/application/page-registry-tests.cs:79
- Description: three test names still say `Style_Guide`. `git grep -i -E 'style[ _-]?guide'`
  outside `kanban/` finds them; the spec's narrower grep misses them.
- Suggestion: rename them, and use the wider grep as the check.
- Source: general-2 #5
- Disposition notes:

### M14 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-playwright-tests/clear-xai-api-key-playwright-tests.cs:1-12
- Description: the Purpose and Design regions don't cover the new host-key test.
- Suggestion: update the regions.
- Source: general-1 #5
- Disposition notes:

### M15 — Severity: nit — Status: open
- File: source/container-apps/web/features/authorization/permission-ids-contracts.cs:26-27
- Description: the `SettingsWrite` note leaves out `UpdateXaiByokMode`.
- Suggestion: add it.
- Source: general-1 #6
- Disposition notes:

### M16 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-playwright-tests/breadcrumb-playwright-tests.cs:84-91
- Description: `rolesActive` is false when the nav item is missing, so the assertion passes even if
  the item never rendered.
- Suggestion: assert the item exists first.
- Source: general-3 #5
- Disposition notes:

### M17 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/design-system/samples/PageHeaderSample.razor:13
- Description: the sample uses a `ph-sample` prefix instead of `ds-`.
- Suggestion: rename to `ds-page-header*`.
- Source: general-3 #6
- Disposition notes:

### M18 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:10-11, 154; source/container-apps/web/projects/web-spa/components/NavMenu.razor:15
- Description: the "Design system: " prefix is typed by hand, and nothing ties it to the page title.
  The Design comments are wrapped badly, and one line is over 100 characters.
- Suggestion: rewrap. Optionally pin the prefix to the title with a test.
- Source: general-2 #6
- Disposition notes:

### M19 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/design-system/samples/PageHeaderSample.razor:22, 28
- Description: the example buttons post only "Sync." / "Add item." toasts, the behavior the old
  header buttons were criticized for.
- Suggestion: optional. Drop the handlers or say it is a placement demo.
- Source: general-3 #7
- Disposition notes:

## Duplicates / conflicts

- M4 overlaps M1. M1 is the defect, and M4 is the missing coverage for it and for the 418 refresh
  behavior. They are kept separate so that M1 can be fixed without M4's full test set.
- No reviewer disagreements. Each reviewer covered a separate slice.
