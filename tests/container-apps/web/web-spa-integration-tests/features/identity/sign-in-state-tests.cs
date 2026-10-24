#region Purpose
// Task 260/265: the sign-in / Microsoft 365 interactions (and the RedirectToLogin auth-gate
// navigation) are TimeWarp.State actions — each one
// dispatched headless here asserts its effect (navigation target, state change, outcome).
#endregion

#region Design
// C-create in-proc ServiceProvider (same shape as SignOutSpa): a scripted IWebServerApiService
// answers per request type, NavigationManager records every NavigateTo with its forceLoad flag,
// and ISessionStorageService is a FakeItEasy fake. IJSRuntime is BrowserJsRuntime: it answers the
// web-authn.js module's credential exports with canned JSON and its SendJson export with the
// BrowserFetchScript reply for the route, which records each browser request. Task 350:
// session-issuing completions go out through SendJson, never IWebServerApiService (ScriptedApiService throws on an unscripted
// request, so a completion sent the old way fails the test), and a successful completion navigates
// with forceLoad so the next request carries the new cookie. A failed first leg (Start* problem)
// marks CeremonyFailed, reaches NotificationState, and does not navigate. States are re-read from the
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
using System.Text.Json;
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

  public static async Task FetchOfferedSignInMethods_Read_The_Server_List_And_Fail_Closed()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    scope.Store.GetState<SignInState>().Microsoft365Offered.ShouldBeNull();
    scope.Store.GetState<SignInState>().PasskeyOffered.ShouldBeNull();

    spa.Api.Answer<GetEntraSignInOffered.Query>(_ =>
      new GetEntraSignInOffered.Response([SignInMethod.Passkey, SignInMethod.Microsoft365]));
    await scope.Send(new SignInState.FetchOfferedSignInMethodsActionSet.Action());
    scope.Store.GetState<SignInState>().Microsoft365Offered.ShouldBe(true);
    scope.Store.GetState<SignInState>().PasskeyOffered.ShouldBe(true);

    spa.Api.Answer<GetEntraSignInOffered.Query>(_ => throw new HttpRequestException("BFF down"));
    await scope.Send(new SignInState.FetchOfferedSignInMethodsActionSet.Action());
    scope.Store.GetState<SignInState>().OfferedMethods.ShouldBeNull();
    scope.Store.GetState<SignInState>().Microsoft365Offered.ShouldBeNull();
    scope.Store.GetState<SignInState>().PasskeyOffered.ShouldBeNull();
    scope.Store.GetState<SignInState>().OfferedMethodsFailed.ShouldBeTrue();

    spa.Api.Answer<GetEntraSignInOffered.Query>(_ =>
      new GetEntraSignInOffered.Response([SignInMethod.Passkey]));
    await scope.Send(new SignInState.FetchOfferedSignInMethodsActionSet.Action());
    scope.Store.GetState<SignInState>().OfferedMethodsFailed.ShouldBeFalse();
    scope.Store.GetState<SignInState>().PasskeyOffered.ShouldBe(true);
    scope.Store.GetState<SignInState>().Microsoft365Offered.ShouldBe(false);
  }

  public static async Task CreateAccountFromMicrosoft365_Navigate_To_The_Safe_Destination()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Browser.Answer<CompleteEntraBootstrapCreate.Command>
    (
      new CompleteEntraBootstrapCreate.Response(PrincipalId.New(), "/Settings")
    );

    await scope.Send(new SignInState.CreateAccountFromMicrosoft365ActionSet.Action());

    SignInState state = scope.Store.GetState<SignInState>();
    state.CeremonyFailed.ShouldBeFalse();
    state.IsAuthenticated.ShouldBe(true);
    spa.Navigations(scope).ShouldBe([("/Settings", true)]);
    spa.Browser.Sent.ShouldBe([("POST", "http://localhost/api/identity/entra/choice/create")]);
  }

  public static async Task CreateAccountFromMicrosoft365_Mark_The_Choice_Expired_On_An_Expired_Problem()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<GetEntraBootstrapChoice.Query>(_ => new GetEntraBootstrapChoice.Response(valid: true, destination: "/"));
    spa.Browser.Problem<CompleteEntraBootstrapCreate.Command>
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

  public static async Task SignInWithPasskey_Complete_In_The_Browser_And_Force_Load()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<StartPasskeyAuthentication.Command>(_ => new StartPasskeyAuthentication.Response("{}"));
    spa.Browser.Answer<CompletePasskeyAuthentication.Command>(new CompletePasskeyAuthentication.Response(PrincipalId.New()));

    await scope.Send(new SignInState.SignInWithPasskeyActionSet.Action("/Admin/Roles"));

    SignInState state = scope.Store.GetState<SignInState>();
    state.CeremonyFailed.ShouldBeFalse();
    state.IsAuthenticated.ShouldBe(true);
    spa.Navigations(scope).ShouldBe([("/Admin/Roles", true)]);
    spa.Browser.Sent.ShouldBe([("POST", "http://localhost/api/identity/passkey/authenticate")]);
  }

  public static async Task CreateAccountWithPasskey_Complete_In_The_Browser_And_Force_Load()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<StartPasskeyRegistration.Command>(_ => new StartPasskeyRegistration.Response("{}"));
    spa.Browser.Answer<CompletePasskeyRegistration.Command>
    (
      new CompletePasskeyRegistration.Response(PrincipalId.New(), CredentialId.New(), "Test Provider")
    );

    await scope.Send(new SignInState.CreateAccountWithPasskeyActionSet.Action("/"));

    SignInState state = scope.Store.GetState<SignInState>();
    state.CeremonyFailed.ShouldBeFalse();
    state.IsAuthenticated.ShouldBe(true);
    spa.Navigations(scope).ShouldBe([("/Settings", true)]);
    spa.Browser.Sent.ShouldBe([("POST", "http://localhost/api/identity/passkey/register")]);
  }

  public static async Task CreateAccountWithPasskey_Report_A_Problem_From_The_Browser_Completion()
  {
    using SignInSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<StartPasskeyRegistration.Command>(_ => new StartPasskeyRegistration.Response("{}"));
    spa.Browser.Problem<CompletePasskeyRegistration.Command>
    (
      new SharedProblemDetails { Status = 409, Title = "Passkey already registered" }
    );

    await scope.Send(new SignInState.CreateAccountWithPasskeyActionSet.Action("/Settings"));

    scope.Store.GetState<SignInState>().CeremonyFailed.ShouldBeTrue();
    spa.Navigations(scope).ShouldBeEmpty();
    scope.Store.GetState<NotificationState>().Messages
      .ShouldContain(message => message.Title == "Passkey already registered");
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
    public BrowserFetchScript Browser { get; } = new();
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

      services.AddTimeWarpStatePersistence();
      services.AddSingleton<TimeWarp.Architecture.Services.IWebServerApiService>(Api);
      services.AddScoped<AuthenticationStateProvider, SignedInAuthenticationStateProvider>();
      services.AddSingleton<IJSRuntime>(new BrowserJsRuntime(Browser));
      services.Configure<JsonSerializerOptions>(ContractSerializationDefaults.Apply);
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

  /// <summary>
  /// Scripted replies for web-authn.js SendJson, keyed by route path, plus every (method, url) the
  /// browser was asked to send. Unscripted routes fail loudly.
  /// </summary>
  private sealed class BrowserFetchScript
  {
    private readonly Dictionary<string, (int Status, string ContentType, string Body)> Replies = [];

    public List<(string Method, string Url)> Sent { get; } = [];

    public void Answer<TRequest>(object response) where TRequest : IApiRequest, new() =>
      Replies[RoutePath<TRequest>()] =
        (200, "application/json; charset=utf-8", JsonSerializer.Serialize(response, ContractSerializationDefaults.Options));

    public void Problem<TRequest>(SharedProblemDetails problem) where TRequest : IApiRequest, new() =>
      Replies[RoutePath<TRequest>()] =
        (problem.Status ?? 400, "application/problem+json", JsonSerializer.Serialize(problem, ContractSerializationDefaults.Options));

    public string Reply(string method, string url)
    {
      Sent.Add((method, url));
      if (!Replies.TryGetValue(new Uri(url).AbsolutePath, out (int Status, string ContentType, string Body) reply))
      {
        throw new InvalidOperationException($"No scripted browser reply for {method} {url}.");
      }

      return JsonSerializer.Serialize(new { status = reply.Status, contentType = reply.ContentType, body = reply.Body });
    }

    private static string RoutePath<TRequest>() where TRequest : IApiRequest, new() =>
      "/" + new TRequest().GetRoute().TrimStart('/');
  }

  /// <summary>The browser half: web-authn.js credential exports answer canned JSON; SendJson asks the script.</summary>
  private sealed class BrowserJsRuntime(BrowserFetchScript script) : IJSRuntime, IJSObjectReference
  {
    // Test double: this forwards to the other overload. It is not a browser interop call.
#pragma warning disable BL0016
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);
#pragma warning restore BL0016

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      _ = cancellationToken;
      object? result = identifier switch
      {
        "import" => this,
        "CreateCredential" => """{"credentialId":"cid","clientDataJson":"cdj","attestationObject":"att"}""",
        "GetCredential" =>
          """{"credentialId":"cid","clientDataJson":"cdj","authenticatorData":"ad","signature":"sig","userHandle":null}""",
        "SendJson" => script.Reply((string)args![0]!, (string)args[1]!),
        _ => default(TValue)
      };
      return ValueTask.FromResult((TValue)result!);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
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
