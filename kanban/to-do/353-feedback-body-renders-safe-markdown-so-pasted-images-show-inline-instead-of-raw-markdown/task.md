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

- [x] Shared safe-markdown renderer (raw HTML off, link/image allow-list)
- [x] Inline-referenced attachments not duplicated
- [x] Applied on filer + admin views (+ replies if markdown)
- [x] Tests incl. XSS vectors
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, from Steve's screenshot)
- 2026-10-14 implementer (ganda task work): renderer, shared component, both views, tests, gates.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T13:17:44Z

## Results

**Rendering is client-side** (in the SPA). The server stores and returns the raw markdown.

- `components/composites/safe-markdown-policy.cs`: `SafeMarkdownPolicy` parses with Markdig
  1.4.0 and `DisableHtml()`. It holds the link allow-list (`http(s)`, `/path` but not `//host`,
  and `#fragment`; whitespace, control characters, and `\` are refused) and the exact-match image
  allow-list. `ReferencedSources` reports which of the given paths the body shows inline.
- `components/composites/safe-markdown.cs`: the `SafeMarkdown` component walks the Markdig AST
  into the Blazor render tree. Text goes in through `AddContent` and URLs through `AddAttribute`.
  There is no `MarkupString`, so no HTML string from Markdig reaches the DOM. A refused link
  renders as plain text. A refused image renders as a plain link when the URL is safe, and as its
  alt text otherwise. Soft line breaks render as `<br>`, so plain-text bodies keep their lines.
- `components/composites/MarkdownImage.razor`: an allowed image. When the load fails
  (`@onerror`, e.g. a 404) it swaps to a small "image unavailable" placeholder. Inline images and
  listed image attachments both use it.
- `components/composites/FeedbackBody.razor` (the one shared component): the body as
  `SafeMarkdown`, then only the attachments the body does not reference. Only this item's own
  image attachments may render inline. The composer writes `![name](path)` for every upload,
  including non-images, which render as inline links. So any inline reference (image or link) to
  one of the item's attachments leaves it out of the list.
- `FeedbackItemPage` (filer) and `FeedbackAdminItemPage` (admin) both use `FeedbackBody`. The
  per-page attachment-list markup and CSS were removed.
- **Thread replies stay plain text.** The reply and note boxes take no attachments and no
  markdown composer, so they do not accept markdown.
- Tests: `tests/container-apps/web/web-spa-integration-tests/features/feedback/safe-markdown-render-tests.cs`
  (9 cases). They cover `<script>`, inline HTML with `on*`, `<iframe>` and `<div>` blocks,
  `javascript:` in mixed case, a tab inside the scheme, and an entity-encoded scheme. They also
  cover `data:`, `vbscript:`, `file:`, `//host`, `/\host`, autolinks, alt-text attribute
  injection, the allowed/other/external image sources, and dedupe for both the image and the
  file-link cases. The Playwright `PasteAndUpload_Should_ShowAttachmentsOnTheItem` now asserts
  that the permalink shows the pasted image exactly once, inline. It also checks there is no raw
  `![` text, that the text file is an inline link, and that nothing is listed twice.
- Gates: `dev build` 0/0 · `dev test` all green · `ganda repo audit` pass ·
  `dev check-version` OK (2.0.0-beta.20 is ahead of NuGet) · `dev template-smoke` succeeded.
- Not fixed here (pre-existing, outside this task): in the Playwright run, text typed with
  `FillAsync` into Details before an upload was replaced by the appended markdown. The body
  held only the two `![…]` lines. The composer's `AppendMarkdown` reads `Draft.Body`, which may
  not have picked up the typed value yet. Worth a follow-up check with real typing.

### Implementation review

- Rounds: 1 · effort 2 · roster: general (review oracle, claude-opus-5-5)
- Final counts: bug 0 · suggestion 0 · nit 3 fixed · 0 open · 0 wontfix
- Disposition: **clean**. The review found no security issues. The nits were fixed on this task:
  dedupe ignores links nested in image alt text (with a test), and two Design comments were
  corrected or reflowed. Post-fix: `dev build` 0/0, SafeMarkdown suite 9/9, `ganda repo audit` pass.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`

### How to validate

**Smoke:**

```bash
dotnet run --project tests/container-apps/web/web-spa-integration-tests -- --filter-class SafeMarkdown
dotnet run --project tests/container-apps/web/web-spa-playwright-tests -- --filter-method PasteAndUpload
```

Manual check: `dev run`, sign in, and file feedback at `/Feedback` with a pasted image and a text
like `hi <script>alert(1)</script> [x](javascript:alert(1))`. Then open the permalink, and
`/Admin/Feedback/{id}`.

**Expect:**

- The unit suite shows `total: 9, failed: 0`. The Playwright run shows `total: 1, failed: 0`.
- On the permalink and the admin page, the pasted image shows **once**, inline in the body. No
  raw `![…](…)` text. It is not repeated in the list under the body.
- `<script>` shows as literal text and no alert runs. The `javascript:` link renders as plain
  text "x" with no anchor.
- An attachment whose bytes are gone shows a dashed "image unavailable" box, not a broken image.
