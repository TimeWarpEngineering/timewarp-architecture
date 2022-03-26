namespace TimeWarp.Architecture.Testing
{
  using System.Threading.Tasks;
  using TimeWarp.Architecture.Features;

  public interface IWebApiTestService
  {
    /// <summary>
    /// Confirm that the endpoint for the request will return a BadRequest Status and
    /// explicit contain the <paramref name="aAttributeName"/> in the error message
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="aApiRequest"></param>
    /// <param name="aAttributeName"></param>
    /// <returns></returns>
    public Task ConfirmEndpointValidationError<TResponse>
    (
      IApiRequest aApiRequest,
      string aAttributeName
    );

    /// <summary>
    /// Return the Response object by getting it as json and deseralizing it/>
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="aRequest"></param>
    /// <returns></returns>
    public Task<TResponse> GetResponse<TResponse>(IApiRequest aApiRequest);
  }
}
