#region Purpose
// SettingsWrite lookup of an Entra tenant id from a customer's primary domain.
#endregion

#region Design
// The server calls EntraTenantResolver. The client never parses an issuer. Route is one
// constant so the abuse limiter and this contract cannot drift. SettingsWrite matches
// UpdateSiteSettings: the lookup is an admin edit, not a public existence oracle.
// GET carries Domain on the query string. Validator refuses a blank or non-domain value
// before the handler. Response invariants reject an empty tenant id or a blank domain.
// GetMockResponseFactory returns a fixed pair so mock mode does not call Microsoft.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using System.Collections.Specialized;
using TimeWarp.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.SettingsWrite,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class ResolveEntraTenant
{
  /// <summary>Path the abuse limiter and <see cref="Query"/> share.</summary>
  public const string Route = "api/settings/entra-tenant";

  [ApiRoute(Route, HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IQueryStringRouteProvider, IQuery<OneOf<Response, SharedProblemDetails>>
  {
    public string Domain { get; set; } = null!;

    /// <inheritdoc />
    public string GetRouteWithQueryString()
    {
      NameValueCollection parameters = new() { { nameof(Domain), Domain } };
      return $"{GetRoute()}?{this.GetQueryString(parameters)}";
    }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(query => query.Domain)
        .NotEmpty()
        .WithMessage("Enter a domain name.");
      RuleFor(query => query.Domain)
        .Must(domain => EntraTenantIds.TryNormalizeDomain(domain, out string? normalized) && normalized is not null)
        .WithMessage("'{PropertyValue}' is not a domain name.");
    }
  }

  public sealed class Response
  {
    public Guid TenantId { get; }
    public string Domain { get; }

    public Response(Guid tenantId, string domain)
    {
      TenantId = Guard.Against.NullOrEmpty(tenantId);
      Domain = Guard.Against.NullOrWhiteSpace(domain);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response(
      Guid.Parse("11111111-1111-1111-1111-111111111111"),
      "contoso.example");
  }
}
