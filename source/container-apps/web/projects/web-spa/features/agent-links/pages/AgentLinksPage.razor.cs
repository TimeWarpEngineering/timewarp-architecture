#region Purpose
// Registers the Agent Links route and authorize policy; markup lives in AgentLinksPage.razor.
#endregion

#region Design
// Human chrome for the optional approve/deny flow (task 205). Agents request links via API, not
// this page. Policy AgentLinkManageSelf is self-service (RolePermissionSeed).
// Task 428: the page also explains the link, shows the signed-in principal id
// (IdentitySessionAuthenticationStateProvider.PrincipalIdClaimType, projected from
// GetCurrentSession), and offers a copyable openssl + curl prompt (AgentLinkOnboardingPrompt).
// Copy buttons dispatch AgentLinksState.CopyOnboarding. No AOT client ships with that prompt.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

[Page("/AgentLinks", Policy = PermissionIds.AgentLinkManageSelf, Navigable = true, Description = "Manage agent links for the signed-in person.", NavOrder = 30)]
[Authorize(Policy = PermissionIds.AgentLinkManageSelf)]
partial class AgentLinksPage;
