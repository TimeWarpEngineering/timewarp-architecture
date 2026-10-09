# Ask AI follow-up to 271: Cloudflare-style side panel, edit modes, and WebMCP parity

## Description

Task 271 shipped the catalog-to-tools mapping, the Ctrl-K Ask modal, WebMCP registration and
in-app approval. Steven wanted the findings from Cloudflare's dashboard "Ask AI" agent (Agent Lee)
folded into that work, but they never made it into 271 before it was built and closed. This task
carries them as follow-up work on top of what 271 shipped, plus a hard WebMCP parity requirement.

## Depends on

- 271

## Requirements

### HARD REQUIREMENT: WebMCP parity (Steven, 2026-10-09)

The in-app AI must be able to do everything WebMCP can do:

- the same tool set on each page;
- the same per-principal permission filtering;
- the same approval gating;
- no capability that exists on only one of the two paths.

Prove it with a test that compares the in-app AI tool list with the WebMCP tool list for each page
and each principal, and fails on any difference.

### Cloudflare Ask AI findings to evaluate and adopt

Take the "What to take for this task" list in the reference below as the starting scope. Design
first: compare each item with what 271 shipped and record a decision (adopt, adapt, or reject, with
the reason) before building.

#### Reference: Cloudflare dashboard "Ask AI" agent (observed 2026-10-07)

Grok Bot looked at Cloudflare's in-dashboard agent ("Agent Lee") in Steven's account. It looks like a reasonable generic pattern for this task. Screenshots are in `screenshots/` (see the Screenshots section).

**UI surface (271 design item 5).**
- The "Ask AI" button in the top bar opens a docked right side panel, 450px at 1280px wide. The page reflows beside it with no scrim, and the panel goes full-screen below about 880px.
- Header: a conversation title dropdown (history), New conversation, expand, and close.
- The empty state has a greeting plus five task-shaped suggestion cards, for example "Deploy a Worker", "Create an API token", and "Find my account ID".
- The input has no attachments. Typing `@` tags a resource by type (worker, zone, KV, R2, queue, workflow, Vectorize, pipeline).
- A dismissible privacy notice says chats are recorded.

**Page context without a chip (ties to task 282 "tools scoped to the current page").**
- The agent read the current URL, the page heading and the active nav item itself. Its visible reasoning shows this. There is no explicit context chip in the input.
- Our version would hand the page's catalog actions, items and flags in as context and as the scoped tool set.

**Approval (271 design item 2).**
- An "Edit behavior" switch in the footer offers two settings: "Ask before editing — Review and approve each change" and "Automatically edit — Always allow edits for this conversation".
- This maps directly onto `ApprovalRequiredAIFunction` as the default, plus an opt-in "approve for this conversation" escalation.

**Credentials and authorization (271 design item 3).**
- The agent's settings say "API token active" / "Cloudflare Agent can access your account" / "Configure permissions".
- It mints a **short-lived user API token for each conversation** (dozens of permissions, same-day expiry). They show up in My Profile > API Tokens as "Cloudflare Agent Conversation: <first prompt>…".
- So the agent acts through a scoped, expiring credential tied to the user and the conversation, not a standing key. Server authorization still decides everything.
- On the API tokens page it said it can explain, but cannot create, edit or revoke tokens. Some actions are deliberately not exposed as tools, which matches our `Visibility` = Human-only.

**Answer rendering.**
- Answers stream in stages ("Agent Lee is thinking…", then "Assembling data…") and render as markdown.
- A collapsible "See reasoning" section shows what it inspected.
- Each answer has copy, thumbs up and thumbs down buttons, plus a Support link.
- For a read-only question it offered no inline "run this" buttons. Changes happen as tool calls under the edit-behavior setting.

**What to take for this task.**
1. A docked, non-modal side panel opened from a top-bar button. Ctrl-K "ask" mode can open the same panel.
2. Page-scoped tools and context are derived automatically from the current route and page state.
3. Ask before editing is the default, with an explicit per-conversation auto-approve.
4. The agent runs under the user's own principal through a per-conversation, short-lived, scoped credential. It never bypasses `[EndpointAuthorize]`.
5. `@` resource references map to typed catalog parameters.
6. Show the reasoning and tool calls in a collapsible section. Add feedback buttons.
7. Treat chat recording and privacy notices as a template configuration concern (271 design item 4).

## Screenshots

All are in `screenshots/` in this task folder. The eight `0N-*.png` shots were captured
2026-10-09 at 1024px wide, not 1280px, so the panel looks wider relative to the page there. The
450px-at-1280px panel width comes from the earlier 2026-10-07 observation.

- [01-ask-ai-button.png](screenshots/01-ask-ai-button.png): the "Ask AI" button in the dashboard top bar.
- [02-side-panel-empty.png](screenshots/02-side-panel-empty.png): the docked side panel's empty state with greeting and suggestion cards.
- [03-at-tagging.png](screenshots/03-at-tagging.png): typing `@` in the input to tag a resource by type.
- [04-edit-modes.png](screenshots/04-edit-modes.png): the "Edit behavior" switch, Ask before editing vs Automatically edit.
- [05-streaming.png](screenshots/05-streaming.png): an answer in progress ("Compiling response…"), with the dismissible chat-recording privacy notice above the input.
- [06-answer-reasoning.png](screenshots/06-answer-reasoning.png): a finished answer with the collapsible "See reasoning" section.
- [07-answer-actions.png](screenshots/07-answer-actions.png): the copy, thumbs up/down and Support actions under an answer.
- [08-agent-settings.png](screenshots/08-agent-settings.png): agent settings menu ("API token active", "Cloudflare Agent can access your account", "Configure permissions").
- [cloudflare-ask-ai-empty-state.png](screenshots/cloudflare-ask-ai-empty-state.png): earlier (2026-10-07) capture of the empty panel.
- [cloudflare-ask-ai-answer.png](screenshots/cloudflare-ask-ai-answer.png): earlier (2026-10-07) capture of an answer.

## Checklist

- [x] Design (`design.md`): compare every reference item with what 271 shipped
- [x] Design: decide docked non-modal side panel vs the current Ctrl-K modal (and whether Ctrl-K ask opens the panel)
- [x] Design: edit-mode setting (Ask before editing default, per-conversation Automatically edit)
- [x] Design: per-conversation, short-lived, scoped credential under the user's principal (never bypasses `[EndpointAuthorize]`)
- [x] Design: `@` resource references to typed catalog parameters; streamed answers with collapsible reasoning and tool steps; copy, thumbs and Support actions; privacy notice and chat recording as configuration
- [x] WebMCP parity: in-app AI and WebMCP expose the same tools, filtering and approval on every page
- [x] Implement the design decisions
- [x] Test: per page and principal, the in-app AI tool list equals the WebMCP tool list
- [x] Tests for edit modes, the credential lifetime and scope, and the panel
- [x] `dev build` 0/0, `dev test`, `ganda repo audit` clean
- [x] Browser proof screenshots in the PR body

## Session

- Created: 1987146 (2026-10-09)
- Implementation: task-work oracle (2026-10-10)

## Results

Ask opens as a docked side panel from the top-bar Ask AI button and from Ctrl-K. At a viewport of at least 880px the dock is 450px wide and the page reflows beside it. Below 880px, and when Expand is on, the panel covers the viewport. There is no scrim.

Decisions are in `design.md`. Adopted: the dock, page-scoped tools, Ask before editing as the default with per-conversation Automatically edit, collapsible reasoning and tool steps, copy and thumbs, and a Support link. Adapted: `@` tokens come from page-context JSON (`@credential:{id}`, `@profile:{alias}`, `@siteSettingsVersion:{n}`), and the conversation credential is an SPA value (version-7 id, principal, scopes, expiry capped at the next UTC midnight) checked before the tool runs. HTTP still uses the signed-in session, so `[EndpointAuthorize]` stays the authorization gate. Rejected: a model-held bearer, a standing agent key, a context chip, a scrim, attachments, and token streaming. `RecordChats` stays false, so the privacy notice stays hidden until a deployment turns recording on. This task does not add a transcript store.

In-app AI and WebMCP share `CatalogAgentToolSet` for every `PageAgentScope` route, routes with no catalog tools, `/Feedback/item`, and `/FeedbackExtra`, four principals, and both edit modes. The comparison covers name, description, input schema, approval bit, and order, plus the shared `page_context` tool. An expired, out-of-scope, or foreign-principal credential refuses both drivers with the same message. Automatic mode skips the approval prompt and still refuses a bad credential.

Proof images for the PR body, beside this file:

- [ask-panel-docked.png](ask-panel-docked.png) — 1280px viewport, 450px dock, page beside the panel.
- [ask-panel-narrow.png](ask-panel-narrow.png) — 800px viewport, panel covers the width.
- [ctrl-k-ask-result-page.png](ctrl-k-ask-result-page.png) — Ctrl-K Ask opens the same panel and shows an answer.
- [ctrl-k-ask-not-configured-page.png](ctrl-k-ask-not-configured-page.png) — unconfigured relay, edit-mode footer still present.

`./bin/dev build`: 0 warnings, 0 errors (8.5s). `./bin/dev test`: exit 0, including web-spa-integration-tests 162/162 and web-spa-playwright-tests 3/3. `ganda repo audit`: Passed 31, Failed 0.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class CatalogAgent_Should
cd tests/container-apps/web/web-spa-playwright-tests && dotnet test -c Release
```

**Expect**

- `CatalogAgent_Should` passes. The parity method fails if any page, principal, or edit mode differs between the in-app tool list and the WebMCP tool list. Panel open, expand, automatic edit, and New conversation are read back from the store after each action. Expired, scope, and principal denials match `AgentConversationAuthority` on both drivers.
- Playwright passes. At 1280×800 the panel width is 450px and the app bar plus the panel fit the viewport. Expand, and an 800px viewport, make the panel the full viewport width. Close hides `[data-qa=AgentAsk]`; the Ask AI button opens it again. `[data-qa=AskPrivacyNotice]` is absent. The shots above are rewritten next to this file.
- `./bin/dev build` reports `0 Warning(s)` and `0 Error(s)`. `./bin/dev test` ends with `Tests completed successfully!`. `ganda repo audit` prints `Passed: 31 | Failed: 0`.

**Automated gate**

```bash
./bin/dev build && ./bin/dev test && ganda repo audit
```

**Not in scope**

- No transcript store. `RecordChats` defaults to false.
- The conversation credential is an SPA value. It is not an HTTP bearer and does not add an authentication scheme.
- Opening the pull request stays with the host `open-pr` node.

## Notes

- Requested by Steven on 2026-10-09 (voice call). Cloudflare observations by Grok Bot on 2026-10-07 and 2026-10-09, read-only, in Steven's Cloudflare account.
- WebMCP manual testing steps are being written up in task 291.
