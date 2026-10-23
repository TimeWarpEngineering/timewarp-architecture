#region Purpose
// ClearXaiApiKey asks through ConfirmationStandIn before it calls the API.
#endregion

#region Design
// In-proc SPA container, same shape as the Ask readiness host, with a scripted
// IWebServerApiService and the wrapper's stand-in. No is no request, no notification, and
// the seeded status stays. Yes applies the cleared status and publishes the success note.
// The stand-in is the confirmation service. The API double is not a mock of the handler.
#endregion

namespace ClearXaiApiKeyConfirmation_;

using TimeWarp.Architecture;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Ai;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;

[TestTag("Unit")]
public class ClearXaiApiKey_Given_A_StandIn
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ClearXaiApiKey_Given_A_StandIn>();

  public static async Task No_Should_Skip_Without_A_Call_Or_A_Notification()
  {
    ConfirmationStandIn standIn = new() { Answer = false };
    ScriptedWebServer server = new(Cleared());
    using ClearSpa spa = new(server, standIn);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    Seed(scope);

    await scope.Send(new XaiApiKeyState.ClearXaiApiKeyActionSet.Action());

    XaiApiKeyState state = scope.Store.GetState<XaiApiKeyState>();
    state.Configured.ShouldBeTrue();
    state.Suffix.ShouldBe("3799");
    state.SaveFailed.ShouldBeFalse();
    standIn.CallCount.ShouldBe(1);
    server.RequestTypes.ShouldBeEmpty();
    scope.Store.GetState<NotificationState>().Messages.Count.ShouldBe(0);
    standIn.CallCount.ShouldBe(1);
    standIn.Title.ShouldBe(XaiApiKeyState.ClearXaiApiKeyActionSet.Handler.ConfirmTitle);
    standIn.Message.ShouldBe(XaiApiKeyState.ClearXaiApiKeyActionSet.Handler.ConfirmMessage);
    standIn.ConfirmLabel.ShouldBe(XaiApiKeyState.ClearXaiApiKeyActionSet.Handler.ConfirmLabel);
    standIn.CancelLabel.ShouldBe(XaiApiKeyState.ClearXaiApiKeyActionSet.Handler.CancelLabel);
  }

  public static async Task Yes_Should_Clear_And_Notify()
  {
    ConfirmationStandIn standIn = new() { Answer = true };
    ScriptedWebServer server = new(Cleared());
    server.Outcomes[typeof(GetAgentChatConfiguration.Response)] = new GetAgentChatConfiguration.Response
    {
      Configured = false,
      SetupCommand = XaiChatDefaults.DeployedSetupHint,
    };
    using ClearSpa spa = new(server, standIn);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    Seed(scope);

    await scope.Send(new XaiApiKeyState.ClearXaiApiKeyActionSet.Action());

    XaiApiKeyState state = scope.Store.GetState<XaiApiKeyState>();
    state.Configured.ShouldBeFalse();
    state.Suffix.ShouldBeNull();
    state.SaveFailed.ShouldBeFalse();
    state.HasSnapshot.ShouldBeTrue();
    server.RequestTypes.ShouldBe(["Command -> Response", "Query -> Response"]);
    scope.Store.GetState<NotificationState>().Messages.Select(message => message.Title)
      .ShouldContain("xAI API key cleared.");
    standIn.CallCount.ShouldBe(1);
  }

  private static void Seed(SpaTestScope scope) =>
    scope.Store.GetState<XaiApiKeyState>().Apply(new XaiApiKeyStatus(
      managedByHost: false,
      configured: true,
      storageAvailable: true,
      canChange: true,
      needsReplacement: false,
      suffix: "3799",
      setAt: null,
      setByPrincipalId: null,
      setupHint: XaiChatDefaults.DeployedSetupHint));

  private static ClearXaiApiKey.Response Cleared() =>
    new(
      managedByHost: false,
      configured: false,
      storageAvailable: true,
      canChange: true,
      needsReplacement: false,
      suffix: null,
      setAt: null,
      setByPrincipalId: null,
      setupHint: XaiChatDefaults.DeployedSetupHint);

  /// <summary>One scripted web-server response.</summary>
  private sealed class ScriptedWebServer(object Outcome) : IWebServerApiService
  {
    public Dictionary<Type, object> Outcomes { get; } = [];

    public List<string> RequestTypes { get; } = [];

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      cancellationToken.ThrowIfCancellationRequested();
      RequestTypes.Add(request.GetType().Name + " -> " + typeof(TResponse).Name);
      object outcome = Outcomes.TryGetValue(typeof(TResponse), out object? byType) ? byType : Outcome;
      if (outcome is TResponse response)
      {
        return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(response);
      }

      throw new InvalidOperationException($"No scripted outcome for {typeof(TResponse).Name}.");
    }
  }

  /// <summary>In-proc SPA container. Confirmation is the stand-in.</summary>
  private sealed class ClearSpa(IWebServerApiService WebServer, IConfirmationService Confirmation) : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; } = Build(WebServer, Confirmation);

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }

    private static ServiceProvider Build(IWebServerApiService webServer, IConfirmationService confirmation)
    {
      ServiceCollection services = new();
      services.AddLogging();
      services.AddFluentUIComponents();
      services.AddSingleton(confirmation);
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.Assemblies =
          [
            typeof(IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped(_ => webServer);
      return services.BuildServiceProvider();
    }
  }
}
