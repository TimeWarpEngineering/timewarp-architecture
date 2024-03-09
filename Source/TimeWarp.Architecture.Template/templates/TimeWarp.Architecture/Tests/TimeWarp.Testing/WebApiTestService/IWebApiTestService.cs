namespace TimeWarp.Architecture.Testing;

public interface IWebApiTestService
{
  /// <summary>
  /// Confirm that the endpoint for the request will return a BadRequest Status and
  /// explicitly contain the <paramref name="attributeName"/> in the error message
  /// </summary>
  /// <typeparam name="TResponse"></typeparam>
  /// <param name="apiRequest"></param>
  /// <param name="attributeName"></param>
  /// <returns></returns>
  Task ConfirmEndpointValidationError<TResponse>
  (
    IApiRequest apiRequest,
    string attributeName
  );

  /// <summary>
  /// Return the Response object by getting it as json and deserializing it/>
  /// </summary>
  /// <typeparam name="TResponse"></typeparam>
  /// <param name="apiRequest"></param>
  /// <returns></returns>
  Task<TResponse> GetResponse<TResponse>(IApiRequest apiRequest) where TResponse : class;
}
