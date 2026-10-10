#region Purpose
// Task 260/265: the sign-in / Microsoft 365 interactions (and the RedirectToLogin auth-gate
// navigation) are TimeWarp.State actions — each one
// dispatched headless here asserts its effect (navigation target, state change, outcome).
#endregion

#region Design
// C-create in-proc ServiceProvider (same shape as SignOutSpa): a scripted IWebServerApiService
// answers per request type, NavigationManager records every NavigateTo with its forceLoad flag,
// and ISessionStorageService / IJSRuntime are FakeItEasy fakes. The passkey ceremony is driven
// only to its first HTTP leg (Start{Passkey,}Registration / StartPasskeyAuthentication answer with a problem): the browser
// half needs a real authenticator, and what this suite pins is the action seam — failure marks
// CeremonyFailed, reaches NotificationState, and does not navigate. States are re-read from the
// Store after Send (clone-on-dispatch replaces instances). Fetch actions (session, Microsoft 365
// offered/choice) are covered for their success, problem and throwing reads.
#endregion

namespace SignInState_;

using Blazored.SessionStorage;
using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;

[TestTag("Integration")]
public class SignInActions_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SignInActions_Should_>();

  public static async Task LinkMicrosoft365_ForceLoad_The_Link_Challenge_Back_To_Settings()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CredentialsState.LinkMicrosoft365ActionSet.Action());

    spa.Navigations(scope).ShouldBe([("/api/identity/entra/challenge?mode=link&returnUrl=%2FSettings", true)]);
  }

  public static async Task SignInWithMicrosoft365_ForceLoad_The_Bootstrap_Challenge_With_A_Safe_Return()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new SignInState.SignInWithMicrosoft365ActionSet.Action("/Admin/Roles"));
    await scope.Send(new SignInState.SignInWithMicrosoft365ActionSet.Action("//evil.example"));

    spa.Navigations(scope).ShouldBe
    ([
      ("/api/identity/entra/challenge?mode=bootstrap&returnUrl=%2FAdmin%2FRoles", true),
      ("/api/identity/entra/challenge?mode=bootstrap&returnUrl=%2F", true)
    ]);
  }

  public static async Task RedirectToLogin_ForceLoad_Login_With_A_Safe_Return()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new SignInState.RedirectToLoginActionSet.Action("/Admin/Roles?tab=2"));
    await scope.Send(new SignInState.RedirectToLoginActionSet.Action("/"));
    await scope.Send(new SignInState.RedirectToLoginActionSet.Action("//evil.example"));
    await scope.Send(new SignInState.RedirectToLoginActionSet.Action("/Login"));

    spa.Navigations(scope).ShouldBe
    ([
      ("/Login?returnUrl=%2FAdmin%2FRoles%3Ftab%3D2", true),
      ("/Login", true),
      ("/Login", true),
      ("/Login", true)
    ]);
  }

  public static async Task FetchMicrosoft365Offered_Read_The_Server_Flag_And_Fail_Closed()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    spa.Api.Answer<GetEntraSignInOffered.Query>(_ => new GetEntraSignInOffered.Response(offered: true));
    await scope.Send(new SignInState.FetchMicrosoft365OfferedActionSet.Action());
    scope.Store.GetState<SignInState>().Microsoft365Offered.ShouldBeTrue();

    spa.Api.Answer<GetEntraSignInOffered.Query>(_ => throw new HttpRequestException("BFF down"));
    await scope.Send(new SignInState.FetchMicrosoft365OfferedActionSet.Action());
    scope.Store.GetState<SignInState>().Microsoft365Offered.ShouldBeFalse();
  }

  public static async Task CreateAccountFromMicrosoft365_Navigate_To_The_Safe_Destination()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<CompleteEntraBootstrapCreate.Command>
    (
      _ => new CompleteEntraBootstrapCreate.Response(PrincipalId.New(), "/Settings")
    );

    await scope.Send(new SignInState.CreateAccountFromMicrosoft365ActionSet.Action());

    SignInState state = scope.Store.GetState<SignInState>();
    state.CeremonyFailed.ShouldBeFalse();
    state.IsAuthenticated.ShouldBe(true);
    spa.Navigations(scope).ShouldBe([("/Settings", false)]);
  }

  public static async Task CreateAccountFromMicrosoft365_Mark_The_Choice_Expired_On_An_Expired_Problem()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<GetEntraBootstrapChoice.Query>(_ => new GetEntraBootstrapChoice.Response(valid: true, destination: "/"));
    spa.Api.Problem<CompleteEntraBootstrapCreate.Command>
    (
      new SharedProblemDetails { Status = 400, Title = "Microsoft 365 sign-in expired" }
    );

    await scope.Send(new SignInState.FetchMicrosoft365ChoiceActionSet.Action());
    scope.Store.GetState<SignInState>().Microsoft365ChoiceValid.ShouldBe(true);

    await scope.Send(new SignInState.CreateAccountFromMicrosoft365ActionSet.Action());

    SignInState state = scope.Store.GetState<SignInState>();
    state.CeremonyFailed.ShouldBeTrue();
    state.Microsoft365ChoiceValid.ShouldBe(false);
    spa.Navigations(scope).ShouldBeEmpty();
    scope.Store.GetState<NotificationState>().Messages
      .ShouldContain(message => message.Title == "Microsoft 365 sign-in expired");
  }

  public static async Task SignInWithPasskey_Report_A_Failed_Ceremony_Without_Navigating()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Problem<StartPasskeyAuthentication.Command>
    (
      new SharedProblemDetails { Status = 501, Title = "Passkey sign-in unavailable" }
    );

    await scope.Send(new SignInState.SignInWithPasskeyActionSet.Action("/Settings"));

    scope.Store.GetState<SignInState>().CeremonyFailed.ShouldBeTrue();
    spa.Navigations(scope).ShouldBeEmpty();
    scope.Store.GetState<NotificationState>().Messages
      .ShouldContain(message => message.Title == "Passkey sign-in unavailable");
  }

  public static async Task CreateAccountWithPasskey_Report_A_Failed_Ceremony_Without_Navigating()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Problem<StartPasskeyRegistration.Command>
    (
      new SharedProblemDetails { Status = 501, Title = "Passkey registration unavailable" }
    );

    await scope.Send(new SignInState.CreateAccountWithPasskeyActionSet.Action("/Settings"));

    scope.Store.GetState<SignInState>().CeremonyFailed.ShouldBeTrue();
    spa.Navigations(scope).ShouldBeEmpty();
    scope.Store.GetState<NotificationState>().Messages
      .ShouldContain(message => message.Title == "Passkey registration unavailable");
  }

  public static async Task UseExistingAccountForMicrosoft365_Report_A_Failure_Without_Navigating()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<GetEntraBootstrapChoice.Query>(_ => new GetEntraBootstrapChoice.Response(valid: true, destination: "/"));
    spa.Api.Problem<StartPasskeyAuthentication.Command>
    (
      new SharedProblemDetails { Status = 400, Title = "Microsoft 365 sign-in expired" }
    );

    await scope.Send(new SignInState.FetchMicrosoft365ChoiceActionSet.Action());
    await scope.Send(new SignInState.UseExistingAccountForMicrosoft365ActionSet.Action());

    SignInState state = scope.Store.GetState<SignInState>();
    state.CeremonyFailed.ShouldBeTrue();
    state.Microsoft365ChoiceValid.ShouldBe(false);
    spa.Navigations(scope).ShouldBeEmpty();
    scope.Store.GetState<NotificationState>().Messages
      .ShouldContain(message => message.Title == "Microsoft 365 sign-in expired");
  }

  public static async Task FetchSession_Read_The_Session_And_Fail_Closed()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    spa.Api.Answer<GetCurrentSession.Query>(_ => new GetCurrentSession.Response(true, PrincipalId.New()));
    await scope.Send(new SignInState.FetchSessionActionSet.Action());
    scope.Store.GetState<SignInState>().IsAuthenticated.ShouldBe(true);

    spa.Api.Answer<GetCurrentSession.Query>(_ => new GetCurrentSession.Response(false, null));
    await scope.Send(new SignInState.FetchSessionActionSet.Action());
    scope.Store.GetState<SignInState>().IsAuthenticated.ShouldBe(false);

    spa.Api.Problem<GetCurrentSession.Query>(new SharedProblemDetails { Status = 500, Title = "Session unavailable" });
    await scope.Send(new SignInState.FetchSessionActionSet.Action());
    scope.Store.GetState<SignInState>().IsAuthenticated.ShouldBeNull();
  }

  public static async Task FetchMicrosoft365Choice_Read_Expired_On_Failure()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    spa.Api.Answer<GetEntraBootstrapChoice.Query>(_ => new GetEntraBootstrapChoice.Response(valid: true, destination: "/"));
    await scope.Send(new SignInState.FetchMicrosoft365ChoiceActionSet.Action());
    scope.Store.GetState<SignInState>().Microsoft365ChoiceValid.ShouldBe(true);

    spa.Api.Answer<GetEntraBootstrapChoice.Query>(_ => throw new HttpRequestException("BFF down"));
    await scope.Send(new SignInState.FetchMicrosoft365ChoiceActionSet.Action());
    scope.Store.GetState<SignInState>().Microsoft365ChoiceValid.ShouldBe(false);
  }

  public static async Task ForgetPasskeySoftPromptLater_Remove_The_Session_Key()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CredentialsState.ForgetPasskeySoftPromptLaterActionSet.Action());

    A.CallTo(() => spa.SessionStorage.RemoveItemAsync(PasskeySoftPrompt.LaterStorageKey, A<CancellationToken>._))
      .MustHaveHappenedOnceExactly();
  }

  public static async Task DismissPasskeySoftPrompt_Persist_Later_Only_When_Asked()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CredentialsState.DismissPasskeySoftPromptActionSet.Action());
    A.CallTo(spa.SessionStorage).MustNotHaveHappened();

    await scope.Send(new CredentialsState.DismissPasskeySoftPromptActionSet.Action(rememberForSession: true));

    scope.Store.GetState<CredentialsState>().PasskeySoftPromptDismissed.ShouldBeTrue();
    A.CallTo(() => spa.SessionStorage.SetItemAsync(PasskeySoftPrompt.LaterStorageKey, true, A<CancellationToken>._))
      .MustHaveHappenedOnceExactly();
  }

  private sealed class SignInSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ScriptedApiService Api { get; } = new();
    public ISessionStorageService SessionStorage { get; } = A.Fake<ISessionStorageService>();

    public SignInSpa()
    {
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
      services.AddTimeWarpStateBlazor();

      services.AddScoped<
        TimeWarp.Features.Persistence.IPersistenceService,
        TimeWarp.Features.Persistence.PersistenceService>();
      services.AddSingleton<TimeWarp.Architecture.Services.IWebServerApiService>(Api);
      services.AddScoped<AuthenticationStateProvider, SignedInAuthenticationStateProvider>();
      services.AddSingleton(A.Fake<IJSRuntime>());
      services.AddSingleton(SessionStorage);
      services.AddScoped<TimeWarp.Architecture.Services.PasskeyCeremonyClient>();
      services.AddScoped<NavigationManager, RecordingNavigationManager>();

      ServiceProvider = services.BuildServiceProvider();
    }

    public List<(string Uri, bool ForceLoad)> Navigations(SpaTestScope scope) =>
      ((RecordingNavigationManager)scope.ServiceProvider.GetRequiredService<NavigationManager>()).Navigations;

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  /// <summary>Answers each request type with a scripted response or problem; unscripted requests fail loudly.</summary>
  private sealed class ScriptedApiService : TimeWarp.Architecture.Services.IWebServerApiService
  {
    private readonly Dictionary<Type, Func<IApiRequest, object>> Answers = [];

    public void Answer<TRequest>(Func<TRequest, object> answer) where TRequest : IApiRequest =>
      Answers[typeof(TRequest)] = request => answer((TRequest)request);

    public void Problem<TRequest>(SharedProblemDetails problem) where TRequest : IApiRequest =>
      Answers[typeof(TRequest)] = _ => problem;

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      if (!Answers.TryGetValue(request.GetType(), out Func<IApiRequest, object>? answer))
      {
        throw new InvalidOperationException($"No scripted response for {request.GetType()}.");
      }

      return answer(request) switch
      {
        SharedProblemDetails problem => Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(problem),
        TResponse response => Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(response),
        object other => throw new InvalidOperationException($"Scripted {other.GetType()} is not {typeof(TResponse)}."),
        _ => throw new InvalidOperationException("Scripted response was null.")
      };
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
}
