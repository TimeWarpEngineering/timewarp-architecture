namespace TimeWarp.Architecture.Services;

/// <summary>
/// Provides predictable access tokens for local development, integration tests, and demos without live authentication infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// This implementation supports components that depend on <see cref="IAccessTokenProvider"/> when real token issuing services are unavailable.
/// It is useful for local testing, integration testing, and demonstration scenarios such as offline development.
/// </para>
/// <para>
/// Register this provider as the <see cref="IAccessTokenProvider"/> implementation in development-only configuration paths.
/// Both token request overloads delegate to <see cref="GenerateAccessToken"/>, which returns a consistent dummy token.
/// </para>
/// </remarks>
public partial class MockAccessTokenProvider : IAccessTokenProvider
{
  /// <summary>
  /// Requests a mock access token while accepting request options for API compatibility.
  /// </summary>
  /// <param name="options">The access token request options. The mock provider does not use these values.</param>
  /// <returns>A successful access token result containing a dummy token.</returns>
  public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
  {
    return GenerateAccessToken();
  }

  /// <summary>
  /// Requests a mock access token.
  /// </summary>
  /// <returns>A successful access token result containing a dummy token.</returns>
  public ValueTask<AccessTokenResult> RequestAccessToken()
  {
    return GenerateAccessToken();
  }

  /// <summary>
  /// Creates a successful dummy access token result used by both request overloads.
  /// </summary>
  /// <returns>A successful access token result with a short-lived dummy token.</returns>
  private static ValueTask<AccessTokenResult> GenerateAccessToken()
  {
    AccessToken accessToken = new()
    {
      Value = "dummy-token",
      Expires = DateTimeOffset.Now.AddHours(1)
    };

    return new ValueTask<AccessTokenResult>(Task.FromResult(new AccessTokenResult(AccessTokenResultStatus.Success, accessToken, null, null)));
  }
}
