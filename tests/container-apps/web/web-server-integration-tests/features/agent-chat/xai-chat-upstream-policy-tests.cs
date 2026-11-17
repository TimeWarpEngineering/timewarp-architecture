#region Purpose
// The server relay's user-key policy (Allow / Required / Off) and the per-key chat client cache bound.
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
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;
using static XaiChatUpstream_.XaiChatUpstreamFixture;

public class XaiChatUpstream_Policy_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<XaiChatUpstream_Policy_Given_>();

  public static async Task Allow_Sends_The_Signed_In_Users_Key_And_A_Policy_Change_Is_The_Next_Turn()
  {
    const string userKey = "xai-user-key-399-aaaa";
    const string siteKey = "xai-site-key-399-ssss";
    Guid userA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    Guid userB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    MemoryProtectedSecretStore store = new();
    await store.SetAsync(
      UserXaiApiKeyNames.NameFor(userA),
      UserXaiApiKeyNames.PurposeFor(userA),
      userKey,
      userA,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      siteKey,
      userA,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create());
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(userA) };
    CapturingChatClientFactory factory = new();
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      factory,
      settings,
      principal);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    OneOf<Response, SharedProblemDetails> userTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    SiteSettings? current = await settings.GetAsync();
    current.ShouldNotBeNull();
    current.SetXaiByokMode(XaiByokMode.Off);
    await settings.UpdateAsync(current);
    OneOf<Response, SharedProblemDetails> offTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    current = await settings.GetAsync();
    current.ShouldNotBeNull();
    current.SetXaiByokMode(XaiByokMode.Required);
    await settings.UpdateAsync(current);
    OneOf<Response, SharedProblemDetails> requiredTurn =
      await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    current = await settings.GetAsync();
    current.ShouldNotBeNull();
    current.SetXaiByokMode(XaiByokMode.Allow);
    await settings.UpdateAsync(current);
    principal.Id = PrincipalId.From(userB);
    OneOf<Response, SharedProblemDetails> otherTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    userTurn.IsT0.ShouldBeTrue();
    offTurn.IsT0.ShouldBeTrue();
    requiredTurn.IsT0.ShouldBeTrue();
    otherTurn.IsT0.ShouldBeTrue();
    factory.Keys.ShouldBe([userKey, siteKey, userKey, siteKey]);
    upstream.MissingKeyDetail(development: false).ShouldNotContain(userKey);
  }

  public static async Task Required_Names_Profile_And_Omits_The_Admin_Hint()
  {
    const string siteKey = "xai-site-key-399-ssss";
    Guid userA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    MemoryProtectedSecretStore store = new();
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      siteKey,
      userA,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create(xaiByokMode: XaiByokMode.Required));
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(userA) };
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      new CapturingChatClientFactory(),
      settings,
      principal);
    TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration.Response response =
      await Configuration(provider, principal);

    response.Configured.ShouldBeFalse();
    response.AdminHint.ShouldBeNull();
    response.KeyHint.ShouldBe(XaiByokCopy.SetYourKey);
    response.KeyHint!.ShouldNotContain(siteKey);
  }

  public static async Task Off_Names_An_Admin_And_Does_Not_Use_The_User_Key()
  {
    const string userKey = "xai-user-key-399-aaaa";
    Guid userA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    MemoryProtectedSecretStore store = new();
    await store.SetAsync(
      UserXaiApiKeyNames.NameFor(userA),
      UserXaiApiKeyNames.PurposeFor(userA),
      userKey,
      userA,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create(xaiByokMode: XaiByokMode.Off));
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(userA) };
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      new CapturingChatClientFactory(),
      settings,
      principal);
    TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration.Response response =
      await Configuration(provider, principal);

    response.Configured.ShouldBeFalse();
    response.AdminHint.ShouldBe(XaiChatDefaults.AdminAiSentence);
    response.KeyHint.ShouldBe(XaiByokCopy.AskAdmin + " " + XaiChatDefaults.HostSetupText(development: false));
    response.KeyHint!.ShouldNotContain(userKey);
  }

  public static async Task Two_Keys_Alternating_Reuse_The_Cached_Clients()
  {
    Guid userA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    Guid userB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    MemoryProtectedSecretStore store = new();
    await StoreUserKeyAsync(store, userA, "xai-user-key-399-aaaa");
    await StoreUserKeyAsync(store, userB, "xai-user-key-399-bbbb");
    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create(xaiByokMode: XaiByokMode.Required));
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(userA) };
    TrackingChatClientFactory factory = new();
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      factory,
      settings,
      principal);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    for (int turn = 0; turn < 10; turn++)
    {
      principal.Id = PrincipalId.From(turn % 2 == 0 ? userA : userB);
      OneOf<Response, SharedProblemDetails> result =
        await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
      result.IsT0.ShouldBeTrue();
    }

    factory.Created.ShouldBe(2);
    factory.Live.ShouldBe(2);
  }

  public static async Task Distinct_Keys_Past_The_Cache_Bound_Do_Not_Keep_Every_Client()
  {
    const int keys = XaiChatUpstream.MaxCachedClients + 4;
    MemoryProtectedSecretStore store = new();
    Guid[] users = new Guid[keys];
    for (int index = 0; index < keys; index++)
    {
      users[index] = Guid.Parse($"00000000-0000-0000-0000-{index + 1:x12}");
      await StoreUserKeyAsync(store, users[index], $"xai-user-key-399-{index:x4}");
    }

    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create(xaiByokMode: XaiByokMode.Required));
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(users[0]) };
    TrackingChatClientFactory factory = new();
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      factory,
      settings,
      principal);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    foreach (Guid userId in users)
    {
      principal.Id = PrincipalId.From(userId);
      OneOf<Response, SharedProblemDetails> result =
        await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
      result.IsT0.ShouldBeTrue();
    }

    factory.Created.ShouldBe(keys);
    factory.Live.ShouldBe(XaiChatUpstream.MaxCachedClients);
  }
}
