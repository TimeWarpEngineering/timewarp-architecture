#region Purpose
// Code-behind for the admin role detail page: [Page] route with RoleId and read policy.
#endregion

#region Design
// Task 206: membership editor lives here, not on the list. Policy is AdminRolesRead so a
// read-only principal can view the bundle; Save and checkbox writes require AdminRolesManage
// (honest disable in markup). RoleId is generated onto the partial from the route token.
// Protected-core UI lock uses PermissionIds.IsProtectedCoreLocked; server SetRolePermissions
// still 409s a strip. Last-admin is SetPrincipalRoles, not this page.
// GetRole stores the response. The page copies both permission and agent-scope sets from it
// when the route id is loaded. A successful permission save recopies permissions. A successful
// agent-scope save recopies scopes. Those sets are not store fields. Save sends the command
// built from the page set. A failed write does not copy again.
// The name and description form is RoleEditForm when the caller can manage the role.
// A read-only caller sees the description as one DefinitionList item when it is not blank.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

[Page
(
  "/Admin/Roles/{RoleId:Guid}",
  Policy = PermissionIds.AdminRolesRead,
  AgentActions = [typeof(RoleState.UpdateRoleActionSet.Action)]
)]
[Authorize(Policy = PermissionIds.AdminRolesRead)]
partial class RoleDetailPage;
