namespace TimeWarp.Architecture.Services;

internal sealed class GetRolesMockResponseFactory : IMockResponseFactory
{

  public object CreateMockResponse(dynamic request)
  {
    GetRoles.Query query = request;

    GetRoles.RoleDto[] items =
    [
      new
      (
        roleId: RoleIds.Administrator,
        name: nameof(RoleIds.Administrator),
        description: "The Administrator role is for administrators. And has access to all modules."
      ),
      new
      (
        roleId: RoleIds.Accountant,
        name: nameof(RoleIds.Accountant),
        description: "The Accountant role is for accountants. And has access to the accounting module."
      ),
    ];

    return new GetRoles.Response(totalCount: items.Length, items);
  }
}
