#region Purpose
// Shared private helpers and nested test doubles (RecordingChatClient, TagSpa) for the AskTag_Should_ partial class.
#endregion

namespace AskTag_;

using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation;

[TestTag("Unit")]
public partial class AskTag_Should_
{

  private static async Task<string> RenderAdminSectionAsync(string[] permissions)
  {
    using TagSpa spa = new(permissions);
    using IServiceScope scope = spa.ServiceProvider.CreateScope();
    await using HtmlRenderer renderer = new(scope.ServiceProvider, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
    {
      RenderFragment section = builder =>
      {
        builder.OpenComponent<Section>(0);
        builder.AddComponentParameter(1, nameof(Section.Title), "Admin only");
        builder.AddComponentParameter(2, nameof(Section.Policy), PermissionIds.AdminFeedbackRead);
        builder.AddComponentParameter(3, nameof(Section.ChildContent), (RenderFragment)(content => content.AddContent(0, "Secret admin text")));
        builder.CloseComponent();
      };
      ParameterView parameters = ParameterView.FromDictionary
      (
        new Dictionary<string, object?> { [nameof(CascadingAuthenticationState.ChildContent)] = section }
      );
      Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent root = await renderer.RenderComponentAsync<CascadingAuthenticationState>(parameters);
      await root.QuiescenceTask;
      return root.ToHtmlString();
    });
  }

  private static AskTagCandidate Candidate(string kind, string label) =>
    new(kind, label, label + " text", DataQa: null, Landmark: null, Reference: null);

  private static AskTagCandidate Row(string reference) =>
    new("Feedback row", "Dark theme broken", "Dark theme broken Bug Open", "AdminFeedbackRow", "Card: Feedback", reference);

  private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

  private static string RowJson(string prefix) => JsonSerializer.Serialize(Row("@" + prefix + ":" + FeedbackId), WebJson);

  private sealed class RecordingChatClient : IChatClient
  {
    public List<List<ChatMessage>> Requests { get; } = [];

    public List<ChatOptions?> Options { get; } = [];

    public bool Fail { get; init; }

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    )
    {
      if (Fail)
      {
        throw new InvalidOperationException("upstream failed");
      }

      Requests.Add([.. messages]);
      Options.Add(options);
      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
      ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
      foreach (ChatResponseUpdate update in response.ToChatResponseUpdates())
      {
        yield return update;
      }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
  }

  private sealed class TagSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ClaimsPrincipal User { get; }

    // The fake ask-tag.js module, kept only to read its recorded calls.
    private readonly object ModuleFake;

    /// <summary>The first argument of every ask-tag.js call to <paramref name="identifier"/>, in order.</summary>
    public IReadOnlyList<object?> ModuleCalls(string identifier) =>
    [
      .. Fake.GetCalls(ModuleFake)
        .Where(call => Equals(call.Arguments.FirstOrDefault(), identifier))
        .Select(static call => call.Arguments.OfType<object?[]>().FirstOrDefault()?.FirstOrDefault()),
    ];

    public TagSpa(string[]? permissions, Func<ValueTask<string?>>? pick = null, Func<ValueTask<bool>>? start = null)
    {
      User = permissions is null
        ? new ClaimsPrincipal(new ClaimsIdentity())
        : new ClaimsPrincipal(new ClaimsIdentity(
          [
            new Claim("sub", Guid.NewGuid().ToString()),
            .. permissions.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
          ],
          authenticationType: "test"));

      // ask-tag.js: Start arms when the test says (at once by default), Answer answers what the test
      // says; every other module call is a no-op and is recorded.
      IJSObjectReference module = A.Fake<IJSObjectReference>();
      ModuleFake = module;
      Func<ValueTask<string?>> answer = pick ?? (static () => new ValueTask<string?>(""));
      Func<ValueTask<bool>> armed = start ?? (static () => new ValueTask<bool>(true));
      A.CallTo(module).WithReturnType<ValueTask<string?>>().ReturnsLazily(answer);
      A.CallTo(module).WithReturnType<ValueTask<bool>>().ReturnsLazily(armed);
      IJSRuntime jsRuntime = A.Fake<IJSRuntime>();
      A.CallTo(jsRuntime).WithReturnType<ValueTask<IJSObjectReference>>().Returns(new ValueTask<IJSObjectReference>(module));

      ServiceCollection services = new();
      services.AddLogging();
      services.AddFluentUIComponents();
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
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(User));
      services.AddScoped(_ => jsRuntime);
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<IWebServerApiService>(provider => new MockWebApiService
      (
        A.Fake<IApiService>(),
        provider.GetRequiredService<ILogger<MockWebApiService>>(),
        provider
      ));

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

  private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(user));
  }
}
