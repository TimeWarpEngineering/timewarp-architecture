# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** commit a21068bf5 (AgentAsk.razor/.css, AskSettingsMenu.razor/.cs/.css, ask-settings-menu-tests.cs, two Playwright suites)

## Summary

Adds a FluentMenu settings button to the Ask composer whose status item derives from
`AgentSurfaceState.Conversation` via `AgentConversationAuthority.IsExpired` (the same test the
header uses at AgentAsk.razor:113), a New conversation renewal when expired, and Configure
permissions that closes the panel then dispatches `RouteState.ChangeRoute` to `/AgentLinks`,
with the cross-slice edge declared via `[CrossSliceReference]`. Requirements met; tests cover
the three states, the route, placement, keyboard/ARIA and the signed-in Active path. Low risk.

Considered and not raised: status computed at render time (documented; matches header banner);
tab stop before textarea (forced by MessageInput's TrailingActions replacing send; documented
in AgentAsk Design and Results); privacy link omitted (optional in spec, rationale recorded).

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskSettingsMenu.razor:8
- Description: The Design comment's first line runs ~140 columns while the rest of the region wraps near 100, unlike surrounding regions.
- Suggestion: Wrap it to match.
- Status: open
