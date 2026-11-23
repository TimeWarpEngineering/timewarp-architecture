#region Purpose
// ProfilePage applies a prefill only after the loaded profile is the working copy.
#endregion

#region Design
// HtmlRenderer's static host reports IsInteractive false, so ProfilePage would skip its fetch.
// This host subclasses StaticHtmlRenderer and reports an interactive renderer, the same shape
// as AiPage_Should_. That host cancels the display update, so the renderer never calls
// OnAfterRenderAsync. The test calls it after the load settles and after each draft, which is
// when an interactive circuit would. The profile read can wait on a gate, so the first call
// is before CopyCommandFromProfile and the one after the gate is after it. A scripted
// IConfirmationService records the replace question. The page's command is private, so the
// tests read it by name. No Aspire host: the store and the rendered command are the facts.
#endregion

namespace ProfilePagePrefill_;

using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.Analytics;
using TimeWarp.Architecture.Features.Profiles;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Testing;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;

[TestTag("Unit")]
public class ProfilePage_Should_
{
  private const string Draft = """{"displayName":"Grace"}""";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ProfilePage_Should_>();

  public static async Task Apply_A_Clean_Draft_Without_Asking()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    PageView view = await RenderAsync(spa, scope, afterOpen: page =>
    {
      ArgumentNullException.ThrowIfNull(page);
      return scope.Send(new ProfileState.SetProfileSuggestionActionSet.Action(Draft)).AsTask();
    });

    view.DisplayName.ShouldBe("Grace");
    view.Confirmations.ShouldBe(0);
    view.SuggestionJson.ShouldBeNull();
    view.Html.ShouldContain("data-qa=\"ProfileDisplayName\"");
    view.Html.ShouldContain("data-qa=\"AgentFilled\"");
  }

  public static async Task Ask_Before_Replacing_A_Dirty_Form()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Confirmation.Answer = true;

    PageView view = await RenderAsync(spa, scope, afterOpen: page =>
    {
      CommandOf(page).DisplayName = "Pat";
      return scope.Send(new ProfileState.SetProfileSuggestionActionSet.Action(Draft)).AsTask();
    });

    view.DisplayName.ShouldBe("Grace");
    view.Confirmations.ShouldBe(1);
    view.Message.ShouldBe(
      "An agent drafted new profile values. Applying them replaces unsaved changes on this form.");
    view.SuggestionJson.ShouldBeNull();
    view.Html.ShouldContain("data-qa=\"AgentFilled\"");
  }

  public static async Task Keep_The_Typed_Value_When_The_Person_Declines()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Confirmation.Answer = false;

    PageView view = await RenderAsync(spa, scope, afterOpen: page =>
    {
      CommandOf(page).DisplayName = "Pat";
      return scope.Send(new ProfileState.SetProfileSuggestionActionSet.Action(Draft)).AsTask();
    });

    view.DisplayName.ShouldBe("Pat");
    view.Confirmations.ShouldBe(1);
    view.SuggestionJson.ShouldBeNull();
    view.Html.ShouldNotContain("data-qa=\"AgentFilled\"");
  }

  public static async Task Hide_The_Agent_Badge_When_The_Person_Changes_The_Field()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    string? withBadge = null;

    PageView view = await RenderAsync(
      spa,
      scope,
      afterOpen: page =>
      {
        ArgumentNullException.ThrowIfNull(page);
        return scope.Send(new ProfileState.SetProfileSuggestionActionSet.Action(Draft)).AsTask();
      },
      afterSettled: (page, html) =>
      {
        withBadge = html;
        CommandOf(page).DisplayName = "Pat";
        return Task.CompletedTask;
      });

    withBadge.ShouldNotBeNull().ShouldContain("data-qa=\"AgentFilled\"");
    view.DisplayName.ShouldBe("Pat");
    view.Html.ShouldNotContain("data-qa=\"AgentFilled\"");
  }

  public static async Task Keep_A_Draft_That_Arrives_While_The_Profile_Is_Loading()
  {
    using ProfileSpa spa = new();
    spa.Api.HoldProfile = true;
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new ProfileState.SetProfileSuggestionActionSet.Action(Draft));

    PageView view = await RenderAsync(
      spa,
      scope,
      duringHold: async page =>
      {
        CommandOf(page).DisplayName.ShouldBeNull();
        scope.Store.GetState<ProfileState>().ProfileSuggestionJson.ShouldNotBeNull().ShouldContain("Grace");
        spa.Confirmation.Calls.ShouldBe(0);
        await PumpAfterRenderAsync(page);
        CommandOf(page).DisplayName.ShouldBeNull();
        scope.Store.GetState<ProfileState>().ProfileSuggestionJson.ShouldNotBeNull().ShouldContain("Grace");
        spa.Confirmation.Calls.ShouldBe(0);
      });

    view.DisplayName.ShouldBe("Grace");
    view.Confirmations.ShouldBe(0);
    view.SuggestionJson.ShouldBeNull();
    view.Html.ShouldContain("data-qa=\"AgentFilled\"");
  }

  private static async Task<PageView> RenderAsync
  (
    ProfileSpa spa,
    SpaTestScope scope,
    Func<ProfilePage, Task>? duringHold = null,
    Func<ProfilePage, Task>? afterOpen = null,
    Func<ProfilePage, string, Task>? afterSettled = null
  )
  {
    IServiceProvider services = scope.ServiceProvider;
    PageCapture capture = new();
    await using InteractiveHtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent root = await renderer.Dispatcher.InvokeAsync(() =>
      renderer.BeginRenderingComponent(
        typeof(PageHost),
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
          [nameof(PageHost.Capture)] = capture,
        })));

    if (duringHold is not null)
    {
      ProfilePage loading = capture.Page ?? throw new InvalidOperationException("ProfilePage did not render while loading.");
      await duringHold(loading);
      spa.Api.ProfileGate.TrySetResult();
    }

    await root.QuiescenceTask;
    ProfilePage page = capture.Page ?? throw new InvalidOperationException("ProfilePage did not render.");
    await PumpAfterRenderAsync(page);
    if (afterOpen is not null)
    {
      await afterOpen(page);
      await PumpAfterRenderAsync(page);
    }

    page.ReRender();
    string html = await renderer.Dispatcher.InvokeAsync(() => WebUtility.HtmlDecode(root.ToHtmlString()));
    if (afterSettled is not null)
    {
      await afterSettled(page, html);
      page.ReRender();
      html = await renderer.Dispatcher.InvokeAsync(() => WebUtility.HtmlDecode(root.ToHtmlString()));
    }

    return new PageView(
      html,
      CommandOf(page).DisplayName,
      spa.Confirmation.Calls,
      spa.Confirmation.Message,
      scope.Store.GetState<ProfileState>().ProfileSuggestionJson);
  }

  private static async Task PumpAfterRenderAsync(ProfilePage page)
  {
    MethodInfo method = typeof(ProfilePage).GetMethod(
      "OnAfterRenderAsync",
      BindingFlags.Instance | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException("OnAfterRenderAsync");
    Task task = (Task?)method.Invoke(page, [false])
      ?? throw new InvalidOperationException("OnAfterRenderAsync");
    await task;
  }

  private static UpdateProfile.Command CommandOf(ProfilePage page)
  {
    PropertyInfo property = typeof(ProfilePage).GetProperty(
      "Command",
      BindingFlags.Instance | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException("Command");
    return (UpdateProfile.Command?)property.GetValue(page)
      ?? throw new InvalidOperationException("Command");
  }

  private sealed record PageView(
    string Html,
    string? DisplayName,
    int Confirmations,
    string? Message,
    string? SuggestionJson);

  private sealed class PageCapture
  {
    public ProfilePage? Page { get; set; }
  }

  private sealed class PageHost : ComponentBase
  {
    [Parameter] public PageCapture? Capture { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<ProfilePage>(0);
        child.AddComponentReferenceCapture(1, instance =>
        {
          if (Capture is not null)
          {
            Capture.Page = (ProfilePage)instance;
          }
        });
        child.CloseComponent();
      }));
      builder.CloseComponent();
    }
  }

  /// <summary>Static HTML rendering with <see cref="RendererInfo.IsInteractive"/> set.</summary>
  private sealed class InteractiveHtmlRenderer : StaticHtmlRenderer
  {
    public InteractiveHtmlRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
      : base(services, loggerFactory)
    {
    }

    protected override RendererInfo RendererInfo { get; } = new("Test", isInteractive: true);
  }

  private sealed class ProfileSpa : ISpaTestApplication, IDisposable
  {
    public ScriptedProfileApi Api { get; } = new();

    public RecordingConfirmation Confirmation { get; } = new();

    public IServiceProvider ServiceProvider { get; }

    public ProfileSpa()
    {
      ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, "ada")], authenticationType: "test");
      ServiceCollection services = StaticRenderHost.CreateServices(user: new ClaimsPrincipal(identity));
      services.AddSingleton<IWebServerApiService>(Api);
      services.AddSingleton<IConfirmationService>(Confirmation);
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
    public bool HoldProfile { get; set; }

    public TaskCompletionSource ProfileGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static readonly GetProfile.Response Fetched = new(
      displayName: "Ada",
      email: null,
      language: "en-US",
      region: "US",
      theme: "system",
      notifications: false,
      avatar: "data:image/svg+xml;base64,fetched");

    public async Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = request;
      cancellationToken.ThrowIfCancellationRequested();
      if (typeof(TResponse) == typeof(TrackEvent.Response))
      {
        return (TResponse)(object)new TrackEvent.Response();
      }

      if (typeof(TResponse) == typeof(GetProfile.Response))
      {
        if (HoldProfile)
        {
          await ProfileGate.Task;
        }

        return (TResponse)(object)Fetched;
      }

      if (typeof(TResponse) == typeof(GetUserXaiApiKeyStatus.Response))
      {
        return (TResponse)(object)new GetUserXaiApiKeyStatus.Response(
          visible: false,
          mode: XaiByokMode.Off,
          configured: false,
          needsReplacement: false,
          canChange: false,
          storageAvailable: false,
          supersededByHost: false,
          suffix: null);
      }

      throw new InvalidOperationException("No scripted response for " + typeof(TResponse).Name + ".");
    }
  }

  private sealed class RecordingConfirmation : IConfirmationService
  {
    public bool Answer { get; set; } = true;

    public int Calls { get; private set; }

    public string? Message { get; private set; }

    public Task<bool> ConfirmAsync
    (
      string title,
      string message,
      string confirmLabel,
      string cancelLabel,
      CancellationToken cancellationToken
    )
    {
      cancellationToken.ThrowIfCancellationRequested();
      Calls++;
      Message = message;
      _ = title;
      _ = confirmLabel;
      _ = cancelLabel;
      return Task.FromResult(Answer);
    }
  }
}
