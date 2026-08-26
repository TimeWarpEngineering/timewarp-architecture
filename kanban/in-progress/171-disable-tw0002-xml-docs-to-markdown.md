# Disable TW0002 XML docs to markdown

## Description

Turn off TW0002 (`XmlDocsToMarkdownAnalyzer`: "XML documentation should be moved to a markdown file").
With AI-assisted documentation, the nag is no longer useful.

## Checklist

- [x] `.editorconfig`: `dotnet_diagnostic.TW0002.severity = none`
- [ ] Board close: `ganda kanban done 171` so origin-home matches shipped product
- [ ] Kanban-only PR; STOP (do not merge)

## Session

- Implementation: grok (2026-08-06)
- Cockpit: Grok close request (2026-08-26) — product already on origin/master; remaining is board close

## Notes

Product already shipped on origin/master as `dc9fc273` (`build(editorconfig): disable TW0002 XML-docs-to-markdown nag (171)`). No dedicated PR; it rode with the later analyzer wave (PR 298 / task 172).

Remaining work is **board hygiene only** on this same task id:

- Do **not** change `.editorconfig` or any product file
- Do **not** create a sibling close/hygiene task
- `ganda kanban done 171`, commit the kitchen move
- `tw-pr` / `gh pr create` with explicit `--head` and `--base`; STOP; do not merge

## Results

### What changed
- Root `.editorconfig` sets TW0002 severity to none.

### How to validate
- Open a type with XML docs (e.g. `HttpFacilitatorClient`) — no TW0002 in Problems / build.
- TW0001 (kebab-case basenames) remains warning.
