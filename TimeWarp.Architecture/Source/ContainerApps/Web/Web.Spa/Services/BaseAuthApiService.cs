namespace TimeWarp.Architecture.Services;

/// <summary>
/// This is the Base Service that is used to interact with the API.Server
/// Using the Bearer Token for Authentication
/// </summary>
internal abstract class BaseAuthApiService : BaseApiService
{
  private readonly IAccessTokenProvider AccessTokenProvider;
  protected BaseAuthApiService
  (
    IAccessTokenProvider accessTokenProvider,
    IHttpClientFactory httpClientFactory,
    string httpClientName,
    IOptions<JsonSerializerOptions> options
  ) : base(httpClientFactory, httpClientName, options)
  {
    AccessTokenProvider = accessTokenProvider;
  }

  // Add testing constructor
  protected BaseAuthApiService
  (
    IAccessTokenProvider accessTokenProvider,
    HttpClient httpClient,
    JsonSerializerOptions jsonSerializerOptions
  ) : base(httpClient, jsonSerializerOptions)
  {
    AccessTokenProvider = accessTokenProvider;
  }

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
