namespace TimeWarp.Architecture.Services;

using static Features.Admin.Roles.UpdateRole;

internal class UpdateRoleMockResponseFactory : IMockResponseFactory
{
  public object CreateMockResponse(dynamic request)
  {
    Command command = request;
    var response = new Response();
    return response;
  }
}
