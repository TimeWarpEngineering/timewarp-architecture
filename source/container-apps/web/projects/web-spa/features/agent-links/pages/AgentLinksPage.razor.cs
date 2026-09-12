#region Purpose
// Registers the Agent Links route and authorize policy; markup lives in AgentLinksPage.razor.
#endregion

#region Design
// Human chrome for the optional approve/deny flow (task 205). Agents request links via API, not
// this page. Policy AgentLinkManageSelf is self-service (RolePermissionSeed).
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

[Page("/AgentLinks", Policy = PermissionIds.AgentLinkManageSelf)]
[Authorize(Policy = PermissionIds.AgentLinkManageSelf)]
partial class AgentLinksPage;
