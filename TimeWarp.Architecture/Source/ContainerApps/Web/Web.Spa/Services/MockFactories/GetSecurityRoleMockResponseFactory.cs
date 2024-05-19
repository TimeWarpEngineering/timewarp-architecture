namespace TimeWarp.Architecture.Services;

internal sealed class GetRoleMockResponseFactory : IMockResponseFactory
{
  public object CreateMockResponse(dynamic request)
  {
    GetRole.Query query = request;

    return new GetRole.Response
    (
      roleId: RoleIds.Administrator,
      name: nameof(RoleIds.Administrator),
      description: "The Administrator role is for administrators. And has access to all modules."
    );
  }
}
