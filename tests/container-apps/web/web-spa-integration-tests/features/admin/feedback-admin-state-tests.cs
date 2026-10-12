#region Purpose
// Task 314: the admin feedback actions dispatched headless against a scripted API, asserting
// what lands in FeedbackAdminState.
#endregion

#region Design
// C-create in-proc ServiceProvider (same shape as SignInSpa): a scripted IWebServerApiService
// answers per request type. States are re-read from the Store after Send (clone-on-dispatch
// replaces instances). Covers the filter-to-query path, mutations storing the returned item,
// the reply/note success flags, the non-revealing 404, paging (a filter change resets Page),
// opening another item clearing the previous one, and a failed fetch clearing the rows.
#endregion

namespace FeedbackAdminState_;

using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TimeWarp.Architecture.Features.Admin.Feedback;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;

[TestTag("Integration")]
public class FeedbackAdminActions_Should_
{
  private static readonly Guid ItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeedbackAdminActions_Should_>();

  public static async Task Fetch_Sends_The_Current_Filter_And_Stores_The_Page()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    ListFeedback.Query? seen = null;
    spa.Api.Answer<ListFeedback.Query>(query =>
    {
      seen = query;
      return ListFeedback.GetMockResponseFactory()(query);
    });

    await scope.Send(new FeedbackAdminState.SetFilterActionSet.Action(FeedbackStatus.Planned, FeedbackKind.BugReport, includeClosed: true));
    await scope.Send(new FeedbackAdminState.FetchFeedbackListActionSet.Action());

    seen.ShouldNotBeNull();
    seen.Status.ShouldBe(FeedbackStatus.Planned);
    seen.Kind.ShouldBe(FeedbackKind.BugReport);
    seen.IncludeClosed.ShouldBeTrue();
    seen.Page.ShouldBe(1);
    FeedbackAdminState state = scope.Store.GetState<FeedbackAdminState>();
    state.Items.ShouldNotBeNull();
    state.Items.Count.ShouldBe(2);
    state.TotalCount.ShouldBe(2);
  }

  public static async Task ChangeStatus_Stores_The_Returned_Item()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<ChangeFeedbackStatus.Command>(command =>
      new ChangeFeedbackStatus.Response(FeedbackAdminView.Sample(ItemId, command.Status), filerEmailed: true));

    await scope.Send(new FeedbackAdminState.ChangeFeedbackStatusActionSet.Action(ItemId, FeedbackStatus.Completed));

    scope.Store.GetState<FeedbackAdminState>().Current.ShouldNotBeNull().Status.ShouldBe(FeedbackStatus.Completed);
  }

  public static async Task Reply_And_Note_Set_Their_Success_Flags_Only_On_Success()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<ReplyToFeedback.Command>(_ => new ReplyToFeedback.Response(FeedbackAdminView.Sample(ItemId), filerEmailed: false));
    spa.Api.Problem<AddFeedbackNote.Command>(new SharedProblemDetails { Status = 400, Title = "Invalid note" });

    await scope.Send(new FeedbackAdminState.ReplyToFeedbackActionSet.Action(ItemId, "Thanks"));
    await scope.Send(new FeedbackAdminState.AddFeedbackNoteActionSet.Action(ItemId, "x"));

    FeedbackAdminState state = scope.Store.GetState<FeedbackAdminState>();
    state.LastReplySucceeded.ShouldBeTrue();
    state.LastNoteSucceeded.ShouldBeFalse();
    state.Current.ShouldNotBeNull();
  }

  public static async Task Open_Of_A_Missing_Item_Sets_A_Non_Revealing_Error()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Problem<GetFeedbackForAdmin.Query>(new SharedProblemDetails { Status = 404, Title = "Not found" });

    await scope.Send(new FeedbackAdminState.OpenFeedbackForAdminActionSet.Action(ItemId));

    FeedbackAdminState state = scope.Store.GetState<FeedbackAdminState>();
    state.Current.ShouldBeNull();
    state.LoadError.ShouldBe("This feedback item is not available.");
  }

  public static async Task Filter_Change_Resets_Page_And_Paging_Flags_Follow_The_Total()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    ListFeedback.Query? seen = null;
    spa.Api.Answer<ListFeedback.Query>(query =>
    {
      seen = query;
      return new ListFeedback.Response(totalCount: (2 * query.PageSize) + 1, [], query.Page, query.PageSize);
    });

    await scope.Send(new FeedbackAdminState.SetPageActionSet.Action(2));
    await scope.Send(new FeedbackAdminState.FetchFeedbackListActionSet.Action());
    seen.ShouldNotBeNull().Page.ShouldBe(2);
    FeedbackAdminState state = scope.Store.GetState<FeedbackAdminState>();
    state.HasPreviousPage.ShouldBeTrue();
    state.HasNextPage.ShouldBeTrue();

    await scope.Send(new FeedbackAdminState.SetFilterActionSet.Action(null, FeedbackKind.BugReport, includeClosed: false));
    await scope.Send(new FeedbackAdminState.FetchFeedbackListActionSet.Action());
    seen.Page.ShouldBe(1);
    scope.Store.GetState<FeedbackAdminState>().HasPreviousPage.ShouldBeFalse();
  }

  public static async Task Failed_Fetch_Clears_The_Rows_And_Sets_A_List_Error()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<ListFeedback.Query>(query => ListFeedback.GetMockResponseFactory()(query));
    await scope.Send(new FeedbackAdminState.FetchFeedbackListActionSet.Action());
    scope.Store.GetState<FeedbackAdminState>().Items.ShouldNotBeNull();

    spa.Api.Problem<ListFeedback.Query>(new SharedProblemDetails { Status = 500, Title = "Boom" });
    await scope.Send(new FeedbackAdminState.SetFilterActionSet.Action(FeedbackStatus.Planned, null, includeClosed: false));
    await scope.Send(new FeedbackAdminState.FetchFeedbackListActionSet.Action());

    FeedbackAdminState state = scope.Store.GetState<FeedbackAdminState>();
    state.Items.ShouldBeNull();
    state.TotalCount.ShouldBe(0);
    state.ListLoadError.ShouldNotBeNull();
  }

  public static async Task Open_Of_Another_Item_Clears_The_Previous_One_When_It_Fails()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<GetFeedbackForAdmin.Query>(query => new GetFeedbackForAdmin.Response(FeedbackAdminView.Sample(query.FeedbackItemId)));
    await scope.Send(new FeedbackAdminState.OpenFeedbackForAdminActionSet.Action(ItemId));
    scope.Store.GetState<FeedbackAdminState>().Current.ShouldNotBeNull();

    spa.Api.Problem<GetFeedbackForAdmin.Query>(new SharedProblemDetails { Status = 500, Title = "Boom" });
    await scope.Send(new FeedbackAdminState.OpenFeedbackForAdminActionSet.Action(Guid.NewGuid()));

    scope.Store.GetState<FeedbackAdminState>().Current.ShouldBeNull();
  }

  private sealed class AdminSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ScriptedApiService Api { get; } = new();

    public AdminSpa()
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
      services.AddSingleton<TimeWarp.Architecture.Services.IWebServerApiService>(Api);
      services.AddScoped<AuthenticationStateProvider, SignedInAuthenticationStateProvider>();
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
}
