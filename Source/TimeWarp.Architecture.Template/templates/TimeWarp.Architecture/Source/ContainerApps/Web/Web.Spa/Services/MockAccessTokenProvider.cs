namespace TimeWarp.Architecture.Services;
public class MockAccessTokenProvider : IAccessTokenProvider
{
  public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
  {
    return GenerateAccessToken();
  }

  public ValueTask<AccessTokenResult> RequestAccessToken()
  {
    return GenerateAccessToken();
  }

  private static ValueTask<AccessTokenResult> GenerateAccessToken()
  {
    var accessToken = new AccessToken
    {
      Value = "dummy-token",
      Expires = DateTimeOffset.Now.AddHours(1)
    };

    return new ValueTask<AccessTokenResult>(Task.FromResult(new AccessTokenResult(AccessTokenResultStatus.Success, accessToken, null, null)));
  }
}
