#region Purpose
// Round-trip tests for the identity feature contract GetCredentials_Response (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

#region Design
// Never_Serializes_Handle_Or_PublicMaterial (task 104-005) is the contract-level half of a
// two-layer pin — a reflection-based structural assertion (CredentialSummary has no
// Handle/PublicMaterial property, so a Label containing "handle" cannot false-fail it) plus a
// wire-level json.ShouldNotContain as belt-and-suspenders, proving the promise survives actual
// JSON serialization, not just the C# type shape. The integration-level twin
// (Credential_List_Tests.cs's Never_Serializes_Handle_Or_PublicMaterial) proves the same
// two-layer promise survives the whole generated-endpoint pipeline too.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using System.Text.Json.Nodes;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class GetCredentials_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentials_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    var firstId = CredentialId.New();
    GetCredentials.Response response = new
    (
      [
        new GetCredentials.CredentialSummary
        (
          firstId, CredentialType.Passkey, "1Password", "Work laptop",
          DateTimeOffset.UtcNow.AddDays(-10), revokedAt: null, isActive: true,
          new RegisteredWith(AuthenticatorAttachment.Platform, "Chrome", "Windows"), "3f9a1c2e",
          lastUsedAt: new DateTimeOffset(2026, 9, 23, 8, 30, 0, TimeSpan.Zero),
          canRevoke: true,
          canRename: true
        ),
        new GetCredentials.CredentialSummary
        (
          CredentialId.New(), CredentialType.AgentKey, label: null, nickname: null,
          DateTimeOffset.UtcNow.AddDays(-5), revokedAt: DateTimeOffset.UtcNow, isActive: false,
          RegisteredWith.Unknown, "b71e04dd"
        ),
        new GetCredentials.CredentialSummary
        (
          CredentialId.New(), CredentialType.EntraAccount, "Microsoft 365", nickname: null,
          DateTimeOffset.UtcNow.AddDays(-2), revokedAt: null, isActive: true,
          RegisteredWith.Unknown, "5c7d9e01",
          accountHint: "steve@contoso.com",
          canRevoke: false,
          canRename: true
        )
      ],
      canLinkMicrosoft365: true
    );

    GetCredentials.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.Credentials.Count.ShouldBe(3);
    parsed.Credentials[0].Id.ShouldBe(response.Credentials[0].Id);
    parsed.Credentials[0].Type.ShouldBe(CredentialType.Passkey);
    parsed.Credentials[0].Label.ShouldBe("1Password");
    parsed.Credentials[0].Nickname.ShouldBe("Work laptop");
    parsed.Credentials[0].IsActive.ShouldBeTrue();
    parsed.Credentials[0].RevokedAt.ShouldBeNull();
    parsed.Credentials[0].RegisteredWith.ShouldBe(new RegisteredWith(AuthenticatorAttachment.Platform, "Chrome", "Windows"));
    parsed.Credentials[0].Fingerprint.ShouldBe("3f9a1c2e");
    parsed.Credentials[0].LastUsedAt.ShouldBe(new DateTimeOffset(2026, 9, 23, 8, 30, 0, TimeSpan.Zero));
    parsed.Credentials[1].Label.ShouldBeNull();
    parsed.Credentials[1].Nickname.ShouldBeNull();
    parsed.Credentials[1].IsActive.ShouldBeFalse();
    parsed.Credentials[1].RevokedAt.ShouldNotBeNull();
    parsed.Credentials[1].RegisteredWith.IsUnknown.ShouldBeTrue();
    parsed.Credentials[1].Fingerprint.ShouldBe("b71e04dd");
    parsed.Credentials[1].LastUsedAt.ShouldBeNull("never-used rides the wire as an explicit null (task 248-002)");
    parsed.Credentials[0].AccountHint.ShouldBeNull("passkeys carry no external account (task 250)");
    parsed.Credentials[2].Type.ShouldBe(CredentialType.EntraAccount);
    parsed.Credentials[2].Label.ShouldBe("Microsoft 365");
    parsed.Credentials[2].AccountHint.ShouldBe("steve@contoso.com");
    // Task 282: the server's availability flags ride the same read — per row and page-level.
    parsed.Credentials[0].CanRevoke.ShouldBeTrue();
    parsed.Credentials[0].CanRename.ShouldBeTrue();
    parsed.Credentials[1].CanRevoke.ShouldBeFalse("a revoked row gets no flags");
    parsed.Credentials[1].CanRename.ShouldBeFalse();
    parsed.Credentials[2].CanRevoke.ShouldBeFalse();
    parsed.Credentials[2].CanRename.ShouldBeTrue();
    parsed.CanLinkMicrosoft365.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Mock_Response_With_Flags()
  {
    GetCredentials.Response response = GetCredentials.GetMockResponseFactory()(new GetCredentials.Query());

    GetCredentials.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.Credentials.Count.ShouldBe(response.Credentials.Count);
    for (int index = 0; index < response.Credentials.Count; index++)
    {
      parsed.Credentials[index].CanRevoke.ShouldBe(response.Credentials[index].CanRevoke, $"row {index} CanRevoke");
      parsed.Credentials[index].CanRename.ShouldBe(response.Credentials[index].CanRename, $"row {index} CanRename");
    }

    parsed.CanLinkMicrosoft365.ShouldBe(response.CanLinkMicrosoft365);

    string json = JsonSerializer.Serialize(response, ContractSerialization.Options);
    json.ShouldContain("\"canRevoke\":true");
    json.ShouldContain("\"canRename\":true");
    json.ShouldContain("\"canLinkMicrosoft365\":false");
    return Task.CompletedTask;
  }

  public static Task Default_Missing_Flags_To_False()
  {
    // A payload without the flags (an older server) shows no action: every flag defaults to false.
    GetCredentials.Response full = GetCredentials.GetMockResponseFactory()(new GetCredentials.Query());
    JsonObject root = JsonSerializer.SerializeToNode(full, ContractSerialization.Options)!.AsObject();
    root.Remove("canLinkMicrosoft365");
    foreach (JsonNode? row in root["credentials"]!.AsArray())
    {
      row!.AsObject().Remove("canRevoke");
      row.AsObject().Remove("canRename");
    }

    string json = root.ToJsonString();
    json.ShouldNotContain("canRevoke");

    GetCredentials.Response parsed =
      JsonSerializer.Deserialize<GetCredentials.Response>(json, ContractSerialization.Options).ShouldNotBeNull();

    parsed.Credentials.Count.ShouldBe(full.Credentials.Count);
    parsed.Credentials.ShouldAllBe(static credential => !credential.CanRevoke && !credential.CanRename);
    parsed.CanLinkMicrosoft365.ShouldBeFalse();
    return Task.CompletedTask;
  }

  // Load-bearing security pin (task 104-005): CredentialSummary structurally omits
  // Handle/PublicMaterial — this proves that holds through actual JSON serialization, not just the
  // C# type shape. See this file's Design region for the second (integration-level) half of this pin.
  public static Task Never_Serializes_Handle_Or_PublicMaterial()
  {
    // Structural check FIRST (round-1 review M4) — the real guarantee, and the only one that cannot
    // false-fail on Label content: CredentialSummary itself has no Handle/PublicMaterial member.
    string[] propertyNames = typeof(GetCredentials.CredentialSummary).GetProperties().Select(p => p.Name).ToArray();
    propertyNames.ShouldNotContain(nameof(Credential.Handle));
    propertyNames.ShouldNotContain(nameof(Credential.PublicMaterial));

    GetCredentials.Response response = new
    (
      [
        new GetCredentials.CredentialSummary
        (
          CredentialId.New(), CredentialType.Passkey, "laptop", nickname: null, DateTimeOffset.UtcNow,
          revokedAt: null, isActive: true, RegisteredWith.Unknown, "0123abcd"
        )
      ]
    );

    string json = JsonSerializer.Serialize(response, ContractSerialization.Options);

    // Wire-level check SECOND, belt-and-suspenders — matches Credential_List_Tests.cs's
    // integration-level twin. Task 248-001: the fingerprint IS on the wire (one-way, 8 hex) and
    // is the only handle-derived value allowed there.
    json.ToLowerInvariant().ShouldNotContain("handle");
    json.ToLowerInvariant().ShouldNotContain("publicmaterial");
    json.ShouldContain("\"fingerprint\":\"0123abcd\"");
    return Task.CompletedTask;
  }
}
