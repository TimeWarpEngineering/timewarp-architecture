# Ask panel renders assistant replies as safe Markdown

## Problem (2026-10-17)

Akili's replies in the Ask panel show raw Markdown: `**navigate**`, `- ` bullets and
`[Settings](/Settings)` appear literally (Steve, screenshot 2026-10-17). The panel is
`source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor`
(+ `.razor.cs`, `.razor.css`). It wraps the library `Microsoft.AspNetCore.Components.AI.MessageList`
(package `Microsoft.AspNetCore.Components.AI` 0.1.0-preview.1.26466.103, pinned in
`Directory.Packages.props`, referenced by `web-spa.csproj`) and `MessageInput`, with no renderer for
assistant text, so the library's default text rendering shows the raw characters.

## Findings (investigated read-only, public XML docs only)

- **The library DOES offer a content-render hook.** `Microsoft.AspNetCore.Components.AI.xml` in the
  package documents `BlockRenderer<TBlock>`: "Registers custom markup for a block type inside a
  MessageList. The most recently registered renderer that matches a block wins." It has `ChildContent`
  and a `When` predicate. `MessageListContext.RenderBlock` documents that registered renderers take
  precedence over the built-in ones. `RichContentBlock` is the assistant text block: `RawText` (every
  fragment so far) and `Content` (structured), fed by `AppendText` while streaming.
- `AgentAsk.razor` already registers `BlockRenderer` for `FunctionInvocationContentBlock` and
  `FunctionApprovalBlock` inside `MessageList.ChildContent`. A `BlockRenderer
  TBlock="RichContentBlock"` slots in beside them. No fork and no reflection is needed.
- Open point for the implementer: confirm from the public API whether user turns also arrive as
  `RichContentBlock`. If so, use `When`, or the turn role if the API exposes it, so user messages
  stay plain text. If the API gives no way to tell, stop and record it here, then file the upstream
  ask.
- Existing renderer (task 353): `components/composites/safe-markdown.cs` (`SafeMarkdown`, Markdig AST
  to render tree, no `MarkupString`), `safe-markdown-policy.cs` (`SafeMarkdownPolicy`: raw HTML off,
  link allow-list, exact-match image allow-list), `MarkdownImage.razor`, `FeedbackBody.razor`.
  Parameters today: `Markdown`, `AllowedImageSources`, `DataQa`. Anchors render through
  `OpenAnchor` as a plain `<a href rel="nofollow noopener noreferrer">`, with no `target` and no
  State dispatch. Soft line breaks render as `<br>`.
- Routing: in-app navigation is `RouteState.ChangeRoute` (see
  `AgentAsk.razor` login redirect via `NoSubRouteState.ChangeRoute`, and
  `AskSettingsMenu.razor.cs`). Repo rule: every user interaction is a TimeWarp.State action, and
  TWA0026 forbids page-local navigation.

## Requirements

- Assistant replies render through `SafeMarkdown` (reuse it; no second Markdown pipeline). User
  messages stay plain text.
- Links:
  - In-app relative links such as `/Settings` navigate in-app by dispatching the routing State
    action (`RouteState.ChangeRoute`). No full reload, no page-local navigation method. Add an
    optional link-click hook on `SafeMarkdown` (a parameter, so feedback bodies keep plain
    anchors), not a fork.
  - External `http(s)` links open in a new tab with `rel="noopener noreferrer"`, only if
    `SafeMarkdownPolicy` allows them. Otherwise render as text.
  - Images are off for Ask (empty allow-list).
- Soft line breaks: decide whether Ask keeps the feedback behaviour (soft break to `<br>`) or
  standard CommonMark (soft break is a space). Model replies are CommonMark, so lean to standard.
  If they differ, add a parameter on `SafeMarkdown` instead of forking, and record the choice in
  Results.
- Streaming: a partial reply renders correctly while it streams. Incomplete Markdown (an unclosed
  `**`, a half-typed link, an open fence) must not throw or flicker badly, and the block re-renders
  as tokens arrive from `RichContentBlock.RawText`.
- Tests (don't mock your friends):
  - A Playwright test in `tests/container-apps/web/web-spa-playwright-tests/` (the Ask lane, next to
    `ask-surface-playwright-tests.cs`) showing that bold, a list and a link render as elements, that
    an in-app link navigates via the State action without a reload, and that a raw-HTML reply
    renders as text.
  - Unit tests for any new `SafeMarkdown` parameter, in the SafeMarkdown suite under
    `tests/container-apps/web/web-spa-integration-tests/features/feedback/`.
  - Workers must not run `dev run` (worktree AppHosts share user secrets).
- Gates: `dev check-version` (bump `<Version>` and pins in the same commit), `dev build` 0
  warnings and 0 errors, `dev test`, `ganda repo audit`.

## Goal (implementer restatement)

Akili's answers in the Ask panel should read as formatted text, not raw Markdown symbols. Each
assistant text block goes through the existing `SafeMarkdown` component (one Markdown pipeline,
same safety rules). What the person typed stays plain text.

Done means:

- A `BlockRenderer<RichContentBlock>` in `AgentAsk.razor` renders assistant blocks with
  `SafeMarkdown` and user blocks as plain text, keeping the library's message bubble markup.
- `SafeMarkdown` gains opt-in parameters (feedback bodies keep today's output): an in-app link
  callback (Ask dispatches `RouteState.ChangeRoute`, no reload), external links in a new tab with
  `noopener noreferrer`, and a CommonMark soft-break switch. Ask passes no image allow-list.
- A streaming or half-typed reply (open `**`, half link, open fence) renders without throwing.
- Unit tests cover each new parameter; a Playwright test shows bold, list and link as elements,
  an in-app link navigating without a reload, and raw HTML as text.
- Gates pass: `dev check-version` (with bump), `dev build` 0/0, `dev test`, `ganda repo audit`.

## Checklist

- [x] Confirm the hook: `BlockRenderer<RichContentBlock>` inside `MessageList` replaces the default
      text rendering, and whether user turns can be told apart. If there is no hook, STOP: record the
      finding here and file the upstream ask (no fork, no reflection)
- [x] Register the `RichContentBlock` renderer in `AgentAsk.razor` using `SafeMarkdown`; user
      messages stay plain text
- [x] `SafeMarkdown`: link-click hook for in-app links (State action), external links in a new tab
      with `noopener noreferrer` when the policy allows, images off
- [x] Decide and record soft-break behaviour; add a parameter if it differs from feedback
- [x] Streaming: partial and malformed Markdown render safely and update per token
- [x] Playwright test: bold, list and link as elements; in-app link navigates without reload; raw
      HTML shows as text
- [x] Unit tests for new parameters
- [x] Gates: `dev check-version`, `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 1829706 (2026-10-17, cockpit, from Steve's screenshot)
- 2026-10-17: implement oracle (ganda task work, headless Claude) — hook confirmed, renderer + SafeMarkdown params + tests; gates green
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-17T02:15:50Z
- Review oracle: claude-opus-5-5 (headless ganda task work), effort 2, roster general, session not reported — 2026-10-17

## Notes

- Sequencing: run AFTER task 303-001 merges. 303-001 is changing the Ask panel now, so both touch
  `AgentAsk.razor`. This is stated here and not as a `## Depends on` line, because ganda task 412
  (predecessor check bug) could block claiming after the merge.
- Design questions for Steve if the soft-break or the new-tab behaviour is contested: one question
  at a time.

## Results

### Hook finding

`BlockRenderer<RichContentBlock>` inside `MessageList` replaces the default text rendering:
`MessageListContext.RenderBlock` checks registered renderers first. `MessageList` re-renders on
every `ContentBlock.OnChanged`, so the fragment re-runs with the new `RawText` per token. User
turns are told apart by `ContentBlock.Role` (`ChatRole.User`). No upstream ask was needed. The
renderer keeps the library's `sc-ai-message--{user|assistant}` / `__bubble` / `__content` markup,
so the existing avatar and bubble CSS still apply.

### What changed

- `AgentAsk.razor`: a `RichContentBlock` renderer. Assistant blocks go through `SafeMarkdown`
  (`DataQa=AskReplyMarkdown`); user blocks render `RawText` as plain text. App-relative links call
  `OpenAppLinkAsync`, which dispatches `RouteState.ChangeRoute` and leaves the panel open.
  `AgentAsk.razor.css`: `white-space: normal` on the markdown root (the library's content box is
  `pre-wrap`) plus block spacing.
- `SafeMarkdown` (opt-in parameters, so feedback bodies render exactly as before):
  - `OnAppLinkClick` (`EventCallback<string>`): `/path` anchors keep `href` and `rel`, the click is
    `preventDefault`ed and the callback gets the URL.
  - `ExternalLinksInNewTab`: `http(s)` anchors get `target="_blank"`. `rel` stays
    `nofollow noopener noreferrer`. Policy-refused links still render as text.
  - `SoftBreaksAsSpaces`: a soft break renders as a space; hard breaks stay `<br>`.
  - Images: Ask passes no allow-list, so every image is refused (shown as a link or alt text).
- Soft-break decision: Ask uses standard CommonMark (a soft break is a space) because model replies
  are CommonMark. Feedback keeps `<br>`.
- Streaming bug fixed: an open fence with no lines yet (a reply ending in a bare ```` ``` ````)
  threw `ArgumentNullException` in `SafeMarkdown` (`code.Lines.Lines` is null). It now renders an
  empty `<pre><code>`. The test renders every prefix of a reply.
- Fake upstream: "markdown sample" answers with `MarkdownSample` (bold, list, `/Login` link,
  external link, raw `<b>`), for the Playwright test.

### Tests

- `safe-markdown-render-tests.cs`: soft break default `<br>`, `SoftBreaksAsSpaces`,
  `ExternalLinksInNewTab` (only http(s) gets `target`), `OnAppLinkClick` with a real click
  dispatched through a bare `Renderer` (only the `/Login` link has a handler, and the callback gets
  `/Login`), no handler without a callback, and every prefix of a streaming reply.
- `ask-markdown-playwright-tests.cs` (`AskMarkdown_Given_Wasm`): `strong`, two `li`, and links
  render as elements; the external link has `target=_blank` and `noopener noreferrer`; raw HTML is
  text (no `[data-qa=AskRawHtml]` element); the user's `**not bold**` stays literal; clicking
  `/Login` reaches `/Login` with a `window` marker intact (no reload).

### Gates

- `dev build`: 0 warnings, 0 errors.
- `dev test`: 23 of 23 projects passed (web-spa-integration 300, web-spa-playwright 20).
- `dev check-version`: source 2.0.0-beta.20 is already ahead of nuget.org 2.0.0-beta.19, so no
  bump was needed.
- `ganda repo audit`: clean before commit (see commit log).

### How to validate

Smoke:

```bash
./bin/dev build
./bin/dev test
```

Expect: the build shows 0 warnings and 0 errors, and all projects pass, including
`SafeMarkdown_Should_` (web-spa-integration-tests) and
`AskMarkdown_Given_Wasm.Reply_Should_RenderMarkdown_And_NavigateInApp` (web-spa-playwright-tests).
The screenshot
`tests/container-apps/web/web-spa-playwright-tests/bin/Release/net11.0/playwright-screenshots/AskMarkdown_Given_Wasm/ask-markdown-reply.png`
shows bold, a bullet list, two underlined links, and the raw `<b ...>raw</b>` as literal text,
while the user bubble shows `**not bold**` literally.

### Review disposition

- Rounds: 1. Effort 2 (by-diff, 446 lines). Roster: general.
- Final counts: bug 0, suggestion 0, nit 2 (both wontfix). Open: 0.
- Disposition: **accepted-exceptions**.
  - M1: the browser "no reload" check cannot tell State routing from Blazor's link
    interception. The unit test covers the callback wiring.
  - M2: Ctrl/Cmd+click on an in-app Ask link routes in place, because Blazor's preventDefault is
    static.
- The reviewer re-ran the gates: `ganda repo audit` passes, `dev build` 0/0, `dev test` 23/23,
  `dev check-version` exit 0.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/disposition.md`.
