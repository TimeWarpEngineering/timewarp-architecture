namespace TimeWarp.Architecture.Services;

using static DeleteRole;

internal class DeleteRoleMockResponseFactory : IMockResponseFactory
{
  public object CreateMockResponse(dynamic request)
  {
      return new Response();
  }
}
