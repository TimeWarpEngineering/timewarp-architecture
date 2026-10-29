# Version dialog copies all info to clipboard

## Description

The version dialog (footer version button opens AssemblyInfoModal) shows the
full assembly record but offers no way to copy it. Add a copy icon action that
puts all of the displayed information on the clipboard as plain text.

Reported 2026-10-28 with screenshot of the Web.Spa dialog (Description,
Configuration, Company, Product, Version, FileVersion, InformationalVersion,
InternalsVisibleTo, CommitHash, ShortHash, CommitDate, CommitUrl,
RepositoryUrl).

## Checklist

- [ ] Add a copy button (FluentUI copy glyph, data-qa AssemblyInfoCopy) to
  AssemblyInfoModal that serializes every displayed property plus the two URLs
  as "Name: value" lines and writes it via AskAiJsModule.CopyTextAsync (or the
  ClipboardJsModule WriteText path with a Warning bar on denial).
- [ ] Omit blank properties the same way PropertyDisplay does.
- [ ] Cover with a test (serialized text contains the commit hash and both
  URLs; button hidden while AssemblyInfo is null).

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)

## Notes

Modal lives at features/application/modals/assembly-info/AssemblyInfoModal.razor.
Existing clipboard precedent: AskAnswerBar.razor CopyAsync through
AskAiJsModule.CopyTextAsync; shell notification CopyMessage through
source/features/clipboard.ts WriteText with denial surfaced as a Warning bar.
