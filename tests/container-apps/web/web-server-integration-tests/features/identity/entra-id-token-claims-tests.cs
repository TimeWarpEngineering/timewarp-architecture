#region Purpose
// Host-free coverage for EntraIdTokenClaims.TryRead first-failing-check reasons.
#endregion

#region Design
// ClaimsPrincipal only — no host. Distinguishes missing vs unparsable tid/oid and missing iss so
// the HTTP adapter can name the check without logging values.
#endregion

namespace EntraIdTokenClaims_;

using System.Security.Claims;
using TimeWarp.Architecture.Features.Identity.Application;

public class TryRead_Given_
{
  private static readonly Guid TenantId = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
  private static readonly Guid ObjectId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
  private const string Issuer = "https://login.microsoftonline.com/30f3971f-4719-4f20-9b6f-88916e0b95bd/v2.0";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryRead_Given_>();

  public static Task Complete_Short_Names_Should_Succeed()
  {
    ClaimsPrincipal principal = PrincipalWith(
      new Claim("tid", TenantId.ToString("D")),
      new Claim("oid", ObjectId.ToString("D")),
      new Claim("iss", Issuer),
      new Claim("name", "Test User"));

    bool read = EntraIdTokenClaims.TryRead(
      principal,
      out EntraIdTokenClaims claims,
      out EntraIdTokenClaimReadFailure failure);

    read.ShouldBeTrue();
    failure.ShouldBe(EntraIdTokenClaimReadFailure.None);
    claims.TenantId.ShouldBe(TenantId);
    claims.ObjectId.ShouldBe(ObjectId);
    claims.Issuer.ShouldBe(Issuer);
    claims.DisplayName.ShouldBe("Test User");
    claims.PreferredUsername.ShouldBeNull();
    claims.AccountHint.ShouldBeNull("name identifies the person, not the account");
    return Task.CompletedTask;
  }

  public static Task Preferred_Username_Should_Be_The_Account_Hint()
  {
    ClaimsPrincipal principal = PrincipalWith(
      new Claim("tid", TenantId.ToString("D")),
      new Claim("oid", ObjectId.ToString("D")),
      new Claim("iss", Issuer),
      new Claim("name", "Test User"),
      new Claim("preferred_username", "Steven.Cramer@TimeWarp.Enterprises"));

    bool read = EntraIdTokenClaims.TryRead(
      principal,
      out EntraIdTokenClaims claims,
      out EntraIdTokenClaimReadFailure failure);

    read.ShouldBeTrue();
    failure.ShouldBe(EntraIdTokenClaimReadFailure.None);
    claims.DisplayName.ShouldBe("Test User");
    claims.PreferredUsername.ShouldBe("Steven.Cramer@TimeWarp.Enterprises");
    claims.AccountHint.ShouldBe("Steven.Cramer@TimeWarp.Enterprises");
    return Task.CompletedTask;
  }

  public static Task Name_Only_Should_Leave_Account_Hint_Null()
  {
    ClaimsPrincipal principal = PrincipalWith(
      new Claim("tid", TenantId.ToString("D")),
      new Claim("oid", ObjectId.ToString("D")),
      new Claim("iss", Issuer),
      new Claim("name", "Test User"));

    EntraIdTokenClaims.TryRead(principal, out EntraIdTokenClaims claims, out _).ShouldBeTrue();
    claims.PreferredUsername.ShouldBeNull();
    claims.AccountHint.ShouldBeNull("name identifies the person, not the account");
    return Task.CompletedTask;
  }

  public static Task Neither_Name_Nor_Preferred_Username_Should_Leave_Account_Hint_Null()
  {
    ClaimsPrincipal principal = PrincipalWith(
      new Claim("tid", TenantId.ToString("D")),
      new Claim("oid", ObjectId.ToString("D")),
      new Claim("iss", Issuer));

    EntraIdTokenClaims.TryRead(principal, out EntraIdTokenClaims claims, out _).ShouldBeTrue();
    claims.DisplayName.ShouldBeNull();
    claims.PreferredUsername.ShouldBeNull();
    claims.AccountHint.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Schema_Uri_Fallbacks_Should_Succeed()
  {
    ClaimsPrincipal principal = PrincipalWith(
      new Claim("http://schemas.microsoft.com/identity/claims/tenantid", TenantId.ToString("D")),
      new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", ObjectId.ToString("D")),
      new Claim("iss", Issuer));

    bool read = EntraIdTokenClaims.TryRead(
      principal,
      out EntraIdTokenClaims claims,
      out EntraIdTokenClaimReadFailure failure);

    read.ShouldBeTrue();
    failure.ShouldBe(EntraIdTokenClaimReadFailure.None);
    claims.TenantId.ShouldBe(TenantId);
    claims.ObjectId.ShouldBe(ObjectId);
    return Task.CompletedTask;
  }

  public static Task Missing_Tid_Should_Report_MissingTenantId()
  {
    AssertFailure(
      EntraIdTokenClaimReadFailure.MissingTenantId,
      new Claim("oid", ObjectId.ToString("D")),
      new Claim("iss", Issuer));
    return Task.CompletedTask;
  }

  public static Task Unparsable_Tid_Should_Report_UnparsableTenantId()
  {
    AssertFailure(
      EntraIdTokenClaimReadFailure.UnparsableTenantId,
      new Claim("tid", "not-a-guid"),
      new Claim("oid", ObjectId.ToString("D")),
      new Claim("iss", Issuer));
    return Task.CompletedTask;
  }

  public static Task Missing_Oid_Should_Report_MissingObjectId()
  {
    AssertFailure(
      EntraIdTokenClaimReadFailure.MissingObjectId,
      new Claim("tid", TenantId.ToString("D")),
      new Claim("iss", Issuer));
    return Task.CompletedTask;
  }

  public static Task Unparsable_Oid_Should_Report_UnparsableObjectId()
  {
    AssertFailure(
      EntraIdTokenClaimReadFailure.UnparsableObjectId,
      new Claim("tid", TenantId.ToString("D")),
      new Claim("oid", "not-a-guid"),
      new Claim("iss", Issuer));
    return Task.CompletedTask;
  }

  public static Task Missing_Iss_Should_Report_MissingIssuer()
  {
    AssertFailure(
      EntraIdTokenClaimReadFailure.MissingIssuer,
      new Claim("tid", TenantId.ToString("D")),
      new Claim("oid", ObjectId.ToString("D")));
    return Task.CompletedTask;
  }

  public static Task Problem_Detail_Should_Name_The_Failing_Check_Without_Values()
  {
    SharedProblemDetails missingOid = IdentityProblems.InvalidEntraToken(
      EntraIdTokenClaimReadFailure.MissingObjectId);
    missingOid.Title.ShouldBe("Invalid Entra token");
    missingOid.Status.ShouldBe(400);
    missingOid.Detail.ShouldBe("The Entra ID token is missing the oid claim.");
    (missingOid.Detail ?? "").ShouldNotContain(ObjectId.ToString("D"));
    (missingOid.Detail ?? "").ShouldNotContain(TenantId.ToString("D"));
    return Task.CompletedTask;
  }

  private static void AssertFailure(EntraIdTokenClaimReadFailure expected, params Claim[] claims)
  {
    bool read = EntraIdTokenClaims.TryRead(
      PrincipalWith(claims),
      out EntraIdTokenClaims parsed,
      out EntraIdTokenClaimReadFailure failure);
    read.ShouldBeFalse();
    failure.ShouldBe(expected);
    parsed.ShouldBe(default);
  }

  private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
    new(new ClaimsIdentity(claims, authenticationType: "test"));
}
