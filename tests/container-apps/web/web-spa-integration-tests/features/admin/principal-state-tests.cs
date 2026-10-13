#region Purpose
// Task 328: the admin Principals actions dispatched headless against a scripted API, asserting
// what lands in PrincipalState.
#endregion

#region Design
// C-create in-proc ServiceProvider (same shape as feedback-admin-state-tests): a scripted
// IWebServerApiService answers per request type; states are re-read from the Store after Send.
// Covers SetSearch and SetSort landing on the fetch query, a failed fetch clearing the rows,
// OpenPrincipal storing the detail and seeding the role draft, and the non-revealing 404.
#endregion

namespace PrincipalState_;

using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TimeWarp.Architecture.Features.Admin.Principals;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;

[TestTag("Integration")]
public class PrincipalActions_Should_
{
  private static readonly Guid PrincipalId = Guid.Parse("11111111-1111-1111-1111-111111111111");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PrincipalActions_Should_>();

  public static async Task Fetch_Defaults_To_Store_Order_And_Stores_The_Rows()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    ListPrincipals.Query? seen = null;
    spa.Api.Answer<ListPrincipals.Query>(query =>
    {
      seen = query;
      return ListPrincipals.GetMockResponseFactory()(query);
    });

    await scope.Send(new PrincipalState.FetchPrincipalsActionSet.Action());

    seen.ShouldNotBeNull();
    seen.Search.ShouldBeNull();
    seen.SortBy.ShouldBe(PrincipalListSortColumn.CreatedAt);
    seen.SortDescending.ShouldBeFalse();
    PrincipalState state = scope.Store.GetState<PrincipalState>();
    state.Principals.ShouldNotBeNull().Count.ShouldBe(2);
    state.ListLoadError.ShouldBeNull();
  }

  public static async Task Search_And_Sort_Land_On_The_Fetch_Query()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    ListPrincipals.Query? seen = null;
    spa.Api.Answer<ListPrincipals.Query>(query =>
    {
      seen = query;
      return ListPrincipals.GetMockResponseFactory()(query);
    });

    await scope.Send(new PrincipalState.SetSearchActionSet.Action("  ada  "));
    await scope.Send(new PrincipalState.SetSortActionSet.Action(PrincipalListSortColumn.Email, sortDescending: true));
    await scope.Send(new PrincipalState.FetchPrincipalsActionSet.Action());

    seen.ShouldNotBeNull();
    seen.Search.ShouldBe("ada");
    seen.SortBy.ShouldBe(PrincipalListSortColumn.Email);
    seen.SortDescending.ShouldBeTrue();
    PrincipalState state = scope.Store.GetState<PrincipalState>();
    state.Search.ShouldBe("ada");
    state.SortBy.ShouldBe(PrincipalListSortColumn.Email);

    await scope.Send(new PrincipalState.SetSearchActionSet.Action("  "));
    await scope.Send(new PrincipalState.FetchPrincipalsActionSet.Action());
    seen.Search.ShouldBeNull("blank text clears the search");
    seen.SortBy.ShouldBe(PrincipalListSortColumn.Email, "clearing the search keeps the sort");
  }

  public static async Task Failed_Fetch_Clears_The_Rows_And_Sets_A_List_Error()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<ListPrincipals.Query>(query => ListPrincipals.GetMockResponseFactory()(query));
    await scope.Send(new PrincipalState.FetchPrincipalsActionSet.Action());
    scope.Store.GetState<PrincipalState>().Principals.ShouldNotBeNull();

    spa.Api.Problem<ListPrincipals.Query>(new SharedProblemDetails { Status = 403, Title = "Profile access required" });
    await scope.Send(new PrincipalState.SetSearchActionSet.Action("ada"));
    await scope.Send(new PrincipalState.FetchPrincipalsActionSet.Action());

    PrincipalState state = scope.Store.GetState<PrincipalState>();
    state.Principals.ShouldBeNull();
    state.ListLoadError.ShouldNotBeNull();
  }

  public static async Task Open_Stores_The_Detail_And_Seeds_The_Role_Draft()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<GetPrincipal.Query>(query => GetPrincipal.GetMockResponseFactory()(query));

    await scope.Send(new PrincipalState.OpenPrincipalActionSet.Action(PrincipalId));

    PrincipalState state = scope.Store.GetState<PrincipalState>();
    GetPrincipal.PrincipalDetailDto current = state.Current.ShouldNotBeNull();
    current.PrincipalId.Value.ShouldBe(PrincipalId);
    current.Profile.ShouldNotBeNull().DisplayName.ShouldBe("Ada Lovelace");
    state.LoadError.ShouldBeNull();
    state.IsRoleSelected(PrincipalId, RoleIds.Member).ShouldBeTrue();
    state.IsRoleSelected(PrincipalId, RoleIds.Administrator).ShouldBeFalse();
  }

  public static async Task Open_Of_A_Missing_Principal_Sets_A_Non_Revealing_Error()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Problem<GetPrincipal.Query>(new SharedProblemDetails { Status = 404, Title = "Principal not found" });

    await scope.Send(new PrincipalState.OpenPrincipalActionSet.Action(PrincipalId));

    PrincipalState state = scope.Store.GetState<PrincipalState>();
    state.Current.ShouldBeNull();
    state.LoadError.ShouldBe("This principal is not available.");
  }

  public static async Task Open_Of_Another_Principal_Clears_The_Previous_One_When_It_Fails()
  {
    using AdminSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Api.Answer<GetPrincipal.Query>(query => GetPrincipal.GetMockResponseFactory()(query));
    await scope.Send(new PrincipalState.OpenPrincipalActionSet.Action(PrincipalId));
    scope.Store.GetState<PrincipalState>().Current.ShouldNotBeNull();

    spa.Api.Problem<GetPrincipal.Query>(new SharedProblemDetails { Status = 500, Title = "Boom" });
    await scope.Send(new PrincipalState.OpenPrincipalActionSet.Action(Guid.NewGuid()));

    scope.Store.GetState<PrincipalState>().Current.ShouldBeNull();
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
