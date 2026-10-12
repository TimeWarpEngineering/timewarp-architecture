# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** HEAD (6cf19c84d) vs HEAD~1

## Summary
The change is sound. TextArea.razor passes Value/ValueChanged/ValueExpression through (so `@bind-Value` and EditForm validation work) and splats unmatched attributes onto the fluent-textarea host. The `::deep` rule hangs off the stamped native `.twe-text-area` wrapper and the min-height custom property inherits from it. TWA0032 follows TWA0025's structure (WASM gate, razor-tree handling, simple-name opt-out, release-md row). The 11 analyzer tests pass. No bugs found; three low-severity points below.

## Issues
### Issue 1 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/direct-fluent-text-area-analyzer.cs:~135 (IsHostPath)
- Description: The host exemption matches on file name only. Any `TextArea.razor` in any folder (e.g. `features/x/TextArea.razor`, hint name `features_x_TextArea_razor.g.cs`) is exempt, even though the rule names `components/forms/TextArea.razor` as the only host. A second same-named component could use raw FluentTextArea silently. No test covers a same-named file in another folder or a backslash path.
- Suggestion: Either also require the `components/forms` segment (or the `components_forms_` prefix of the generated hint name), or state in the Design region that file-name matching deliberately accepts this false negative. Optionally add a backslash-path test.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs (SubmitFeedbackAsync)
- Description: The helper re-clicks Submit up to 5 times, with a 6s receipt wait per attempt. If the first submit is just slow, the re-click can file duplicate items. It also hides the underlying "click lands on a disabled button after the blur action" race rather than failing on it. The test only needs one item, so it still passes, but the retry can mask a real double-submit or disabled-state bug.
- Suggestion: Wait on a deterministic condition (button enabled and no active action) before a single click. If a retry is kept, assert exactly one item was filed or fail on a second receipt.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/forms/FormField.razor.css:31
- Description: `.twe-form-field__control ::deep > fluent-textarea` is now dead for the migrated call sites, because the textarea sits inside the `.twe-text-area` wrapper and is no longer a direct child. The FormField Design comment still lists fluent-textarea. Nothing breaks, since the wrapper is `display:block; width:100%`.
- Suggestion: Remove the stale selector and comment mention in a boyscout edit, or leave it.
- Status: open
