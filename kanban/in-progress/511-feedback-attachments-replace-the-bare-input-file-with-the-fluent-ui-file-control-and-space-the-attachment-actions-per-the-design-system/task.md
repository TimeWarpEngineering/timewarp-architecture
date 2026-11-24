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

## Goal (implementer restatement)

The Attachments field on `/Feedback` should look like the rest of the form. The browser's grey
"Choose Files | No file chosen" widget goes away. A FluentUI outline "Choose files" button with an
attach icon opens the file picker through a hidden `FluentInputFile` (`AnchorId`, no drop zone,
`Buffer` mode). Our `OnFilesAsync` still reads and uploads the bytes through `OnInputFileChange`, so
upload rules, `accept`, `multiple`, `aria-label="Attachments"` and `data-qa="FeedbackFile"` stay on
the hidden input. The three attachment actions sit in one wrapping row with a `--twe-space-3` gap.
The chips list sits `--twe-space-2` under that row and shows nothing when empty. The page CSS uses
tokens only and drops the hand-picked rem margins.

Done when:
- The page renders `FluentInputFile` plus a keyboard-operable "Choose files" button, and the
  native file widget is not visible.
- The action row and chips use the spacing tokens; no rem margins remain in the page CSS.
- The feedback-attachment Playwright tests pass: `setInputFiles` on the hidden input still uploads,
  and the Choose files button opens the file chooser by click and by keyboard.
- `evidence/` holds six screenshots: empty, two files, captured page chip, each light and dark.
- `dev build` is 0/0 and `ganda repo audit` is green.

## Checklist

- [x] `FluentInputFile` manual mode with Choose files button; `OnFilesAsync` unchanged
- [x] One action row at `--twe-space-3`; rem margins removed
- [x] Playwright feedback-attachment tests green
- [x] Six screenshots in `evidence/`
- [x] `dev build` 0/0, web-spa integration tests, `ganda repo audit` green

## Session

- Created: 2026-11-24 from Steve's Feedback page review.
- 2026-11-24 implementer: FluentInputFile 5.1.0 checked by decompile. `OnInputFileChange` returns
  before the component's own reading, and unmatched attributes (`aria-label`, `data-qa`) land on the
  hidden `<input type=file>`. `Icons.Regular.Size16.Attach` exists (FluentUI MCP).

## Results

- `FeedbackListPage.razor`: `InputFile` replaced by a hidden `FluentInputFile`
  (`DragDropZoneVisible=false`, `Mode=Buffer`, `Multiple`, `Accept`, `AnchorId`,
  `OnInputFileChange="OnFilesAsync"`, `aria-label="Attachments"`, `data-qa="FeedbackFile"`). A new
  outline `FluentButton` "Choose files" (`Id="feedback-choose-files"`, `Attach` 16 icon,
  `data-qa="FeedbackChooseFiles"`) opens the picker. The three actions sit in
  `.feedback-attachment-actions`, and that row plus the chips list sit in `.feedback-attachment-field`.
  `OnFilesAsync` is unchanged.
- `FeedbackListPage.razor.css`: `.feedback-file` and `.feedback-capture` removed with their `0.75rem`
  margins. Action row gap `--twe-space-3`, chips `--twe-space-2` under it, chip list and chip gaps on
  tokens. FormGrid's `--twe-space-6` row gap spaces the next field.
- `FeedbackListPage.razor.cs`: Design region notes the picker wiring.
- New `feedback-attachment-picker-playwright-tests.cs` (`FeedbackAttachmentPicker_Given_Wasm`), run in
  light and dark. It checks that the native input is hidden and keeps aria-label/multiple, that
  "No file chosen" is absent, and that the chips list is absent when empty. Enter and Space on the
  focused button each open the file chooser, and a click opens it too. Two chosen files upload. It
  measures a 12px action gap and an 8px chips gap, and it writes the six evidence shots.
  `feedback-attachment-playwright-tests.cs` needed no change: `setInputFiles` on the hidden
  `FeedbackFile` input still uploads.
- Evidence: `evidence/attachments-{empty,two-files,captured-page}-{light,dark}.png`, copied from
  the picker test's output (real WASM host).
- Audit boy-scout: `ganda repo audit --fix` pointed local `task/505-…` back at its own upstream
  (`task-branch-upstream`, Error) and set the local VS Code window title (`vscode-window-icon`).
  Remaining warning `kanban-landed` names task 505's live claim from another walk; this task does
  not own it.

### Gates

- `./bin/dev build`: Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet run --project tests/container-apps/web/web-spa-playwright-tests/web-spa-playwright-tests.csproj -c Release --no-build -- --filter-class FeedbackAttachmentPicker` (new file; csproj project, not a standalone runfile): 1/1 passed.
- `… -- --filter-class FeedbackAttachment_Given_Wasm` (covers the page, unedited): 3/3 passed.
- `… -- --filter-class Screenshot_Given_Wasm` (uses FeedbackCaptureScreenshot / AttachPageBehind): 1/1 passed.
- `dotnet run --project tests/container-apps/web/web-spa-integration-tests/web-spa-integration-tests.csproj -c Release --no-build -- --filter-class FeedbackDetails_Should_`: 1/1 passed.
- `ganda repo audit`: first run failed on `task-branch-upstream` (task 505's branch, not this task's).
  `ganda repo audit --fix` fixed it. No blocking failures remain; only the `kanban-landed` warning for 505.

### How to validate

Smoke: `./bin/dev build`, then
`dotnet run --project tests/container-apps/web/web-spa-playwright-tests/web-spa-playwright-tests.csproj -c Release --no-build -- --filter-class FeedbackAttachment`.
Or `dev run`, sign in, open `/Feedback`, and Tab to "Choose files" then press Enter.

Expect: every FeedbackAttachment* class passes: picker 1, attachment 3, and the admin attachment class the substring also matches.
On the page, Attachments shows a row of outline buttons "Choose files" and "Capture this page"
12px apart, with no grey "Choose Files | No file chosen" widget. Enter, Space or click opens the OS
file picker. Chosen files appear as chips 8px under the row. With no files, nothing shows under the row.

