#region Purpose
// Contract for fetching the signed-in user's display profile (alias, prefs, avatar), with a mock for backend-less demos.
#endregion

#region Design
// The Query has no properties — identity comes from the auth context, which is why the route says Users/Current.
// The empty Validator satisfies the convention that every request ships one, keeping source-gen and test
// patterns uniform across contracts.
// Guard clauses in the Response constructor make an empty profile unrepresentable; Avatar is a
// non-empty string (data URI or placeholder) so the UI never receives a blank image source.
// Wire names (task 148 D2): Alias maps from domain DisplayName (chrome-facing name stays Alias);
// Language/Region/Theme/Notifications mirror the Profile aggregate; Avatar is not domain/EF.
// [EndpointAllowAnonymous] (task 110 / 148 D3): the handler is deliberately dual-mode — an
// anonymous caller gets the contract's mock response so the demo works with no sign-in, and an
// authenticated caller (ICurrentUserService.UserId present) gets store-backed create-if-missing
// profile data. Requiring auth at the endpoint would break the anonymous demo path. Page gate
// remains Policies.CanViewOwnProfile.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[ApiEndpoint]
[EndpointAllowAnonymous("Handler is deliberately dual-mode (anonymous demo response vs store-backed authenticated response); requiring auth would break the anonymous demo path. Page stays CanViewOwnProfile (task 148 D3).")]
public static partial class GetProfile
{
  [ApiRoute(RouteTemplate: "api/Users/Current/Profile", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response : BaseResponse
  {
    public string Alias { get; }
    public string Language { get; }
    public string Region { get; }
    public string Theme { get; }
    public bool Notifications { get; }
    public string Avatar { get; }

    public Response(
      string alias,
      string language,
      string region,
      string theme,
      bool notifications,
      string avatar)
    {
      Alias = Guard.Against.NullOrEmpty(alias);
      Language = Guard.Against.NullOrEmpty(language);
      Region = Guard.Against.NullOrEmpty(region);
      Theme = Guard.Against.NullOrEmpty(theme);
      Notifications = notifications;
      Avatar = Guard.Against.NullOrEmpty(avatar);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
      alias: "alias",
      language: "en-US",
      region: "US",
      theme: "system",
      notifications: false,
      avatar: "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI0OCIgaGVpZ2h0PSI0OCI+PHJlY3Qgd2lkdGg9IjQ4IiBoZWlnaHQ9IjQ4IiBmaWxsPSIjODg4Ii8+PC9zdmc+");
  }
}
