# Feedback attachments: replace the bare InputFile with the FluentUI file control and space the attachment actions per the design system

## Description

On `/Feedback` the Attachments field renders a raw Blazor `InputFile` (`FeedbackListPage.razor`
line 375). The browser draws it as its own grey "Choose Files | No file chosen" widget, so it
matches nothing else on the page, and the outline "Capture this page" button sits directly under
it with a hand-picked `0.75rem` margin (`FeedbackListPage.razor.css` lines 10-13, 36-41). Steve
flagged the pair as cramped and off-guideline on 2026-11-24.

Two violations of `tw-blazor` / `tw-blazor-css-strategy`: a native control where a FluentUI v5
control exists, and page-local rem margins instead of the spacing tokens (`--twe-space-2`
label→control, `--twe-space-3` action gap, `--twe-space-6` field row gap).

## Requirements

- Replace `InputFile` with `FluentInputFile` in manual mode: `DragDropZoneVisible="false"`,
  `Mode="InputFileMode.Buffer"` (the SPA already reads the bytes in `OnFilesAsync`; keep that
  handler by wiring `OnInputFileChange="OnFilesAsync"` so the upload path, `Accept`, `multiple`,
  size limits, and `data-qa="FeedbackFile"` behavior do not change), `AnchorId` pointing at a
  new outline `FluentButton` "Choose files" with `IconStart="Icons.Regular.Size16.Attach"` (or
  `ArrowUpload` if Attach is not in the catalogue at Size16; check with the FluentUI MCP).
- The attachment actions ("Choose files", "Capture this page", and the conditional "page behind"
  capture) form one wrapping flex row with `gap: var(--twe-space-3)`. The chosen-file chips row
  (`.feedback-attachments`) sits under it at `--twe-space-2`. Delete the `0.75rem` margins; the
  FormField's own `--twe-space-6` row gap provides the spacing to the next field.
- "No file chosen" disappears with the native widget. The chips list is the only file feedback;
  when it is empty, show nothing.
- Keyboard: the Choose files button is a real button, focusable, and opens the picker on Enter and
  Space (FluentInputFile `AnchorId` does this). The hidden input keeps `aria-label="Attachments"`.
- Styling only in `FeedbackListPage.razor.css`; no `<style>`, no inline styles, tokens only.
- Update `feedback-attachment` Playwright tests that locate `FeedbackFile` by the native input: the
  input still exists (hidden) and `setInputFiles` still works on it.

### Evidence

- Screenshots in `evidence/`: the Attachments field empty, with two files chosen, and with a
  captured page chip, light and dark, from the running app.

## Checklist

- [ ] `FluentInputFile` manual mode with Choose files button; `OnFilesAsync` unchanged
- [ ] One action row at `--twe-space-3`; rem margins removed
- [ ] Playwright feedback-attachment tests green
- [ ] Six screenshots in `evidence/`
- [ ] `dev build` 0/0, web-spa integration tests, `ganda repo audit` green

## Session

- Created: 2026-11-24 from Steve's Feedback page review.
