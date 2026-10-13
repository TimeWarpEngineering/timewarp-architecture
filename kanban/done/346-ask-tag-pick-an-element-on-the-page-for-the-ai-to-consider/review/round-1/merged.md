# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/ask-tag-chat-client.cs:89
- Description: Chips marked sent before the request succeeds; MarkSent moves all Attached, not the snapshot sent.
- Suggestion: Mark after success; pass sent ids.
- Source: general
- Disposition notes: Fixed in 657470909 — MarkSent after the response / first streamed update only; MarkSent.Action carries Ids. Test `Keep_Chips_Attached_When_The_Request_Fails`.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:242
- Description: Instructions embed page_context.tags once per agent build; stale after first send.
- Suggestion: Refresh instructions per request.
- Source: general
- Disposition notes: Fixed in 657470909 — AskTagChatClient sets Instructions on a cloned ChatOptions from a Func per request (UIAgent passes instructions via ChatOptions, confirmed by decompile). Integration + Playwright assert second turn sees the tag.

### M3 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/ask-tag.ts:215
- Description: Window keydown capture swallowed Tab/Enter inside the Ask panel.
- Suggestion: Exempt insideAsk for native keys.
- Source: general
- Disposition notes: Fixed in 657470909 — Enter/Tab pass natively inside the panel; Esc cancels everywhere. Playwright: Enter on Cancel cancels pick.

### M4 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/ask-tag-state/ask-tag-state.session-changed.cs
- Description: Browser pick not torn down on session change / navigation / dispose; pick root cached.
- Suggestion: Cancel JS pick; re-query root.
- Source: general
- Disposition notes: Fixed in 657470909 — SessionChanged calls CancelPickAsync; AgentAsk dispatches CancelPick on LocationChanged and dispose; root re-queried.

### M5 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/ask-tag-state/ask-tag-state.start-pick.cs
- Description: [TrackAction] on StartPick made the app-wide busy signal on during the whole pick.
- Suggestion: Use AskTagState.IsPicking.
- Source: general
- Disposition notes: Fixed in 657470909 — [TrackAction] removed; panel and chips read IsPicking; StartPick re-renders subscribers after setting IsPicking.

### M6 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-playwright-tests.cs:181
- Description: No E2E proof payload is in the outgoing message; privacy filters and keyboard choose untested.
- Suggestion: Assert request body; add privacy probes and Enter path.
- Source: general
- Disposition notes: Fixed in 657470909 — records completions POST bodies (heading + @my-feedback token), private/aria-hidden probes never hinted or hovered, Tab+Enter chooses.

### M7 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor:138
- Description: Admin row pickable only over the title link though outline covers the row.
- Suggestion: Mark the row or map row hits to the mark.
- Source: general
- Disposition notes: Fixed in 657470909 — candidateAt maps a hit in tr/[role=row] to the mark in that row; describe takes row text.

### M8 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/ask-tag.ts:359
- Description: data-qa fallback climbed to ancestors outside the pick root.
- Suggestion: Own data-qa only.
- Source: general
- Disposition notes: Fixed in 657470909.

## Duplicates / conflicts

- None (single reviewer).
