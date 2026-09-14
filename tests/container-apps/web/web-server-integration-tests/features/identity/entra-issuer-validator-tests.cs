#region Purpose
// Unit coverage for EntraIssuerValidator accept/reject paths (JsonWebToken and JwtSecurityToken).
#endregion

#region Design
// Host-free: builds compact JWTs and invokes EntraIssuerValidator.Validate directly. Pins the
// same iss == EntraIssuerMaterial.FromTenantId(tid) ordinal check wired on the named OIDC scheme.
#endregion

namespace EntraIssuerValidator_;

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Identity;

public class Validate_Given_
{
  private static readonly Guid TenantId = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
  private static readonly TokenValidationParameters TokenValidationParameters = new();

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Validate_Given_>();

  public static Task Matching_JsonWebToken_Issuer_Should_Return_Issuer()
  {
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TenantId));
    JsonWebToken securityToken = CreateUnsignedJsonWebToken(issuer, TenantId);

    string accepted = EntraIssuerValidator.Validate(issuer, securityToken, TokenValidationParameters);

    accepted.ShouldBe(issuer);
    return Task.CompletedTask;
  }

  public static Task Matching_JwtSecurityToken_Issuer_Should_Return_Issuer()
  {
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TenantId));
    JwtSecurityToken securityToken = CreateUnsignedJwtSecurityToken(issuer, TenantId);

    string accepted = EntraIssuerValidator.Validate(issuer, securityToken, TokenValidationParameters);

    accepted.ShouldBe(issuer);
    return Task.CompletedTask;
  }

  public static Task Wrong_Issuer_Should_Throw()
  {
    string expectedIssuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TenantId));
    const string wrongIssuer = "https://login.microsoftonline.com/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v2.0";
    JsonWebToken securityToken = CreateUnsignedJsonWebToken(expectedIssuer, TenantId);

    Should.Throw<SecurityTokenInvalidIssuerException>
    (
      () => EntraIssuerValidator.Validate(wrongIssuer, securityToken, TokenValidationParameters)
    );
    return Task.CompletedTask;
  }

  public static Task Missing_Issuer_Should_Throw()
  {
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TenantId));
    JsonWebToken securityToken = CreateUnsignedJsonWebToken(issuer, TenantId);

    Should.Throw<SecurityTokenInvalidIssuerException>
    (
      () => EntraIssuerValidator.Validate(" ", securityToken, TokenValidationParameters)
    );
    return Task.CompletedTask;
  }

  public static Task Missing_Tid_Should_Throw()
  {
    string issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TenantId));
    string header = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");
    string payload = Base64UrlEncoder.Encode($"{{\"iss\":\"{issuer}\"}}");
    JsonWebToken securityToken = new($"{header}.{payload}.");

    Should.Throw<SecurityTokenInvalidIssuerException>
    (
      () => EntraIssuerValidator.Validate(issuer, securityToken, TokenValidationParameters)
    );
    return Task.CompletedTask;
  }

  private static JsonWebToken CreateUnsignedJsonWebToken(string issuer, Guid tenantId)
  {
    string header = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");
    string payload = Base64UrlEncoder.Encode($"{{\"iss\":\"{issuer}\",\"tid\":\"{tenantId:D}\"}}");
    return new JsonWebToken($"{header}.{payload}.");
  }

  private static JwtSecurityToken CreateUnsignedJwtSecurityToken(string issuer, Guid tenantId)
  {
    string compact = CreateUnsignedJsonWebToken(issuer, tenantId).EncodedToken;
    return new JwtSecurityToken(compact);
  }
}
