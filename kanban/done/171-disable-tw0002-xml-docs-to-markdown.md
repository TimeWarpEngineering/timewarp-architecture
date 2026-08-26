# Disable TW0002 XML docs to markdown

## Description

Turn off TW0002 (`XmlDocsToMarkdownAnalyzer`: "XML documentation should be moved to a markdown file").
With AI-assisted documentation, the nag is no longer useful.

## Checklist

- [x] `.editorconfig`: `dotnet_diagnostic.TW0002.severity = none`
- [x] Board close: `ganda kanban done 171` so origin-home matches shipped product
- [x] Kanban-only PR; STOP (do not merge)

## Session

- Implementation: grok (2026-08-06)
- Cockpit: Grok close request (2026-08-26) — product already on origin/master; remaining is board close
- Implementer: grok headless profile=implementer-grok — board close + kanban-only PR (2026-08-26)
- Board: `ganda kanban done 171` (claim + worktree remain for PR)

## Notes

Product already shipped on origin/master as `dc9fc273` (`build(editorconfig): disable TW0002 XML-docs-to-markdown nag (171)`). No dedicated PR; it rode with the later analyzer wave (PR 298 / task 172).

Remaining work is **board hygiene only** on this same task id:

- Do **not** change `.editorconfig` or any product file
- Do **not** create a sibling close/hygiene task
- `ganda kanban done 171`, commit the kitchen move
- `tw-pr` / `gh pr create` with explicit `--head` and `--base`; STOP; do not merge

## Results

Product already on origin/master as `dc9fc273` (`build(editorconfig): disable TW0002 XML-docs-to-markdown nag (171)`). Root `.editorconfig` sets `dotnet_diagnostic.TW0002.severity = none`. No dedicated product PR; it rode with the later analyzer wave (PR 298 / task 172).

This close is **kanban-only**. `ganda kanban done 171` moves the kitchen from `kanban/in-progress/` to `kanban/done/` so origin-home matches the shipped product. No `.editorconfig` or other product files changed in this PR.

### How to validate

**Smoke**

```bash
test ! -e kanban/in-progress/171-disable-tw0002-xml-docs-to-markdown.md && echo no-in-progress-171
# Expect: no-in-progress-171

test -f kanban/done/171-disable-tw0002-xml-docs-to-markdown.md && echo ok-171
# Expect: ok-171

ganda kanban path 171
# Expect: …/kanban/done/171-disable-tw0002-xml-docs-to-markdown.md

rg -n "dotnet_diagnostic.TW0002.severity" .editorconfig
# Expect: none (already on origin/master as dc9fc273)

rg -n "dotnet_diagnostic.TW0001.severity" .editorconfig
# Expect: warning (kebab-case basenames still build-breaking)

git diff origin/master...HEAD --stat
# Expect: only kanban/ paths (171 column move)
```

**Expect**

- `ganda kanban` / `ganda reposet show live` do not list architecture 171 as in-progress.
- Task 171 stays id **171** in `kanban/done/` with Results and this How to validate.
- TW0002 remains `none` on origin/master; TW0001 remains `warning`.
- This PR is kanban-only; no product code in the diff. STOP; do not merge from this worktree.
