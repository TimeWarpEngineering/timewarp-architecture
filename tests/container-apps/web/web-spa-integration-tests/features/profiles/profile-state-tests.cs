#region Purpose
// ProfileState keeps one GetProfile.Response. Fetch stores it. Update rebuilds it and keeps the avatar.
#endregion

#region Design
// In-proc SPA with a scripted IWebServerApiService, the same shape as the assistant-identity
// facts. No Aspire host: the store is what these facts observe. The working copy is
// UpdateProfile.Command.From on the stored response, and a DevTools snapshot of the populated
// state goes through StateJsonContext. Re-read the store after Send (clone-on-dispatch).
#endregion

namespace ProfileState_;

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Features.Analytics;
using TimeWarp.Architecture.Features.Profiles;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;

[TestTag("Integration")]
public class FetchAndUpdate_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FetchAndUpdate_Should_>();

  public static async Task Fetch_Should_StoreTheWholeResponse()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new ProfileState.FetchProfileDataActionSet.Action());

    GetProfile.Response stored = scope.Store.GetState<ProfileState>().Profile.ShouldNotBeNull();
    stored.DisplayName.ShouldBe(spa.Api.Fetched.DisplayName);
    stored.Email.ShouldBeNull();
    stored.Language.ShouldBe("en-US");
    stored.Region.ShouldBe("US");
    stored.Theme.ShouldBe("system");
    stored.Notifications.ShouldBeFalse();
    stored.Avatar.ShouldBe(ScriptedProfileApi.FetchedAvatar);

    UpdateProfile.Command copy = UpdateProfile.Command.From(stored);
    copy.DisplayName.ShouldBe("Ada");
    copy.Language.ShouldBe("en-US");
    copy.DisplayName = "draft";
    scope.Store.GetState<ProfileState>().Profile!.DisplayName.ShouldBe("Ada");

    string json = JsonSerializer.Serialize(
      scope.Store.GetState<ProfileState>(),
      StateJsonContext.Default.ProfileState);
    json.ShouldContain("Ada");
  }

  public static async Task Update_Should_KeepTheFetchedAvatar()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new ProfileState.FetchProfileDataActionSet.Action());

    await scope.Send(new ProfileState.UpdateProfileActionSet.Action(new UpdateProfile.Command
    {
      DisplayName = "typed-name",
      Email = "typed@example.com",
      Language = "en-US",
      Region = "US",
      Theme = "system",
      Notifications = false,
    }));

    GetProfile.Response stored = scope.Store.GetState<ProfileState>().Profile.ShouldNotBeNull();
    stored.DisplayName.ShouldBe("Ada Lovelace");
    stored.Email.ShouldBe("ada@example.com");
    stored.Language.ShouldBe("fr-FR");
    stored.Region.ShouldBe("FR");
    stored.Theme.ShouldBe("dark");
    stored.Notifications.ShouldBeTrue();
    stored.Avatar.ShouldBe(ScriptedProfileApi.FetchedAvatar);
  }

  public static async Task Update_WithNoSnapshot_Should_UseTheFallbackAvatar()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    scope.Store.GetState<ProfileState>().Profile.ShouldBeNull();

    await scope.Send(new ProfileState.UpdateProfileActionSet.Action(new UpdateProfile.Command
    {
      DisplayName = "typed-name",
      Language = "en-US",
      Region = "US",
      Theme = "system",
    }));

    GetProfile.Response stored = scope.Store.GetState<ProfileState>().Profile.ShouldNotBeNull();
    stored.DisplayName.ShouldBe("Ada Lovelace");
    stored.Avatar.ShouldBe(ProfileState.FallbackAvatar);
  }

  private sealed class ProfileSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ScriptedProfileApi Api { get; } = new();

    public ProfileSpa()
    {
      ServiceCollection services = new();
      services.AddLogging();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.RethrowHandlerExceptions = true;
          options.Assemblies =
          [
            typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddTimeWarpStateBlazor();
      services.AddSingleton<IWebServerApiService>(Api);
      services.AddScoped<NavigationManager, IdleNavigationManager>();
      ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  private sealed class ScriptedProfileApi : IWebServerApiService
  {
    public const string FetchedAvatar = "data:image/svg+xml;base64,fetched";

    public GetProfile.Response Fetched { get; } = new(
      displayName: "Ada",
      email: null,
      language: "en-US",
      region: "US",
      theme: "system",
      notifications: false,
      avatar: FetchedAvatar);

    public UpdateProfile.Response Updated { get; } = new(
      displayName: "Ada Lovelace",
      email: "ada@example.com",
      language: "fr-FR",
      region: "FR",
      theme: "dark",
      notifications: true);

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      TrackEvent.Response tracked = new();
      if (tracked is TResponse trackedResponse)
      {
        return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(trackedResponse);
      }

      object body = request is UpdateProfile.Command ? Updated : Fetched;
      if (body is TResponse response)
      {
        return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(response);
      }

      SharedProblemDetails problem = new()
      {
        Status = 500,
        Title = "No scripted response for " + typeof(TResponse).Name + ".",
      };
      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(problem);
    }
  }

  private sealed class IdleNavigationManager : NavigationManager
  {
    public IdleNavigationManager()
    {
      Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
    }
  }
}
