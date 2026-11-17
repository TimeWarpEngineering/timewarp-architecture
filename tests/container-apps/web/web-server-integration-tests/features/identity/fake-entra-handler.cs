#region Purpose
// Test double for the OpenIdConnect handler: challenge completes the Entra ticket from header values.
#endregion

namespace EntraChallenge_;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Identity;

internal sealed class FakeEntraHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public FakeEntraHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : base(options, logger, encoder)
  {
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
    Task.FromResult(AuthenticateResult.NoResult());

  protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
  {
    Guid tenantId = Guid.TryParse(Request.Headers[FakeEntraHeaders.TenantId], out Guid parsedTenant) ? parsedTenant : Guid.Empty;
    Guid objectId = Guid.TryParse(Request.Headers[FakeEntraHeaders.ObjectId], out Guid parsedObject) ? parsedObject : Guid.Empty;
    string issuer = Request.Headers[FakeEntraHeaders.Issuer].ToString();
    if (string.IsNullOrWhiteSpace(issuer))
    {
      issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(tenantId));
    }

    ClaimsIdentity identity = new(EntraLinkDefaults.Scheme);
    identity.AddClaim(new Claim("tid", tenantId.ToString("D")));
    if (!string.Equals(Request.Headers[FakeEntraHeaders.OmitObjectId], "1", StringComparison.Ordinal))
    {
      identity.AddClaim(new Claim("oid", objectId.ToString("D")));
    }

    identity.AddClaim(new Claim("iss", issuer));
    string name = Request.Headers[FakeEntraHeaders.Name].ToString();
    identity.AddClaim(new Claim("name", string.IsNullOrWhiteSpace(name) ? "Test User" : name));
    string preferredUsername = Request.Headers[FakeEntraHeaders.PreferredUsername].ToString();
    if (!string.IsNullOrWhiteSpace(preferredUsername))
    {
      identity.AddClaim(new Claim("preferred_username", preferredUsername));
    }

    await EntraTicketHttp.HandleTicketAsync(
      Context,
      new ClaimsPrincipal(identity),
      properties,
      Context.RequestAborted);
  }
}
