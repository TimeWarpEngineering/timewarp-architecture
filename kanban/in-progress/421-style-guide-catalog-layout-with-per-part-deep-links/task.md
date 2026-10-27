# Style guide catalog layout with per-part deep links

## Goal (implementer restatement)

The style guide is one long page of 29 cards. Make that page a catalog: a grouped, filterable part list, one part on screen, a hand-written usage snippet, and a stable URL per part. `/StyleGuide` stays the full page (All) and the only menu, palette, and agent destination. `/StyleGuide/{Part}` is a route alias. An unknown slug stays on the page and says the part is missing. All and the deep link render the same sample components. The two inline `style=` attributes become `data-token` rules in the part stylesheet.

Acceptance: All shows the 29 sections in today's order. Each slug shows only that part and its Usage block, with the list entry marked current, including a full page load. An unknown slug shows the missing-part state. The filter narrows the list as you type and clearing it restores the list. Choosing a part changes the URL, and Back returns. No `style=` remains under `features/style-guide`. The registry, nav, command palette, and catalog agent still list one Style Guide destination. `dev build` passes with 0 warnings, and the integration and Playwright suites are green.

## Description

Replace the single 476-line, 29-section `/StyleGuide` page with an in-page catalog: a filterable part list, one part shown at a time, a usage snippet per part, and a deep link per part at `/StyleGuide/{Part}`. Keep an **All** entry that renders the full page as it does today. Fold in action G: remove the two inline `style=` uses that break `tw-blazor-css-strategy` rule 6.

This borrows the *layout* of Ryan Lanciaux's native-surface playground (list, then one part, then usage). It does not take the library as a dependency.

Source: [Native Surface eval](https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-10-26-native-surface-playground-style-guide-layout.md) ([timewarp-intel PR #78](https://github.com/TimeWarpEngineering/timewarp-intel/pull/78)). Busara verdict: GREEN for A with G folded in. Park B, C, D. Kill E, F.

Filed 2026-10-27 at Steven's request ("File the task but don't launch it yet.", relayed by Amina). Not launched.

## Requirements

### Deep links first (land these even if the filter UI slips)

- Multi-route page: `[Page("/StyleGuide", "/StyleGuide/{Part}", ...)]`. The navigable static primary route `/StyleGuide` stays as-is (Developer group, NavOrder 310, DeveloperAccess policy). `/StyleGuide/{Part}` is a `[Route]`-only alias, per the `tw-blazor-layout` multi-route rules (TWE009/TWE011).
- `{Part}` is a stable kebab-case slug per part (for example `/StyleGuide/drawers`, `/StyleGuide/color-tokens`). Choose one casing rule and document it in the page's `#region Design`.
- `/StyleGuide` with no part renders the **All** view (the full page as it is today), so existing links, Ctrl-F, and full-page review still work.
- An unknown part slug shows a clear "no such part" state with a link back to All. It does not throw, and it does not 404 the shell.

### Catalog layout

- The catalog lives **inside `TimeWarpPage`** as page content. The page title, description, and actions stay. Do not use the shell `Aside`, because Ask owns that column when it is open.
- Left: a native `<nav>` + `<ul>` part list authored by a `StyleGuideCatalog` component with isolated `.razor.css`. Use grouped headings (Foundations, Forms, Layout, Data, Feedback, Identity). **All** is the first entry. Do **not** use a second `FluentNav`, because it fights the shell's one-menu registry.
- Filter: a `FluentTextInput` that narrows the list by part name as you type, reached with an anchored `::deep` (Wall A). Clearing it restores the full list.
- Selecting a part navigates to `/StyleGuide/{Part}`, so browser back and forward work. The selected entry is marked (`aria-current="page"`).
- Centre: only the selected part is shown. Keep existing side-by-side variants as they are (Avatars, Status Badges, Empty States). Do not add variant chips.
- Each part has a **Usage** block containing a read-only, hand-written Razor snippet showing how to use the component. Snippet generation is out of scope.

### Per-part split

- Split the 29 cards into per-part sample components under `features/style-guide/` (for example `samples/AvatarSample.razor` + `.razor.css`), following `tw-feature-placement` and `tw-blazor-css-strategy`. The All view and the single-part view render the same sample components; there is no duplicate markup.
- Keep the design rule in `StyleGuidePage.razor.cs`: demos go through production code paths. Notifications use `NotificationState`, Confirmations use `StyleGuideState.ConfirmSample` → `IConfirmationService`, and the drawer follows `ApplicationState`. Keep the existing `CrossSliceReference` and `PageLocalMessageBar` attributes, or move them with a reason.

### Fold in G: inline `style=` (CSS rule 6)

Both are in `source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor` on master `ff36ff0b1`:

- Line 111 (Color Tokens card): `<div class="sg-swatch__chip" style="background: var(@token);"></div>`
- Line 123 (Type Scale card): `<div class="sg-type__row" style="font-size: var(@token);">@label</div>`

Replace each with a per-token class or a `data-token` attribute selected in the part's `.razor.css`. After the change, the style-guide feature has no `style=` attributes left.

## Out of scope

- **B, parked:** task 075's nav-category split (one `[Page]` per area under a "Style Guide" nav group). A's `/StyleGuide/{Part}` already gives per-part URLs. Revisit only if Steven wants menu entries.
- **C, parked:** live props controls (Storybook-style knobs).
- **D, parked:** a per-preview environment frame (light/dark and width presets).
- **E, killed:** BlazingStory as a dependency.
- **F, killed:** native-surface or `@native-surface/playground` as a dependency.

## Acceptance criteria

- `/StyleGuide` renders every section in today's order (All).
- `/StyleGuide/{Part}` for each of the 29 parts renders only that part and its Usage snippet, with the list entry marked current. A full-page load of the deep link works, not just in-app navigation.
- An unknown slug shows the no-such-part state.
- Typing in the filter narrows the list, and clearing it restores the list. Picking an entry updates the URL.
- No `style=` attributes remain in `features/style-guide/**`.
- The page registry, navigation destinations, command palette, and catalog agent still list one Style Guide destination (`/StyleGuide`). The aliases do not show up as extra menu or palette items.
- `dev build` passes with 0 warnings. The integration and Playwright suites are green.

## Tests

- **Contract:** rework `tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs` (`StyleGuidePage_Should_.Show_Every_Upstreamed_Part_In_Page_Order`, from 379-017) into two parts. An index test checks that All lists every part title in order and that every slug is unique and resolves. A per-part contract test checks that each part renders its title and Usage block.
- **Registry/nav:** keep `page-registry-tests.cs`, `navigation-destinations-tests.cs`, `command-palette-tests.cs`, `catalog-agent-tests.cs`, and `catalog-agent-parity-tests.cs` green. Add assertions that the `{Part}` alias is not a navigable or palette entry.
- **Playwright (new):** a style guide catalog test in `tests/container-apps/web/web-spa-playwright-tests/` with these cases:
  - A full navigation to `/StyleGuide/drawers` shows only the Drawers part and marks its list entry current.
  - A full navigation to `/StyleGuide` shows All with every part.
  - Typing in the filter narrows the list, and clearing it restores the list.
  - Clicking an entry changes the URL to `/StyleGuide/{Part}`, and browser Back returns to the previous view.
  - An unknown slug shows the no-such-part state.
- **Playwright (existing):** point `drawer-playwright-tests.cs` (line 57 loads `/StyleGuide`) at `/StyleGuide/drawers`. Point `clear-xai-api-key-playwright-tests.cs` (line 101 loads `/StyleGuide` for the sample confirmation) at the Confirmations part, or keep it on All. Do not change its flake handling; that is a separate issue (see Notes).
- Keep the `screenshot-path-guard-tests.cs` rules in mind: no `"kanban/..."` literals under `tests/`.

## Checklist

- [x] Route aliases plus a part slug table; `/StyleGuide` = All; unknown-slug state
- [x] Split the 29 cards into per-part sample components with isolated CSS
- [x] G: replace both inline `style=` uses (lines 111 and 123)
- [x] `StyleGuideCatalog` list, grouped headings, All entry, `aria-current`
- [x] Filter input
- [x] Usage snippet per part
- [x] Rework the contract tests (index + per part); registry/nav alias assertions
- [x] New catalog Playwright test; retarget the drawer (and confirmation) Playwright tests
- [x] Update the `#region Design` in `StyleGuidePage.razor.cs`
- [x] `dev build` 0 warnings; integration and Playwright suites green; PR via `tw-pr`
- [x] Review round 1: group headings (M1), catalog product-name scan (M2), notification rationale (M3)

## Review

- Round 1: M1, M2, and M3 fixed on this branch. Round 2 is the host re-review.
- Round 2: no new findings. Disposition: clean (`review/disposition.md`).

## Notes

- Today: `features/style-guide/pages/StyleGuidePage.razor` (476 lines, 29 `Card`/`Section` blocks in one `twe-stack` column), plus `StyleGuidePage.razor.css` and `StyleGuidePage.razor.cs` (route, policy, design notes).
- History: task 075 set a promote trigger at about 6–8 sections. The 379 series grew the page to 29, and 379-017 pinned the whole page in a contract test.
- Size M is Akili's estimate. Most of the work is the per-part split and the test rework.
- `ClearKey_Should_Ask_And_LeaveTheKey_Until_Confirmed` has been seen flaking on PR #577 (a probable double-fetch race in `AiPage.razor`). It is not part of this task.
- The architecture repo bot owns this. Don't start it until Steven says go.

## Session

- Created: 2834145 (2026-10-27)
- Implementation: catalog, samples, and tests (2026-10-27)
- Audit: `ganda repo audit --fix` pinned TimeWarp.Nuru and TimeWarp.Nuru.DevCli to 4.0.0-beta.6 and refreshed `.githooks/pre-push.cs`. `kanban-landed` stays an advisory warning for eight other claims with no worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). Those claims are outside this id.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-27T14:40:49Z
- Review oracle round 1: general reviewer (claude, model claude-opus-5-5), effort 3, session not reported — 2026-10-27. Re-ran `dev build`, the integration group (8/8), and the Playwright group (passed). There are 0 bugs. Findings M1 (suggestion) and M2 and M3 (nits) are open, and a fix pass was requested. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-27T14:53:18Z
- Fix pass (implement-fix-2): group headings, the catalog product-name scan, and the notification rationale — 2026-10-27
- Audit: `ganda repo audit --fix` landed kitchen 421 on origin-home. `kanban-landed` stays a warning for the same eight other claims with no worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). Those claims are outside this id.
- Review oracle round 2: general reviewer (claude, model claude-opus-5-5), effort 1, max-turns 80, session not reported — 2026-10-27. Scope was the fix delta `8e9eed258..0a49d867d`. It re-ran `dev build`, the integration group (8/8), and the Playwright group (passed). M1, M2, and M3 are confirmed fixed, and there are no new findings. Disposition: clean. The implementer restatement matches the task, so there is no misread.

## Results

`/StyleGuide` is the All view and the only menu, palette, and agent destination. `/StyleGuide/{Part}` is a lowercase kebab alias. Each of the 29 cards is a sample under `features/style-guide/samples/`, shared by All and the single-part view. The single-part view adds a hand-written Usage block. An unknown slug stays on the page and says "No such part." Color chips and type rows use `data-token` rules, so the style-guide feature has no `style=` attributes. Catalog groups are headings, and each part list is labelled by its group. NotificationsSample records why it uses NotificationState and the exception pipeline. The pull request is left for the host open-pr node.

### Review

Two rounds ran, each with one general reviewer: round 1 at effort 3 on the full branch diff, and round 2 at effort 1 on the fix delta. Final counts: 0 bugs, 1 suggestion fixed, 2 nits fixed, 0 wontfix, 0 open. Round 2 found no new issues. It re-ran `dev build`, the integration group (8/8), and the Playwright group (passed). **Disposition: clean.** Paths: `review/review-framework.md`, `review/round-2/merged.md`, and `review/disposition.md`.

### How to validate

Smoke: Sign in as a developer. Open `/StyleGuide`. Confirm the catalog groups (Foundations, Forms, Layout, Data, Feedback, Identity) read as headings and that each part list is labelled by its group. Type `Drawers` in Filter parts and confirm only the Layout heading remains. Clear the filter, click Drawers, then use the browser Back button. Load `/StyleGuide/drawers` and `/StyleGuide/no-such-part` in the address bar.

Expect: The filter leaves only Drawers under the Layout heading. The click lands on `/StyleGuide/drawers`, marks Drawers current, and shows its Usage block. Back returns to All, every part, and all six group headings. A direct drawers load shows only the Drawers card, with the catalog headings still present. An unknown slug stays on Style Guide and says "No such part." with a link back to All.
