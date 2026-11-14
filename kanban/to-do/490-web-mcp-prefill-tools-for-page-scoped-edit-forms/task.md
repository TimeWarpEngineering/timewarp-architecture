# WebMCP prefill tools for page-scoped edit forms

## Description

Let an agent draft a form for a person to review and submit, without controlling form inputs
directly. Every effect stays an action.

- For each page-scoped submit action in `[Page(AgentActions=...)]` whose page has an edit
  form, generate a `Prefill.<Action>` agent tool. Schema = the Command schema with every field
  optional.
- The tool dispatches a `Prefill<X>` action that stores an immutable *suggestion* in state
  (legal: via an action). The page applies only the supplied fields onto its working copy
  (from the edit-form pattern task), then dispatches an action to clear the suggestion.
- If the working copy has unsaved edits, ask before applying.
- Off-page: reuse the existing `navigateTo` offer from `CatalogAgentToolSet`.
- Approval: no server effect, so prompt only in "Ask before editing" mode (like `navigate`).
- Mark agent-filled fields visibly (design TBD).
- Same tools available to in-app Ask and WebMCP (`web-mcp-publisher.cs`).

Background (research 2026-11-14): vercel-labs/ui (UI SDK) lets agents drive widgets
(`snapshot`/`query`/`act`). We rejected widget control; the only real gap it exposed was the
"agent drafts, person commits" handoff, which this task covers.

## Depends on

- 489-002

## Checklist

- [ ] Suggestion state + apply/clear on Profile (first consumer)
- [ ] Tool generation from page scope
- [ ] Approval / edit-mode handling
- [ ] Dirty-form conflict handling
- [ ] Tests (catalog-agent-webmcp-tests)

## Related follow-ups (not in scope)

- Analyzer: every ID-typed parameter on an agent-visible action has a
  `[ReferenceableResource]` list query with matching `IdTypeName`.
- `readOnlyHint` / `untrustedContentHint` on `WebMcpToolDescriptor`.
- Redaction markers for what `page_context` reads off the page.

## Results
