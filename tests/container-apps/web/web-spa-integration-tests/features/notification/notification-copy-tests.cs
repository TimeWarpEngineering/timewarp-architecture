#region Purpose
// Task 313: CopyMessage writes a bar's text to the clipboard, confirms with an icon swap, and
// reports a clipboard failure as a Warning bar; the MessageBars Copy button dispatches it.
#endregion

#region Design
// C-create in-proc ServiceProvider (TimeWarp.State + ClientPipeline), same shape as
// SignOut_Should_: the closed-box Aspire host cannot observe IJSRuntime calls. The clipboard is
// an externality, so IJSRuntime is a recording fake (import → module → "WriteText"); throwing a
// JSException from the module stands in for a denied clipboard, and a TaskCanceledException
// for a JS-interop timeout.
// The dispatch test renders MessageBars on ClickRenderer, a minimal Renderer that records each
// element's onclick handler id by data-qa and fires it with DispatchEventAsync (no bUnit in this
// repo). Clicking MessageBarCopy must reach the fake clipboard through the real pipeline, which
// proves the button dispatches CopyMessage rather than calling the clipboard itself.
#endregion

namespace NotificationCopy_;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using static TimeWarp.Architecture.Features.NotificationState;

[TestTag("Integration")]
public class CopyMessage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CopyMessage_Should_>();

  public static Task Format_Title_Only_When_There_Is_No_Body()
  {
    NotificationMessage message = new("id", MessageBarIntent.Error, "Passkey rejected.", null, null);

    ClipboardText(message).ShouldBe("Passkey rejected.");
    return Task.CompletedTask;
  }

  public static Task Format_Title_Blank_Line_Body()
  {
    NotificationMessage message =
      new("id", MessageBarIntent.Error, "Already on this account", "This passkey is already on this account.", null);

    ClipboardText(message).ShouldBe("Already on this account\n\nThis passkey is already on this account.");
    return Task.CompletedTask;
  }

  public static async Task Write_Title_And_Body_To_Clipboard_And_Confirm_Until_Expiry()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Error, "Save failed", "The server is unavailable."));
    string id = scope.Store.GetState<NotificationState>().Messages[0].Id;

    await scope.Send(new CopyMessageActionSet.Action(id));

    spa.JsRuntime.ImportedSpecifier.ShouldBe(ClipboardJsModule.Specifier);
    spa.JsRuntime.Module.Calls.ShouldBe([(ClipboardJsModule.WriteTextExport, "Save failed\n\nThe server is unavailable.")]);
    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBe(id);
    state.CopiedUntil.ShouldNotBeNull();
    state.NextExpiry.ShouldBe(state.CopiedUntil);
    state.Messages.Count.ShouldBe(1, "a successful copy adds no bar");

    // The host's ExpireMessages timer ends the confirmation; the Error bar itself stays.
    await scope.Send(new ExpireMessagesActionSet.Action(state.CopiedUntil.Value + TimeSpan.FromSeconds(1)));
    state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBeNull();
    state.NextExpiry.ShouldBeNull();
    state.Messages.Count.ShouldBe(1);
  }

  public static async Task Report_Warning_When_Clipboard_Rejects()
  {
    using CopySpa spa = new(clipboardThrows: true);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Error, "Save failed"));
    string id = scope.Store.GetState<NotificationState>().Messages[0].Id;

    await scope.Send(new CopyMessageActionSet.Action(id));

    spa.JsRuntime.Module.Calls.Count.ShouldBe(1);
    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBeNull();
    state.Messages.Count.ShouldBe(2);
    NotificationMessage warning = state.Messages.Single(message => message.Intent == MessageBarIntent.Warning);
    warning.Title.ShouldBe(CopyFailedTitle);
    warning.Body.ShouldBe(CopyFailedBody);
  }

  public static async Task Report_Warning_When_Clipboard_Times_Out()
  {
    using CopySpa spa = new(clipboardThrows: false, clipboardTimesOut: true);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Error, "Save failed"));
    string id = scope.Store.GetState<NotificationState>().Messages[0].Id;

    await scope.Send(new CopyMessageActionSet.Action(id));

    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBeNull();
    state.Messages.Count.ShouldBe(2, "a timeout is a Warning, not a pipeline Error bar");
    state.Messages.Single(message => message.Intent == MessageBarIntent.Warning).Title.ShouldBe(CopyFailedTitle);
    state.Messages.ShouldNotContain(message => message.Intent == MessageBarIntent.Error && message.Title != "Save failed");
  }

  public static async Task Ignore_Unknown_Id()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CopyMessageActionSet.Action("gone"));

    spa.JsRuntime.Module.Calls.ShouldBeEmpty();
    scope.Store.GetState<NotificationState>().Messages.ShouldBeEmpty();
  }

  public static async Task Copy_Button_Dispatches_CopyMessage()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Info, "Heads up", "Details here."));

    await using ClickRenderer renderer = new(scope.ServiceProvider);
    await renderer.RenderAsync<MessageBars>();

    renderer.Labels["MessageBarCopy"].ShouldBe("Copy message");
    spa.JsRuntime.Module.Calls.ShouldBeEmpty();

    await renderer.ClickAsync("MessageBarCopy");

    spa.JsRuntime.Module.Calls.ShouldBe([(ClipboardJsModule.WriteTextExport, "Heads up\n\nDetails here.")]);
    scope.Store.GetState<NotificationState>().CopiedMessageId.ShouldNotBeNull();

    // A diff batch carries only changed attributes, so paint a fresh host to read the swapped label.
    await using ClickRenderer fresh = new(scope.ServiceProvider);
    await fresh.RenderAsync<MessageBars>();
    fresh.Labels["MessageBarCopy"].ShouldBe("Copied", "the copied row swaps to the Copied confirmation");
  }

  private sealed class CopySpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public RecordingJsRuntime JsRuntime { get; }

    public CopySpa(bool clipboardThrows, bool clipboardTimesOut = false)
    {
      JsRuntime = new RecordingJsRuntime(clipboardThrows, clipboardTimesOut);
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
      services.AddAuthorizationCore();

      services.AddScoped<
        TimeWarp.Features.Persistence.IPersistenceService,
        TimeWarp.Features.Persistence.PersistenceService>();
      services.AddScoped<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();
      services.AddSingleton<IJSRuntime>(JsRuntime);
      services.AddScoped<NavigationManager, TestNavigationManager>();

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

  private sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
  }

  // Reads full element frames only: enough for a first render; diffs are not tracked.
  // Renderer and RenderBatch are RenderTree types (BL0006); this test double reads them on purpose.
#pragma warning disable BL0006
  private sealed class ClickRenderer : Renderer
  {
    public ClickRenderer(IServiceProvider serviceProvider)
      : base(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>())
    {
    }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    protected override RendererInfo RendererInfo { get; } = new("Test", isInteractive: false);

    /// <summary>Latest onclick handler id per data-qa.</summary>
    public Dictionary<string, ulong> ClickHandlers { get; } = [];

    /// <summary>Latest aria-label per data-qa.</summary>
    public Dictionary<string, string> Labels { get; } = [];

    public Task RenderAsync<TComponent>() where TComponent : IComponent =>
      Dispatcher.InvokeAsync(() =>
      {
        IComponent component = InstantiateComponent(typeof(TComponent));
        int componentId = AssignRootComponentId(component);
        return RenderRootComponentAsync(componentId, ParameterView.Empty);
      });

    public Task ClickAsync(string dataQa) =>
      Dispatcher.InvokeAsync(() =>
        DispatchEventAsync(ClickHandlers[dataQa], fieldInfo: null, new MouseEventArgs()));

    protected override void HandleException(Exception exception) =>
      ExceptionDispatchInfo.Capture(exception).Throw();

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
      ArrayRange<RenderTreeFrame> frames = renderBatch.ReferenceFrames;
      for (int index = 0; index < frames.Count; index++)
      {
        if (frames.Array[index].FrameType != RenderTreeFrameType.Element)
        {
          continue;
        }

        string? dataQa = null;
        string? label = null;
        ulong onClick = 0;
        for (int attribute = index + 1;
          attribute < frames.Count && frames.Array[attribute].FrameType == RenderTreeFrameType.Attribute;
          attribute++)
        {
          RenderTreeFrame frame = frames.Array[attribute];
          if (frame.AttributeName == "data-qa")
          {
            dataQa = frame.AttributeValue as string;
          }
          else if (frame.AttributeName == "aria-label")
          {
            label = frame.AttributeValue as string;
          }
          else if (frame.AttributeName == "onclick" && frame.AttributeEventHandlerId != 0)
          {
            onClick = frame.AttributeEventHandlerId;
          }
        }

        if (dataQa is null)
        {
          continue;
        }

        if (onClick != 0)
        {
          ClickHandlers[dataQa] = onClick;
        }

        if (label is not null)
        {
          Labels[dataQa] = label;
        }
      }

      return Task.CompletedTask;
    }
  }
#pragma warning restore BL0006

  private sealed class RecordingJsRuntime : IJSRuntime
  {
    public RecordingJsRuntime(bool clipboardThrows, bool clipboardTimesOut)
    {
      Module = new RecordingModule(clipboardThrows, clipboardTimesOut);
    }

    public string? ImportedSpecifier { get; private set; }
    public RecordingModule Module { get; }

    // Test double: this forwards to the other overload. It is not a browser interop call.
#pragma warning disable BL0016
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);
#pragma warning restore BL0016

    public ValueTask<TValue> InvokeAsync<TValue>
    (
      string identifier,
      CancellationToken cancellationToken,
      object?[]? args
    )
    {
      if (identifier == "import" && args is [ClipboardJsModule.Specifier])
      {
        ImportedSpecifier = ClipboardJsModule.Specifier;
        return ValueTask.FromResult((TValue)(object)Module);
      }

      // FluentUI components' own interop is not under test.
      return ValueTask.FromResult<TValue>(default!);
    }
  }

  private sealed class RecordingModule : IJSObjectReference
  {
    private readonly bool Throws;
    private readonly bool TimesOut;

    public RecordingModule(bool throws, bool timesOut)
    {
      Throws = throws;
      TimesOut = timesOut;
    }

    public List<(string Export, string Text)> Calls { get; } = [];

    // Test double: this forwards to the other overload. It is not a browser interop call.
#pragma warning disable BL0016
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);
#pragma warning restore BL0016

    public ValueTask<TValue> InvokeAsync<TValue>
    (
      string identifier,
      CancellationToken cancellationToken,
      object?[]? args
    )
    {
      Calls.Add((identifier, args is [string text] ? text : string.Empty));
      if (Throws)
      {
        throw new JSException("NotAllowedError: Write permission denied.");
      }

      if (TimesOut)
      {
        throw new TaskCanceledException("JS interop call timed out.");
      }

      return ValueTask.FromResult<TValue>(default!);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }
}
