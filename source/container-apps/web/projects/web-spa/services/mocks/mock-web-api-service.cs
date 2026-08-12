#region Purpose
// Decorator over the real IWebServerApiService that answers selected requests from contract-defined mock factories.
#endregion

#region Design
// Mock data lives with each contract (GetMockResponseFactory); the request-type -> factory
// registry is SOURCE-GENERATED from every contract exposing that method
// (MockResponseFactoryRegistryGenerator), so defining a factory IS registering it. To use the
// real server for a feature during UI development, add its request type to UseRealApi below.
// Enabled via the MOCK_WEB_API symbol in Program.
// Requests still run through their FluentValidation validators so mocks cannot mask inputs the
// real server would reject.
// Inner IApiService (constructor) is the fall-through when no factory matches:
//   host-present → real WebServerApiService (HTTP)
//   mock-first / no BFF → TimeWarp.Foundation.NullApiService (501 problem arm, no transport)
#endregion

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

  // Requests listed here fall through to the real API service even in mock mode.
  private static readonly HashSet<Type> UseRealApi =
  [
    // typeof(GetProfile.Query),
  ];

  private readonly Dictionary<Type, Delegate> Factories = CreateFactories();

  private static Dictionary<Type, Delegate> CreateFactories()
  {
    Dictionary<Type, Delegate> factories = GeneratedMockResponseFactories.Create();
    foreach (Type type in UseRealApi) factories.Remove(type);
    return factories;
  }

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
      Logger.LogDebug(message: "No mock response factory found for {RequestType}. Falling back to real API service.", requestType.FullName);
      return await ApiService.GetResponse<TResponse>(request, cancellationToken);
    }

    try
    {
      await Task.Delay(millisecondsDelay: 100, cancellationToken); // Simulate async work
      Logger.LogDebug(message: "Mock Api Call, Request type: {RequestType} Url:{Url}", requestType.FullName, request.GetRoute());

      // Invoke the MockResponseFactory
      if (factory is MockResponseFactory<TResponse> mockResponseFactory)
      {
        TResponse response = mockResponseFactory(request);
        return response;
      }

      throw new InvalidOperationException($"Factory for {requestType} is not a MockResponseFactory<{typeof(TResponse)}>");
    }
    catch (Exception ex)
    {
      // Log the exception for debugging purposes
      Logger.LogError(ex, message: "Error occurred while invoking mock factory for {RequestType}", requestType);
      throw;
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
    MethodInfo? validateMethod = validatorType.GetMethod(nameof(IValidator<>.Validate), [requestType]);

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
