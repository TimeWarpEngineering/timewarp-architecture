# Feedback body renders safe markdown, so pasted images show inline instead of raw markdown

## Problem (2026-10-14)

The feedback permalink (`web-spa/features/feedback/pages/FeedbackItemPage.razor`) shows the body as
plain text (`<p>@item.Body</p>`). Pasting an image (task 295) inserts markdown
`![image.png](/api/Feedback/attachments/<id>)` into the body. So the page shows that **raw markdown
text** and then the same image again from the separate attachments list.

## Requirements

- Render the body as **safe markdown**, on the server or the client (record which). Use a
  well-known parser, such as Markdig on the server or an established client library.
  - **No raw HTML** passthrough (disable it, or sanitize to an allow-list).
  - Links: `http(s)` and relative only. Never `javascript:` or `data:`.
  - Images: **only this item's own attachment URLs** (`/api/Feedback/attachments/{id}` where the id
    belongs to the item). Anything else renders as a plain link or is dropped.
- Attachments **referenced inline** are not repeated in the attachments list below. Unreferenced
  attachments are still listed.
- Apply the same rendering wherever a feedback body is shown: the filer permalink, the admin detail
  page (`FeedbackAdminItemPage`), and thread replies if they accept markdown. Use one shared
  component; don't duplicate.
- A missing or 404 attachment shows a small "image unavailable" placeholder, not a broken image
  (pairs with task 352).
- **Tests:**
  - renderer unit tests: XSS vectors (script, `on*` attributes, `javascript:` links, raw HTML),
    allowed and blocked image sources, inline-reference dedupe;
  - a Playwright check that a pasted image shows once, inline.

## Checklist

- [ ] Shared safe-markdown renderer (raw HTML off, link/image allow-list)
- [ ] Inline-referenced attachments not duplicated
- [ ] Applied on filer + admin views (+ replies if markdown)
- [ ] Tests incl. XSS vectors
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, from Steve's screenshot)
