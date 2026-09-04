#region Purpose
// Endpoint-centric contract for updating the signed-in user's progressive profile after the principal exists.
#endregion

#region Design
// PUT of the same IProfileDetails shape GetProfile.Response implements so the Profile page form
// binds once. Identity comes from the auth context (api/Users/Current/Profile), never a
// client-supplied id. [EndpointAuthorize] profile.write — this is never a gate on passkey/agent-key
// register, session, or token (locked 104 decision 1). AuthenticationSchemes: identity-session +
// mock-identity-session (human chrome; agents do not edit this product profile).
// Email is optional. Alias remains required (chrome always has a display name; create-if-missing
// defaults to Member). Response echoes IProfileDetails so the SPA copies server-accepted fields.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.ProfileWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class UpdateProfile
{
  [ApiRoute("api/Users/Current/Profile", HttpVerb.Put)]
  public sealed partial class Command : IApiRequest, IProfileDetails, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string Alias { get; set; } = null!;
    public string? Email { get; set; }
    public string Language { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string Theme { get; set; } = null!;
    public bool Notifications { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command).SetValidator(new ProfileDetailsValidator());
    }
  }

  public sealed class Response : IProfileDetails
  {
    public string Alias { get; set; }
    public string? Email { get; set; }
    public string Language { get; set; }
    public string Region { get; set; }
    public string Theme { get; set; }
    public bool Notifications { get; set; }

    public Response(
      string alias,
      string? email,
      string language,
      string region,
      string theme,
      bool notifications)
    {
      Alias = Guard.Against.NullOrEmpty(alias);
      Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
      Language = Guard.Against.NullOrEmpty(language);
      Region = Guard.Against.NullOrEmpty(region);
      Theme = Guard.Against.NullOrEmpty(theme);
      Notifications = notifications;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
      alias: "alias",
      email: null,
      language: "en-US",
      region: "US",
      theme: "system",
      notifications: false);
  }
}
