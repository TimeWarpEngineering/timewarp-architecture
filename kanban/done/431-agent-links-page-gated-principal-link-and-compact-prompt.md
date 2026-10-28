# Agent links page: gated principal link and compact prompt

## Description

Follow-up to task 428 (merged). Living review of the merged Agent links page found two
usability problems:

1. Each link row shows the agent's principal id as plain text with no way to inspect the
   agent principal (`/Admin/Principals/{id}` — PrincipalDetailPage, policy
   `admin.principals.read`). A person without that permission must see plain text, not a
   dead link (FeedbackOwnerLink precedes the pattern of matching the link to the policy).
2. The after-registration openssl+curl prompt from task 428 rendered as a giant inline
   `<pre>`; a person will not read a screen-filling code block. It should be compact on the
   page and available in full on demand, in a modal.

## Requirements

- Agent row: inside `AuthorizeView(Policy="@PermissionIds.AdminPrincipalsRead")`, Authorized
  renders `FluentLink` to `/Admin/Principals/{id:D}` wrapping the id `<code>`;
  NotAuthorized renders plain text code. Same tag/qa surface for both branches.
- Prompt section keeps account id + copy; the giant pre is replaced by one sentence and a
  `View agent prompt` button. Full prompt lives in `AgentLinkPromptModal`
  (store-driven: `ApplicationState.SetActiveModal(ModalId)`), registered in
  `TimeWarpPage`'s `ModalContainers`; Activate re-derives the prompt (cascade auth +
  NavigationManager) so content builds only while the modal is active.
- Playwright flow reordered: copy account id while the page is uncovered, then open the
  modal and read/copy the prompt from `[data-qa=AgentLinkPrompt]` there.

## Checklist

- [x] Agent rows: gated FluentLink inside AuthorizeView; plain text otherwise
- [x] AgentLinkPromptModal (razor + isolated css), Activate-built prompt, CopyAgentPrompt kept
- [x] Page compacted: pre removed, css moved to the modal, `View agent prompt` button
- [x] TimeWarpPage ModalContainers registration
- [x] Playwright test updated and passing (modal-first flow)
- [x] `dev build` 0 warnings / 0 errors
- [x] `ganda repo audit` clean
- [x] `dev template-smoke` SUCCEEDED

## Notes

- PrincipalDetailPage route/policy: `[Page("/Admin/Principals/{PrincipalId:Guid}", Policy =
  PermissionIds.AdminPrincipalsRead)]`. The gate comparison is therefore against the same
  policy the registry filters destinations with (NavigationDestinations.ForAsync).
- No server change; list summary already carries `AgentPrincipalId` and `Status`.
- The modal cascade: `ModalController` renders inside `TimeWarpPage` under
  `CascadingAuthenticationState` (Routes.razor), so the modal can read the auth cascade.

## Session

- Created: ses_e7d6c83cfffebh1UD5myX7NDeE (2026-10-29)
