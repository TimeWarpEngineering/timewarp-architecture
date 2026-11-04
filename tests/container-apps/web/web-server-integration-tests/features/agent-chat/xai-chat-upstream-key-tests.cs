#region Purpose
// The server relay's API key resolution: stored key, replacement without restart, host key precedence,
// and one unprotect per turn.
#endregion

#region Design
// UseFakeUpstream is honored in Development and ignored in Production. The fake returns a
// page_context function call and, on the next turn, text that includes the tool result. When
// site_map or navigate is offered it also scripts "where" -> site_map, a site_map result ->
// a markdown link answer, and "take me there" -> navigate. Tool declarations are
// AIFunctionDeclaration, not AIFunction, so the server has no InvokeAsync.
#endregion

namespace XaiChatUpstream_;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.AgentChats.Application;
using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;
using static XaiChatUpstream_.XaiChatUpstreamFixture;

public class XaiChatUpstream_Key_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<XaiChatUpstream_Key_Given_>();

  public static async Task Stored_Key_Is_Sent_And_A_Replacement_Is_Used_Without_A_Restart()
  {
    const string first = "xai-test-key-398-aaaa";
    const string second = "xai-test-key-398-bbbb";
    MemoryProtectedSecretStore store = new();
    CapturingChatClientFactory factory = new();
    await using ServiceProvider provider = Build("Production", useFake: false, apiKey: null, store, factory);
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      first,
      Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    OneOf<Response, SharedProblemDetails> firstTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      second,
      Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    OneOf<Response, SharedProblemDetails> secondTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    firstTurn.IsT0.ShouldBeTrue();
    secondTurn.IsT0.ShouldBeTrue();
    factory.Keys.ShouldBe([first, second]);
  }

  public static async Task Host_Key_Wins_Over_The_Stored_Key()
  {
    const string host = "xai-host-key-398-hhhh";
    const string stored = "xai-test-key-398-aaaa";
    MemoryProtectedSecretStore store = new();
    CapturingChatClientFactory factory = new();
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      stored,
      Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    await using ServiceProvider provider = Build("Production", useFake: false, apiKey: host, store, factory);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    OneOf<Response, SharedProblemDetails> turn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    turn.IsT0.ShouldBeTrue();
    factory.Keys.ShouldBe([host]);
  }

  public static async Task One_Turn_Unprotects_The_Stored_Key_Once()
  {
    const string key = "xai-test-key-398-aaaa";
    MemoryProtectedSecretStore store = new();
    CapturingChatClientFactory factory = new();
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      key,
      Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    await using ServiceProvider provider = Build("Production", useFake: false, apiKey: null, store, factory);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    upstream.BeginKeyResolution();
    (await upstream.IsConfiguredAsync(CancellationToken.None)).ShouldBeTrue();
    OneOf<Response, SharedProblemDetails> turn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    turn.IsT0.ShouldBeTrue();
    store.UnprotectCalls.ShouldBe(1);
    factory.Keys.ShouldBe([key]);
  }
}
