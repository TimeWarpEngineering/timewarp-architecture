namespace TimeWarp.Architecture;

public interface IApiService
{
  /// <summary>
  /// Get the response for the given request
  /// </summary>
  /// <typeparam name="TResponse"></typeparam>
  /// <param name="request"></param>
  /// <returns></returns>
  Task<TResponse?> GetResponse<TResponse>(IApiRequest request) where TResponse : class;
}
