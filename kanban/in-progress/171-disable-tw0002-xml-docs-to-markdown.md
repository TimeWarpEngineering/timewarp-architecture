# Disable TW0002 XML docs to markdown

## Description

Turn off TW0002 (`XmlDocsToMarkdownAnalyzer`: "XML documentation should be moved to a markdown file").
With AI-assisted documentation, the nag is no longer useful.

## Checklist

- [x] `.editorconfig`: `dotnet_diagnostic.TW0002.severity = none`

## Session

- Implementation: grok (2026-08-06)

## Results

### What changed
- Root `.editorconfig` sets TW0002 severity to none.

### How to validate
- Open a type with XML docs (e.g. `HttpFacilitatorClient`) — no TW0002 in Problems / build.
- TW0001 (kebab-case basenames) remains warning.
