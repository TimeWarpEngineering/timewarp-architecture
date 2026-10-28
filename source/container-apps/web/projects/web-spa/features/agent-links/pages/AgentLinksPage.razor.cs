#region Purpose
// Registers the Agent Links route and authorize policy; markup lives in AgentLinksPage.razor.
#endregion

#region Design
// Human chrome for the optional approve/deny flow (task 205). Agents request links via API, not
// this page. Policy AgentLinkManageSelf is self-service (RolePermissionSeed).
// Task 428: the page also explains the link, shows the signed-in principal id
// (IdentitySessionAuthenticationStateProvider.PrincipalIdClaimType, projected from
// GetCurrentSession), and offers the openssl + curl prompt (AgentLinkOnboardingPrompt).
// Task 431: the prompt lives in AgentLinkPromptModal (opened with ApplicationState.SetActiveModal)
// so the page stays a short read; the list links each agent principal id to
// /Admin/Principals/{id} only when the viewer holds the detail page's policy
// (admin.principals.read) and shows plain text otherwise. Copy buttons dispatch
// AgentLinksState.CopyOnboarding. No AOT client ships with that prompt.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

[Page("/AgentLinks", Policy = PermissionIds.AgentLinkManageSelf, Navigable = true, Description = "Manage agent links for the signed-in person.", NavOrder = 30)]
[Authorize(Policy = PermissionIds.AgentLinkManageSelf)]
partial class AgentLinksPage;
