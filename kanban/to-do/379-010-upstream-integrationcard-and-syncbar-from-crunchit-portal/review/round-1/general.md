# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** branch vs master (1b86689a8): `IntegrationCard.razor`, `.razor.css`,
`integration-card-variant.cs`, `components/overview.md`, `AiPage.razor`, `StyleGuidePage.razor`,
`integration-card-render-tests.cs`

## Summary

The change adds one native `IntegrationCard` with Full (default) and Compact variants. It adopts
Full on the editable branch of Admin → AI and adds five samples to the style guide. It matches the
Decision and the Checklist. The root is a native `twe-` div with `Class` merge and unmatched-attribute
splat. CSS is isolated and uses `--twe-*` tokens only, with no hex values and no `<style>`. The icon
tile and the compact separator are `aria-hidden`. The title is not a heading. The component wires no
events, and Purpose and Design regions are present. Risk is low.

Verified in this worktree:
- `dotnet test -c Release -- --filter-class IntegrationCard_Should_` in `web-spa-integration-tests`
  built and ran 9 passed, 0 failed.
- The full `web-spa-integration-tests` project ran 402 passed, 0 failed. That includes
  `ClearXaiApiKey_Given_A_StandIn`.
- `git diff master...HEAD -- source tests | grep -ic "crunchit|copic"` printed 0.
- The Playwright clear test asserts `ToContainTextAsync("3799")` on `[data-qa=XaiApiKeyStatus]`.
  The card root keeps that `data-qa`, and the meta still carries the suffix, so the containment
  assertion holds. That test was not run here.
- The tokens used (`--twe-space-2/3/6/12`, `--twe-text-helper`, `--twe-ink-2`, `--twe-paper-2`,
  `--twe-radius-sm`, `--twe-shadow-sm`) all exist in `tokens.css`. The 48px full tile, in place of the
  spec's "about 40px", is the nearest spacing step and is documented.
- The server sets `Configured` only from a readable row, and that row carries a `Suffix`
  (`site-xai-api-key-application.cs:118-123`). So the page's text and badge cannot disagree in a
  reachable state.

Not re-run: `dev build` (full solution), `dev template-smoke`, `ganda repo audit`. The Release build
of the web-spa and test projects that the test run needs succeeded under warnings-as-errors.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:49
- Description: The key state is worked out in three places: `StatusLine`, `KeyBadgeStatus`, and
  `KeyBadgeLabel`. Each one repeats the NeedsReplacement / Configured / otherwise branches, and
  `StatusLine` also checks `Suffix is null`. If `Configured` were true while `Suffix` was null, the
  meta would say "No key stored." while the badge said "Configured". The server cannot produce that
  state today. The repo rule is that two things that must agree get one source.
- Suggestion: Work out a single page-private key state once, then map the badge status, the label,
  and the sentence from it with one `switch` each.
- Status: open
