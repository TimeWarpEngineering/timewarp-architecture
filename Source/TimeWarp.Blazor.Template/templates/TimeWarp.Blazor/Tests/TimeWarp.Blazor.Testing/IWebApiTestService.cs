namespace TimeWarp.Blazor.Testing
{
  using MediatR;
  using System.Threading.Tasks;

  public interface IWebApiTestService
  {
    /// <summary>
    /// Confirm that the endpoint for the request will return a BadRequest Status and
    /// explicit contain the <paramref name="aAttributeName"/> in the error message
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="aRoute"></param>
    /// <param name="aRequest"></param>
    /// <param name="aAttributeName"></param>
    /// <returns></returns>
    public Task ConfirmEndpointValidationError<TResponse>
    (
      IRequest<TResponse> aRequest,
      string aAttributeName
    );

    /// <summary>
    /// Return the Response object by getting it as json and deseralizing it/>
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="aUri"></param>
    /// <returns></returns>
    public Task<TResponse> GetJsonAsync<TResponse>(string aUri);
  }
}
