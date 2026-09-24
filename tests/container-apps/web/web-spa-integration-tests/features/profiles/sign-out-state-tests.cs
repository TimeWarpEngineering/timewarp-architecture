#region Purpose
// Task 251: SignOutActionSet clears Profile/Authorization/Credentials/AgentLinks state and hands
// sign-out to the browser (sign-out.js form POST) — no server-side BFF call, no soft navigation.
#endregion

#region Design
// C-create in-proc ServiceProvider (TimeWarp.State + ClientPipeline), same shape as
// CredentialsSpaTestApplication: the closed-box Aspire host cannot observe IJSRuntime calls.
// IJSRuntime is a recording fake (import → module → "SignOut" export with the two
// SignOutBrowserSession paths); IWebServerApiService is a FakeItEasy fake asserted untouched, which
// pins the removal of the loopback EndBrowserSession POST that broke InteractiveServer. States are
// seeded through reflection on their private setters (no fetch actions need scripting) and re-read
// from the Store after Send (clone-on-dispatch replaces instances). The handler is render-mode
// agnostic, so one path covers WebAssembly, Server and Auto; the browser-visible cookie outcome is
// pinned server-side in web-server-integration-tests (sign-out-browser-session-tests).
#endregion

namespace SignOutState_;

using System.Reflection;
using System.Security.Claims;
using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Features.AgentLinks;
using TimeWarp.Architecture.Features.Authorization;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Profiles;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;

[TestTag("Integration")]
public class SignOut_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SignOut_Should_>();

  public static async Task ClearFourStates_And_SubmitBrowserSignOut()
  {
    using SignOutSpa spa = new(jsThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    Seed(scope.Store);

    await scope.Send(new ProfileState.SignOutActionSet.Action());

    AssertCleared(scope.Store);
    spa.JsRuntime.Identifiers.ShouldBe(["import"]);
    spa.JsRuntime.ImportedSpecifier.ShouldBe(SignOutJsModule.Specifier);
    spa.JsRuntime.Module.Calls.Count.ShouldBe(1);
    (string export, object?[] args) = spa.JsRuntime.Module.Calls[0];
    export.ShouldBe(SignOutJsModule.ExportName);
    args.ShouldBe([SignOutBrowserSession.AntiforgeryTokenPath, SignOutBrowserSession.Path]);

    A.CallTo(spa.ApiService).MustNotHaveHappened();
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    ((RecordingNavigationManager)navigation).Navigations.ShouldBeEmpty(
      "The form POST is the navigation; a soft NavigateTo would keep the circuit's principal.");
  }

  public static async Task ForceLoadLogin_When_BrowserSignOutThrows()
  {
    using SignOutSpa spa = new(jsThrows: true);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    Seed(scope.Store);

    await scope.Send(new ProfileState.SignOutActionSet.Action());

    AssertCleared(scope.Store);
    RecordingNavigationManager navigation =
      (RecordingNavigationManager)scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigation.Navigations.ShouldBe([(SignOutBrowserSession.RedirectPath, true)]);
    A.CallTo(spa.ApiService).MustNotHaveHappened();
  }

  private static void Seed(IStore store)
  {
    SetProperty(store.GetState<ProfileState>(), nameof(ProfileState.Alias), "signed-in-alias");
    SetProperty(store.GetState<AuthorizationState>(), "RolesList", new List<Guid> { Guid.NewGuid() });
    SetProperty(store.GetState<CredentialsState>(), nameof(CredentialsState.CeremonyFailed), true);
    SetProperty(
      store.GetState<AgentLinksState>(),
      nameof(AgentLinksState.Items),
      (IReadOnlyList<ListAgentHumanLinks.LinkSummary>)
      [
        new ListAgentHumanLinks.LinkSummary(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Pending")
      ]);

    store.GetState<ProfileState>().Alias.ShouldBe("signed-in-alias");
    store.GetState<AuthorizationState>().Roles.ShouldNotBeNull();
    store.GetState<CredentialsState>().CeremonyFailed.ShouldBeTrue();
    store.GetState<AgentLinksState>().Items.ShouldNotBeEmpty();
  }

  private static void AssertCleared(IStore store)
  {
    store.GetState<ProfileState>().Alias.ShouldBeNull();
    store.GetState<AuthorizationState>().Roles.ShouldBeNull();
    store.GetState<CredentialsState>().CeremonyFailed.ShouldBeFalse();
    store.GetState<AgentLinksState>().Items.ShouldBeEmpty();
  }

  private static void SetProperty(object target, string name, object? value)
  {
    PropertyInfo property = target.GetType().GetProperty(
      name,
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException($"{target.GetType().Name}.{name} not found.");
    property.SetValue(target, value);
  }

  private sealed class SignOutSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public RecordingJsRuntime JsRuntime { get; }
    public IWebServerApiService ApiService { get; } = A.Fake<IWebServerApiService>();

    public SignOutSpa(bool jsThrows)
    {
      JsRuntime = new RecordingJsRuntime(jsThrows);
      ServiceCollection services = new();

      services.AddLogging();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.Assemblies =
          [
            typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );

      services.AddScoped<
        TimeWarp.Features.Persistence.IPersistenceService,
        TimeWarp.Features.Persistence.PersistenceService>();
      services.AddSingleton(ApiService);
      services.AddScoped<AuthenticationStateProvider, SignedInAuthenticationStateProvider>();
      services.AddSingleton<IJSRuntime>(JsRuntime);
      services.AddScoped<NavigationManager, RecordingNavigationManager>();

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

  private sealed class SignedInAuthenticationStateProvider : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
      ClaimsIdentity identity = new([new Claim("sub", Guid.NewGuid().ToString())], authenticationType: "test");
      return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
  }

  private sealed class RecordingNavigationManager : NavigationManager
  {
    public List<(string Uri, bool ForceLoad)> Navigations { get; } = [];

    public RecordingNavigationManager()
    {
      Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
      Navigations.Add((uri, options.ForceLoad));
      Uri = ToAbsoluteUri(uri).ToString();
      NotifyLocationChanged(isInterceptedLink: false);
    }
  }

  private sealed class RecordingJsRuntime : IJSRuntime
  {
    public RecordingJsRuntime(bool throws)
    {
      Module = new RecordingModule(throws);
    }

    public List<string> Identifiers { get; } = [];
    public string? ImportedSpecifier { get; private set; }
    public RecordingModule Module { get; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
      string identifier,
      CancellationToken cancellationToken,
      object?[]? args)
    {
      Identifiers.Add(identifier);
      if (identifier == "import")
      {
        ImportedSpecifier = args is { Length: > 0 } ? args[0] as string : null;
        return ValueTask.FromResult((TValue)(object)Module);
      }

      throw new InvalidOperationException($"Unexpected identifier '{identifier}'.");
    }
  }

  private sealed class RecordingModule : IJSObjectReference
  {
    private readonly bool Throws;

    public RecordingModule(bool throws)
    {
      Throws = throws;
    }

    public List<(string Export, object?[] Args)> Calls { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
      string identifier,
      CancellationToken cancellationToken,
      object?[]? args)
    {
      Calls.Add((identifier, args ?? []));
      if (Throws)
      {
        throw new JSException("Sign-out antiforgery token request failed: 503");
      }

      return ValueTask.FromResult<TValue>(default!);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }
}
