namespace TimeWarp.Architecture.Services;

public class MockWebApiService : IWebServerApiService
{
  private readonly IApiService ApiService;
  private readonly ILogger<MockWebApiService> Logger;
  private readonly IServiceProvider ServiceProvider;
  public MockWebApiService
  (
  	IApiService apiService,
    ILogger<MockWebApiService> logger,
    IServiceProvider serviceProvider
  )
  {
    ApiService = apiService;
    Logger = logger;
    ServiceProvider = serviceProvider;
  }

  private readonly Dictionary<Type, Delegate> Factories = new()
  {
    // Comment out those where you want to use the real API service
    { typeof(GetCurrentUser.Query), GetCurrentUser.GetMockResponseFactory },
    { typeof(GetRoles.Query),GetRoles.GetMockResponseFactory },
    { typeof(GetRole.Query), GetRole.GetMockResponseFactory },
    { typeof(UpdateRole.Command), UpdateRole.GetMockResponseFactory},
    { typeof(DeleteRole.Command), DeleteRole.GetMockResponseFactory},
    { typeof(GetProfile.Query), GetProfile.GetMockResponseFactory }

    // Add other mappings here
  };

  public async Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {

    Type requestType = request.GetType();

    ValidateRequest(request, ServiceProvider);
    // If no mock factory is found, fall back to the real API service
    if (!Factories.TryGetValue(requestType, out Delegate? factory))
    {
      Logger.LogDebug(message: "No mock response factory found for {requestType}. Falling back to real API service.", requestType.FullName);
      return await ApiService.GetResponse<TResponse>(request, cancellationToken);
    }

    try
    {
      await Task.Delay(millisecondsDelay: 100, cancellationToken); // Simulate async work
      Logger.LogDebug(message: "Mock Api Call, Request type: {requestType} Url:{url}",requestType.FullName,request.GetRoute() );

      switch (factory)
      {
        case Func<IApiRequest, TResponse> typedFactory:
          {
            TResponse response = typedFactory(request);
            return response;
          }
        case Func<IApiRequest, FileResponse> fileFactory:
          {
            FileResponse response = fileFactory(request);
            return response;
          }
        default:
          throw new NotImplementedException();
      }
    }
    catch (OperationCanceledException)
    {
      return new SharedProblemDetails
      {
        Title = "Operation Cancelled",
        Status = 499, // 499 is the code for "Client Closed Request"
        Detail = "The request was cancelled.",
      };
    }
  }

  private static void ValidateRequest(object request, IServiceProvider serviceProvider)
  {
    Type requestType = request.GetType();

    // Get the generic type definition of IValidator<TRequest>
    Type validatorType = typeof(IValidator<>).MakeGenericType(requestType);

    // Get the validator from the ServiceProvider
    object? validator = serviceProvider.GetService(validatorType);

    if (validator == null) return;
    // Create a method info for the Validate method
    MethodInfo? validateMethod = validatorType.GetMethod(nameof(IValidator<object>.Validate), [requestType]);

    if (validateMethod == null) return;
    // Invoke the Validate method
    object? validationResult = validateMethod.Invoke(validator, [request]);

    // Check the validation result (assuming it's of type ValidationResult)
    if (validationResult is ValidationResult { IsValid: false } result)
    {
      // Handle validation failures
      throw new ValidationException(result.Errors);
    }
  }
}
