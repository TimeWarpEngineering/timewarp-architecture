#region Purpose
// Prove update-then-fetch through the real ApiHandler base completes when HandleSuccess
// accidentally dispatches another action on the same state (fake IApiService, no host).
#endregion

#region Design
// Task 236: the per-state semaphore covers GetRequest → network only, then releases before
// HandleApiResponseAsync. Nested dispatch on the same state used to deadlock (Principals
// Save). This probe's Update HandleSuccess Sends Fetch; both are ApiHandler subclasses
// sharing IStore.GetSemaphore. Completing within the timeout means the lock is no longer
// held across HandleSuccess. Handlers in product code still must not nest; pages sequence.
#endregion

namespace ApiHandlerNestedDispatch_;

using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features;
using OneOf;
using TimeWarp.Foundation;
using TimeWarp.Foundation.Features;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;

[TestTag("Unit")]
public class ApiHandler_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ApiHandler_Should_>();

  [Timeout(5000)]
  public static async Task Complete_Nested_Fetch_When_Update_HandleSuccess_Dispatches()
  {
    using NestedDispatchSpaTestApplication app = new();
    using IServiceScope scope = app.ServiceProvider.CreateScope();
    IStore store = scope.ServiceProvider.GetRequiredService<IStore>();
    ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

    store.GetState<NestedDispatchProbeState>().Initialize();

    using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(4));
    await sender.Send(new NestedDispatchProbeState.UpdateActionSet.Action(), cancellationTokenSource.Token);

    NestedDispatchProbeState state = store.GetState<NestedDispatchProbeState>();
    state.UpdateCompleted.ShouldBeTrue();
    state.FetchCompleted.ShouldBeTrue();
    app.Api.CallCount.ShouldBe(2);
  }
}

public sealed partial class NestedDispatchProbeState : State<NestedDispatchProbeState>
{
  public bool UpdateCompleted { get; private set; }
  public bool FetchCompleted { get; private set; }

  public override void Initialize()
  {
    UpdateCompleted = false;
    FetchCompleted = false;
  }

  internal void MarkUpdateCompleted() => UpdateCompleted = true;

  internal void MarkFetchCompleted() => FetchCompleted = true;

  internal static class UpdateActionSet
  {
    internal sealed class Action : IBaseAction;

    internal sealed class Handler : ApiHandler<Action, NestedDispatchProbeRequest, NestedDispatchProbeResponse>
    {
      private readonly ISender Sender;

      public Handler
      (
        IStore store,
        IApiService apiService,
        ILogger<Handler> logger,
        ISender sender
      ) : base(store, apiService, logger)
      {
        Sender = sender;
      }

      protected override Task<NestedDispatchProbeRequest?> GetRequest(
        Action action,
        CancellationToken cancellationToken) =>
        Task.FromResult<NestedDispatchProbeRequest?>(new NestedDispatchProbeRequest());

      protected override async Task HandleSuccess(
        NestedDispatchProbeResponse response,
        CancellationToken cancellationToken)
      {
        _ = response;
        Store.GetState<NestedDispatchProbeState>().MarkUpdateCompleted();
        await Sender.Send(new FetchActionSet.Action(), cancellationToken);
      }

      protected override Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken)
      {
        _ = fileResponse;
        _ = cancellationToken;
        return Task.CompletedTask;
      }

      protected override Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
      {
        _ = problemDetails;
        _ = cancellationToken;
        return Task.CompletedTask;
      }
    }
  }

  internal static class FetchActionSet
  {
    internal sealed class Action : IBaseAction;

    internal sealed class Handler : ApiHandler<Action, NestedDispatchProbeRequest, NestedDispatchProbeResponse>
    {
      public Handler
      (
        IStore store,
        IApiService apiService,
        ILogger<Handler> logger
      ) : base(store, apiService, logger)
      {
      }

      protected override Task<NestedDispatchProbeRequest?> GetRequest(
        Action action,
        CancellationToken cancellationToken) =>
        Task.FromResult<NestedDispatchProbeRequest?>(new NestedDispatchProbeRequest());

      protected override Task HandleSuccess(
        NestedDispatchProbeResponse response,
        CancellationToken cancellationToken)
      {
        _ = response;
        _ = cancellationToken;
        Store.GetState<NestedDispatchProbeState>().MarkFetchCompleted();
        return Task.CompletedTask;
      }

      protected override Task HandleFileResponse(FileResponse fileResponse, CancellationToken cancellationToken)
      {
        _ = fileResponse;
        _ = cancellationToken;
        return Task.CompletedTask;
      }

      protected override Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
      {
        _ = problemDetails;
        _ = cancellationToken;
        return Task.CompletedTask;
      }
    }
  }
}

internal sealed class NestedDispatchProbeRequest : IApiRequest
{
  public string GetRoute() => "probe";
  public HttpVerb GetHttpVerb() => HttpVerb.Post;
}

internal sealed class NestedDispatchProbeResponse;

internal sealed class CompletingApiService : IApiService
{
  public int CallCount { get; private set; }

  public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>(
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    _ = request;
    _ = cancellationToken;
    CallCount++;
    TResponse response = Activator.CreateInstance<TResponse>();
    return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(response);
  }
}

internal sealed class NestedDispatchSpaTestApplication : IDisposable
{
  public IServiceProvider ServiceProvider { get; }
  public CompletingApiService Api { get; }

  public NestedDispatchSpaTestApplication()
  {
    Api = new CompletingApiService();
    ServiceCollection services = new();
    services.AddLogging();
    services.AddTimeWarpState(
      options =>
      {
        options.Assemblies =
        [
          typeof(NestedDispatchProbeState).Assembly
        ];
      });
    services.AddSingleton<IApiService>(Api);
    services.RemoveAll<INotificationHandler<TimeWarp.Features.StateTransactions.ExceptionNotification>>();
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
