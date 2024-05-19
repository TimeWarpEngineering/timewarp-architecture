namespace TimeWarp.Architecture.Services;

internal abstract class BaseAuthApiService
(
  IAccessTokenProvider AccessTokenProvider,
  IHttpClientFactory httpClientFactory,
  string httpClientName,
  IOptions<JsonSerializerOptions> options
) : BaseApiService(httpClientFactory, httpClientName, options)
{
  public override async Task<OneOf<TResponse, SharedProblemDetails>> GetResponse<TResponse>(IApiRequest request, CancellationToken cancellationToken)
  {
    await SetBearerTokenAsync();
    return await base.GetResponse<TResponse>(request, cancellationToken);
  }

  private async Task SetBearerTokenAsync()
  {
    AccessTokenResult tokenResult = await AccessTokenProvider.RequestAccessToken();
    if (tokenResult.TryGetToken(out AccessToken? token))
    {
      HttpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token.Value);
    }
  }
}
