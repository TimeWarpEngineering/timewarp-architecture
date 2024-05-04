namespace TimeWarp.Architecture.Services;

public class MockWebApiService
(
  IApiService ApiService,
  ILogger<MockWebApiService> Logger
) : IWebServerApiService
{
  private readonly Dictionary<Type, IMockResponseFactory> Factories = new()
  {
    // Comment out those where you want to use the real API service
    { typeof(GetCurrentUser.Query), new GetCurrentUserMockResponseFactory() },
    { typeof(GetRoles.Query), new GetRolesMockResponseFactory() },
    { typeof(GetRole.Query), new GetRoleMockResponseFactory() },
    { typeof(UpdateRole.Command), new UpdateRoleMockResponseFactory()},
    { typeof(DeleteRole.Command), new DeleteRoleMockResponseFactory()}

    // Add other mappings here
  };

  public async Task<OneOf<TResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    await Task.Delay(100, cancellationToken); // Simulate async work

    Type requestType = request.GetType();

    // If no mock factory is found, fall back to the real API service
    if (!Factories.TryGetValue(requestType, out IMockResponseFactory? factory))
      return await ApiService.GetResponse<TResponse>(request, cancellationToken);

    Logger.LogDebug("**** Mock Api Call was made to {url} ****", request.GetRoute());
    object response = factory.CreateMockResponse(request);
    if (response is TResponse strongResponse) return strongResponse!;

    throw new InvalidOperationException
      ($"Mock response factory for {requestType.FullName} did not return a response of the expected type {typeof(TResponse).FullName}");
  }
}
