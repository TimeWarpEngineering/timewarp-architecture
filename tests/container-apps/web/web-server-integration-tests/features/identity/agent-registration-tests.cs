#region Purpose
// End-to-end tests for the agent public-key registration ceremony (StartAgentKeyRegistration +
// CompleteAgentKeyRegistration): real host, a deterministic-shape (per-instance-random) software
// agent key standing in for a real agent SDK.
#endregion

#region Design
// Unlike the passkey suites (104-003), these tests use WebTestServerApplication.GetResponse
// throughout — no cookie/session is issued by this ceremony (agents get bearer tokens, not browser
// sessions; see CompleteAgentKeyRegistration.Handler's Design region), so there is nothing here that
// requires the real-HTTP-vs-ScopedSender distinction those suites needed.
// WebTestServerApplication (and its in-memory IPrincipalStore singleton) is shared across every test
// method in this class (Fixie per-class fixture sharing — see IntegrationSoftwareAgentKey's Design
// region) — each test that registers a key uses ITS OWN IntegrationSoftwareAgentKey instance, so
// KeyIds never collide across test methods.
#endregion

namespace AgentRegistration_;

using System.Buffers.Text;
using System.Net;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

public class Returns_
{

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Returns_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Ok_With_PrincipalId_And_KeyId_Given_Valid_Registration()
  {
    var key = new IntegrationSoftwareAgentKey();
    CompleteAgentKeyRegistration.Command completeCommand = await BuildValidCompleteCommand(key);

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(completeCommand, CancellationToken.None);

    result.IsT0.ShouldBeTrue("Registration should succeed.");
    result.AsT0.PrincipalId.IsEmpty.ShouldBeFalse();
    result.AsT0.KeyId.ShouldBe(Base64Url.EncodeToString(key.KeyId));
  }

  public static async Task BadRequest_Given_Reused_Challenge()
  {
    var key = new IntegrationSoftwareAgentKey();
    CompleteAgentKeyRegistration.Command completeCommand = await BuildValidCompleteCommand(key);

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> first =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(completeCommand, CancellationToken.None);
    first.IsT0.ShouldBeTrue("First completion should succeed.");

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> replay =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(completeCommand, CancellationToken.None);

    replay.IsT2.ShouldBeTrue("Replayed completion should fail.");
    replay.AsT2.Status.ShouldBe(400);
  }

  public static async Task BadRequest_Given_Tampered_Signature()
  {
    var key = new IntegrationSoftwareAgentKey();

    OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartAgentKeyRegistration.Response>(new StartAgentKeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = Base64Url.DecodeFromChars(start.AsT0.Challenge);
    byte[] signature = key.Sign(AgentKeyCeremonyType.Registration, challenge);
    signature[0] ^= 0xFF;

    var completeCommand = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = Base64Url.EncodeToString(key.SpkiPublicKey),
      Challenge = Base64Url.EncodeToString(challenge),
      Signature = Base64Url.EncodeToString(signature)
    };

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(completeCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Tampered-signature completion should fail.");
    result.AsT2.Status.ShouldBe(400);
  }

  public static async Task BadRequest_Given_Malformed_Public_Key()
  {
    OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartAgentKeyRegistration.Response>(new StartAgentKeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = Base64Url.DecodeFromChars(start.AsT0.Challenge);

    var completeCommand = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = Base64Url.EncodeToString([0xFF, 0xFF, 0xFF, 0x00, 0x01]),
      Challenge = Base64Url.EncodeToString(challenge),
      Signature = Base64Url.EncodeToString([1, 2, 3])
    };

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(completeCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Malformed-public-key completion should fail.");
    result.AsT2.Status.ShouldBe(400);
  }

  public static async Task Conflict_Given_Duplicate_Key()
  {
    var key = new IntegrationSoftwareAgentKey();

    CompleteAgentKeyRegistration.Command firstCommand = await BuildValidCompleteCommand(key);
    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> first =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(firstCommand, CancellationToken.None);
    first.IsT0.ShouldBeTrue("First registration should succeed.");

    // Same key, a brand-new ceremony/challenge.
    CompleteAgentKeyRegistration.Command secondCommand = await BuildValidCompleteCommand(key);
    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> second =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(secondCommand, CancellationToken.None);

    second.IsT2.ShouldBeTrue("Duplicate key registration should fail.");
    second.AsT2.Status.ShouldBe(409);
  }

  public static async Task ValidationError_Given_Oversized_PublicKey()
  {
    var command = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = new string('A', (2 * 1024) + 1),
      Challenge = "QQ",
      Signature = "QQ"
    };

    await Web.ConfirmEndpointValidationError<CompleteAgentKeyRegistration.Response>
      (command, nameof(CompleteAgentKeyRegistration.Command.PublicKey));
  }

  public static async Task ValidationError_Given_Oversized_Label()
  {
    var command = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = "QQ",
      Challenge = "QQ",
      Signature = "QQ",
      Label = new string('A', 65)
    };

    await Web.ConfirmEndpointValidationError<CompleteAgentKeyRegistration.Response>
      (command, nameof(CompleteAgentKeyRegistration.Command.Label));
  }

  private static async Task<CompleteAgentKeyRegistration.Command> BuildValidCompleteCommand(IntegrationSoftwareAgentKey key)
  {
    OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartAgentKeyRegistration.Response>(new StartAgentKeyRegistration.Command(), CancellationToken.None);

    byte[] challenge = Base64Url.DecodeFromChars(start.AsT0.Challenge);
    byte[] signature = key.Sign(AgentKeyCeremonyType.Registration, challenge);

    return new CompleteAgentKeyRegistration.Command
    {
      PublicKey = Base64Url.EncodeToString(key.SpkiPublicKey),
      Challenge = Base64Url.EncodeToString(challenge),
      Signature = Base64Url.EncodeToString(signature)
    };
  }

}
