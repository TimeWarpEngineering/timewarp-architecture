# Feedback items record the app version and page at filing

## Description

A feedback item records owner, kind, title, body and filed-at. It does not record which version of the app the filer was running, or which page they were on. A bug report triaged days later cannot tell whether a later release already fixed it, and the "capture this page" screenshot is only an attachment with no page path.

The feedback intake tool (a DevTools plugin in timewarp-ganda) reads items from many app instances. The connection tells it which instance an item came from. It cannot tell it the version or the page.

Origin: timewarp-jenzi task 013 (research: cross-repo kanban board and feedback intake as DevTools plugins), `report/analysis.md` §Findings 5 and recommendations I7 and I10 (A2).

## Requirements

- On filing, the server records the app version: the web-server assembly's informational version (the same value the app reports elsewhere, not the stale hard-coded `.well-known` card version). The client does not send it.
- The client sends the page path it was on when the filer opened the feedback form (path only, no query string, so no tokens or PII in query values leak). The server validates it as a relative path with a length limit and stores it. It is optional: a filing without it still succeeds.
- Both are fixed after filing, like the other filing fields.
- The admin detail and admin list item return both. The admin feedback view shows them. The filer's own views do not need them.
- Existing rows keep null for both.

## Acceptance criteria

- [ ] Filing stores the server version and the submitted page path. Handler test.
- [ ] A page path with a query string or an absolute URL is stored as the path only, or refused, per the validator. Validator test.
- [ ] The admin detail and list return both; an agent token with `feedback:read` sees them.
- [ ] The admin view shows both.
- [ ] Migration adds nullable columns.

## Checklist

- [ ] Domain fields and `FeedbackItem.File` arguments
- [ ] Submit contract (page path), validator, handler (version from the server)
- [ ] EF configuration and migration; in-memory store
- [ ] Admin contracts and view
- [ ] Tests
- [ ] Gates

## Session

- Created: 518093 (2026-11-24), filed from timewarp-jenzi task 013.

## Notes

- Domain: `features/feedback/feedback-item-domain.cs` (`File`). Submit: `features/feedback/submit-feedback/`.
- No "about" endpoint is asked for. The intake tool's instance registry already names each instance and its environment.
- Related: 520, 331, 378; timewarp-ganda 687 and the intake plugin task there.
