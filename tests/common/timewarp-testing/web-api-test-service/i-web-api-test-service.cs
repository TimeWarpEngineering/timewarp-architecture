namespace TimeWarp.Architecture.Testing;

public interface IWebApiTestService
{
  /// <summary>
  /// Confirm that the endpoint for the request will return a BadRequest Status and
  /// explicitly contain the <paramref name="attributeName"/> in the error message
  /// </summary>
  Task ConfirmEndpointValidationError<TResponse>
  (
    IApiRequest apiRequest,
    string attributeName
  );

  /// <summary>
  /// Return the Response object by getting it as json and deserializing it/>
  /// </summary>
  public Task<OneOf.OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>(IApiRequest apiRequest, CancellationToken cancellationToken) where TResponse : class;
}
