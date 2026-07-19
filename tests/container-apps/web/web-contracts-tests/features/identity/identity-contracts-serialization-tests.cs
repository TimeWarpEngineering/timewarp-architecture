#region Purpose
// Round-trip tests for the identity feature contracts (task 104-003) — the shapes where
// serialization can actually diverge: a typed-id (PrincipalId) Response with a ctor Guard, and an
// optional-property Command (UserHandle).
#endregion

#region Design
// StartPasskeyRegistration/StartPasskeyAuthentication's empty Command bodies and their Response's
// single-string-property shape are plain auto-property POCOs — no test here per the skill's
// "trivial auto-property POCOs are deliberately not written" guidance; CompletePasskeyRegistration/
// CompletePasskeyAuthentication/GetCurrentSession are the shapes worth pinning (typed-id ctor Guard,
// optional property, nullable typed-id).
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class CompletePasskeyRegistration_Command_Should
{
  public static void SerializeAndDeserialize()
  {
    CompletePasskeyRegistration.Command command = new()
    {
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uY3JlYXRlIn0",
      AttestationObject = "o2NmbXRkbm9uZQ"
    };

    CompletePasskeyRegistration.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.CredentialId.ShouldBe(command.CredentialId);
    parsed.ClientDataJson.ShouldBe(command.ClientDataJson);
    parsed.AttestationObject.ShouldBe(command.AttestationObject);
  }
}

public class CompletePasskeyRegistration_Response_Should
{
  public static void SerializeAndDeserialize_Via_Constructor()
  {
    CompletePasskeyRegistration.Response response = new(PrincipalId.New());

    CompletePasskeyRegistration.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.PrincipalId.ShouldBe(response.PrincipalId);
  }

  public static void Reject_EmptyPrincipalId_During_Deserialization()
  {
    // PrincipalId's own [TypedId] JsonConverter fail-closes on an empty guid before the Response
    // ctor's Guard even runs — either seam rejecting it is the contract that matters here.
    string json = """{"principalId":"00000000-0000-0000-0000-000000000000"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<CompletePasskeyRegistration.Response>(json, ContractSerialization.Options));
  }
}

public class CompletePasskeyAuthentication_Command_Should
{
  public static void SerializeAndDeserialize_Including_Optional_UserHandle()
  {
    CompletePasskeyAuthentication.Command command = new()
    {
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uZ2V0In0",
      AuthenticatorData = "AQIDBA",
      Signature = "AQIDBA",
      UserHandle = "dXNlckhhbmRsZQ"
    };

    CompletePasskeyAuthentication.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.CredentialId.ShouldBe(command.CredentialId);
    parsed.ClientDataJson.ShouldBe(command.ClientDataJson);
    parsed.AuthenticatorData.ShouldBe(command.AuthenticatorData);
    parsed.Signature.ShouldBe(command.Signature);
    parsed.UserHandle.ShouldBe(command.UserHandle);
  }

  public static void SerializeAndDeserialize_Without_UserHandle()
  {
    CompletePasskeyAuthentication.Command command = new()
    {
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uZ2V0In0",
      AuthenticatorData = "AQIDBA",
      Signature = "AQIDBA"
    };

    CompletePasskeyAuthentication.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.UserHandle.ShouldBeNull();
  }
}

public class GetCurrentSession_Response_Should
{
  public static void SerializeAndDeserialize_Authenticated()
  {
    GetCurrentSession.Response response = new(isAuthenticated: true, PrincipalId.New());

    GetCurrentSession.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.IsAuthenticated.ShouldBeTrue();
    parsed.PrincipalId.ShouldBe(response.PrincipalId);
  }

  public static void SerializeAndDeserialize_Unauthenticated()
  {
    GetCurrentSession.Response response = new(isAuthenticated: false, principalId: null);

    GetCurrentSession.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.IsAuthenticated.ShouldBeFalse();
    parsed.PrincipalId.ShouldBeNull();
  }
}
