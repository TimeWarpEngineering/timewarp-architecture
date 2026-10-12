# Shared TextArea component that fills its field and resizes, so pages cannot repeat the 302 narrow-textarea bug

## Problem

On 2026-10-12, Steve found the admin feedback detail page (`/Admin/Feedback/{id}`, task 314)
showing tiny, narrow "Reply to filer" and "Internal note" textareas. This is the same bug fixed for
`/Feedback` in task 302. The filer's permalink reply box (`FeedbackItemPage.razor`) has it too.

**Why it came back:**
- 302 fixed it **in page-scoped CSS** (`features/feedback/pages/FeedbackListPage.razor.css`,
  `.feedback-details ::deep fluent-textarea { --inline-size: 100%; --min-block-size: 9rem; … }`).
- The visible box is FluentUI's shadow `part="root"`. Its `--inline-size` defaults to 18rem, so
  `Width="100%"` and `block` on the host do not size it.
- Every new page that uses `FluentTextArea` has to remember that CSS. That is agreement by memory.

Affected uses today:
- `features/feedback/pages/FeedbackListPage.razor` (fixed by page CSS);
- `features/feedback/pages/FeedbackItemPage.razor` (broken);
- `features/admin/feedback/pages/FeedbackAdminItemPage.razor`, two of them (broken).

## Requirements

- **One shared component** in `web-spa/components/forms/`, for example `TextArea.razor` +
  `TextArea.razor.css`. It wraps `FluentTextArea` and always:
  - fills its container (`--inline-size: 100%` on the host, `display: block`, `min-width: 0`);
  - has a sensible default height via `--min-block-size` (9rem default, overridable by a
    parameter);
  - resizes vertically, with no fixed host height, because that fights the grip as 302 found.

  Pass-through parameters: `@bind-Value`, `Name`, `AriaLabel`, `Placeholder`, `data-qa` /
  additional attributes, and `Disabled`. CSS lives in the component's isolated `.razor.css`
  (`tw-blazor-css-strategy`; no `<style>` element). Purpose and Design regions explain the
  shadow-root sizing in one place.
- **Replace all four uses** with the component. Remove the now-redundant `fluent-textarea` rules
  from `FeedbackListPage.razor.css`. Keep `.feedback-details` as the paste host (295 / 303-001)
  wrapping the new component, and check paste still works.
- **Guard so it cannot recur.** Raw `<FluentTextArea` in web-spa razor outside the shared
  component fails the build or tests. Prefer a TWA analyzer rule in the same family as TWA0025
  (FluentMessageBar is allowed only in `MessageBars.razor`), with an opt-out attribute carrying a
  reason. A source-scan test is acceptable if an analyzer is disproportionate; record the choice.
  If it's an analyzer, give it the next free TWA id and add `AnalyzerReleases.Unshipped.md` and
  the AGENTS-style table entry where diagnostics are listed.
- **Playwright proof.** Generalize 302's `VisibleDetailsBox_Should_MatchTitle_And_Resize`
  measurement, which compares the shadow root box with the field width and drags the grip. Cover
  the admin reply and note boxes and the filer reply box at desktop and narrow widths. Screenshots
  go to test output (316).
- **Docs:** `skills/tw-blazor` (or `tw-blazor-css-strategy`) gets one line: use the shared
  TextArea; never use raw `FluentTextArea`.

## Checklist

- [x] `components/forms/TextArea.razor` (+ isolated CSS) with fill/resize behaviour
- [x] Four call sites migrated; page CSS cleaned; paste host still works
- [x] Guard (analyzer preferred, or source-scan test) with recorded choice
- [x] Playwright measurements for admin reply/note and filer reply
- [x] Skill line
- [x] Gates: `dev build` 0/0, `dev test` (Playwright), `dev template-smoke`, `ganda repo audit` (one aspire-tests failure that also fails on origin/master; see Results)

## Session

- Created: 2026-10-12 (cockpit, per Steve's screenshot of `/Admin/Feedback/{id}`)
- 2026-10-12: implemented under `ganda task work` (implement oracle).
- 2026-10-12: review oracle (Claude Opus 5.5), effort 3; general reviewer subagent a40fda73b7c3f58ac (Sonnet).
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-12T15:04:56Z

## Results

**Component.** `web-spa/components/forms/TextArea.razor` + `TextArea.razor.css`. The native
`.twe-text-area` wrapper is the stamped anchor; `::deep fluent-textarea` sets `display: block`,
`min-width: 0`, `--inline-size: 100%`, and `--min-block-size: var(--twe-text-area-min-block-size, 9rem)`.
The `MinHeight` parameter overrides the 9rem default through that custom property. Resize is
always `Vertical`, and there is no fixed host height. Pass-through: `@bind-Value` (Value /
ValueChanged / ValueExpression, so EditForm validation still sees the caller's field), `Name`,
`AriaLabel`, `Placeholder`, `Disabled`, `Class`, plus unmatched attributes (`data-qa`, ...) on the
`fluent-textarea` host. The shadow-root sizing is explained once, in its Purpose/Design comment.

**Call sites.** `FeedbackListPage` (Details, still inside the `.feedback-details` paste host),
`FeedbackItemPage` (filer reply), and `FeedbackAdminItemPage` (reply, internal note) now use `<TextArea>`.
The `fluent-textarea` rules are gone from `FeedbackListPage.razor.css`. Paste still works:
`PasteAndUpload_Should_ShowAttachmentsOnTheItem` passes, because the paste script finds
`fluent-textarea` as a descendant of the host.

**Guard: analyzer TWA0032 (chosen over a source scan).** `DirectFluentTextAreaAnalyzer` in
convention-analyzers is the same family as TWA0025. It reports `OpenComponent<FluentTextArea>` in
Blazor WASM compilations outside `TextArea.razor`, matching the host by file name (generated
tree or `#line` path) so it survives template namespace renames. It reports on the first
`#line`-mapped parameter, so the error lands on the `.razor` line, for example
`FeedbackItemPage.razor(97,48): error TWA0032`. Opt-out is
`[DirectFluentTextArea("reason")]` (new attribute in `timewarp-architecture-attributes`); an empty
reason does not opt out. TWA0029–0031 are retired, so 0032 is the next free id. Listed in
`AnalyzerReleases.Unshipped.md`, `skills/tw-blazor`, `components/overview.md`, and the
web-spa.csproj Attributes comment. 11 analyzer tests in
`tests/analyzers/.../direct-fluent-text-area-analyzer-tests.cs`.

**Playwright.** New test `ReplyAndNoteBoxes_Should_FillTheirField_And_Resize` in
`feedback-attachment-playwright-tests.cs`. It files an item, opens the permalink (filer reply),
then opens `/Admin/Feedback/{id}` (admin reply + note; the first passkey account claims
Administrator). For each box it measures shadow `part=root` against the content box of the
element holding the TextArea, at 1280 and 600 wide, checks a height of at least 140px, and drags the
grip 80px. The 302 drag logic is now the shared `AssertGripResizesAsync`, which the Details test also uses.
Screenshots: `playwright-screenshots/FeedbackAttachment_Given_Wasm/feedback-{filer,admin}-textareas-{1280,600}.png`.
Negative check: with `--inline-size` removed from TextArea.razor.css, the test fails with
`root=293..581 288x144` against `field=293..1227` (the 18rem bug).
Submit helper: the submit button is `Disabled` while any action is active, so the test waits until
it is enabled and clicks again only if no receipt appears.

**Gates.**
- `dev build`: 0 warnings, 0 errors.
- `dev test`: every project passes except one aspire-tests failure,
  `RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole` (Unauthorized vs
  Forbidden). The same test fails on a clean `origin/master` checkout (9e95be869), so this task did not cause it.
  The run used `TIMEWARP_TEST_PORT_BASE=17300` because another process held port 7000.
- web-spa-playwright-tests: 3/3 FeedbackAttachment tests pass.
- Analyzer tests: 203/203 pass.
- `dev template-smoke`: SUCCEEDED.
- `ganda repo audit`: passes.
- `dev check-version`: source 2.0.0-beta.20 is already ahead of NuGet beta.19, so no bump was needed.

### Review disposition

- **Outcome:** clean. 2 rounds, effort 3, roster: general (Sonnet subagent) plus an orchestrator re-review.
- **Final counts:** bug 0; suggestion 2 fixed; nit 1 fixed; 0 open; 0 wontfix.
- **M1:** TWA0032 now matches its host by project-relative path, not bare file name. The accepted
  forms are the Razor generator hint path `components/forms/TextArea_razor.g.cs`,
  `components_forms_TextArea_razor.g.cs`, or a `#line` path ending in
  `components/forms/TextArea.razor` (either separator). A same-named `TextArea.razor` in another
  folder is now reported. 4 new tests; the class is 15/15.
- **M2:** the Playwright submit waits until the button has stayed enabled for 500ms, then clicks once.
  The 5-click retry that could file duplicates is gone.
- **M3:** removed the dead `> fluent-textarea` selectors from FormField.razor.css and its Design comment.
- **Re-verified:** `dev build` 0/0; guard edit still fails with `FeedbackItemPage.razor(97,48): error TWA0032`;
  FeedbackAttachment Playwright 3/3 on two runs.
- **Artifacts:** `review/review-framework.md`, `review/round-1/{general,merged}.md`,
  `review/round-2/merged.md`, `review/disposition.md`.

### How to validate

**Smoke**

```bash
./bin/dev build
dotnet run --project tests/analyzers/timewarp-architecture-analyzers-tests -c Release -- --filter-class Should_Require_The_Shared_TextArea
dotnet run --project tests/container-apps/web/web-spa-playwright-tests -c Release -- --filter-class FeedbackAttachment
# Guard: change one <TextArea to <FluentTextArea in FeedbackItemPage.razor, then:
dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Release
```

**Expect**

- Build: 0 warnings, 0 errors.
- Analyzer class: 15/15 pass.
- Playwright: 3/3 pass. The console shows `root=` equal to `field=` (for example 293..1227 at 1280 and
  53..547 at 600) for FeedbackReply, AdminFeedbackReply, and AdminFeedbackNote, and `grown=80` for each drag.
- Screenshots `feedback-filer-textareas-*.png` and `feedback-admin-textareas-*.png` show full-width,
  9rem-tall boxes.
- The guard edit fails the build with `FeedbackItemPage.razor(97,48): error TWA0032`.
