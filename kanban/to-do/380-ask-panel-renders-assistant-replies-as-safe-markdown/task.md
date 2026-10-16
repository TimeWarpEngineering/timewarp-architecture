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

## Checklist

- [ ] Confirm the hook: `BlockRenderer<RichContentBlock>` inside `MessageList` replaces the default
      text rendering, and whether user turns can be told apart. If there is no hook, STOP: record the
      finding here and file the upstream ask (no fork, no reflection)
- [ ] Register the `RichContentBlock` renderer in `AgentAsk.razor` using `SafeMarkdown`; user
      messages stay plain text
- [ ] `SafeMarkdown`: link-click hook for in-app links (State action), external links in a new tab
      with `noopener noreferrer` when the policy allows, images off
- [ ] Decide and record soft-break behaviour; add a parameter if it differs from feedback
- [ ] Streaming: partial and malformed Markdown render safely and update per token
- [ ] Playwright test: bold, list and link as elements; in-app link navigates without reload; raw
      HTML shows as text
- [ ] Unit tests for new parameters
- [ ] Gates: `dev check-version`, `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 1829706 (2026-10-17, cockpit, from Steve's screenshot)

## Notes

- Sequencing: run AFTER task 303-001 merges. 303-001 is changing the Ask panel now, so both touch
  `AgentAsk.razor`. This is stated here and not as a `## Depends on` line, because ganda task 412
  (predecessor check bug) could block claiming after the merge.
- Design questions for Steve if the soft-break or the new-tab behaviour is contested: one question
  at a time.

## Results

(none yet)
