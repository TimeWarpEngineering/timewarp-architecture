#region Purpose
// Test doubles for the clipboard copy tests: an in-proc SPA, a recording IJSRuntime, and a
// minimal click-and-read Renderer.
#endregion

#region Design
// Shared by CopyMessage_Should_ (task 313, shell rows) and CopyMessageBar_Should_ (task 445,
// page-local bars). The clipboard is an externality, so IJSRuntime is a recording fake
// (import → module → "WriteText"); throwing a JSException from the module stands in for a
// denied clipboard, and a TaskCanceledException for a JS-interop timeout.
// ClickRenderer records every element's onclick handler id and aria-label by data-qa, in render
// order, from full element frames; it also records each aria-label a diff sets, so a test can
// see a re-rendered button's new label without painting a fresh host.
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
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;

internal sealed class CopySpa : ISpaTestApplication, IDisposable
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

    services.AddTimeWarpStatePersistence();
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

internal sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
  public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
    Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
}

// Reads full element frames (a first render, or an inserted subtree). From a diff it reads only
// the aria-label values SetAttribute edits apply (UpdatedLabels).
// Renderer and RenderBatch are RenderTree types (BL0006); this test double reads them on purpose.
#pragma warning disable BL0006
internal sealed class ClickRenderer : Renderer
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

  /// <summary>Every onclick handler id per data-qa, in render order.</summary>
  public Dictionary<string, List<ulong>> AllClickHandlers { get; } = [];

  /// <summary>Every aria-label per data-qa, in render order.</summary>
  public Dictionary<string, List<string>> AllLabels { get; } = [];

  /// <summary>aria-label values set by diff edits, in order.</summary>
  public List<string> UpdatedLabels { get; } = [];

  public Task RenderAsync<TComponent>(IDictionary<string, object?>? parameters = null) where TComponent : IComponent =>
    Dispatcher.InvokeAsync(() =>
    {
      IComponent component = InstantiateComponent(typeof(TComponent));
      int componentId = AssignRootComponentId(component);
      ParameterView parameterView = parameters is null ? ParameterView.Empty : ParameterView.FromDictionary(parameters);
      return RenderRootComponentAsync(componentId, parameterView);
    });

  public Task ClickAsync(string dataQa) =>
    Dispatcher.InvokeAsync(() =>
      DispatchEventAsync(ClickHandlers[dataQa], fieldInfo: null, new MouseEventArgs()));

  public Task ClickAsync(string dataQa, int index) =>
    Dispatcher.InvokeAsync(() =>
      DispatchEventAsync(AllClickHandlers[dataQa][index], fieldInfo: null, new MouseEventArgs()));

  protected override void HandleException(Exception exception) =>
    ExceptionDispatchInfo.Capture(exception).Throw();

  protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
  {
    ArrayRange<RenderTreeFrame> frames = renderBatch.ReferenceFrames;
    for (int diff = 0; diff < renderBatch.UpdatedComponents.Count; diff++)
    {
      ArrayBuilderSegment<RenderTreeEdit> edits = renderBatch.UpdatedComponents.Array[diff].Edits;
      for (int edit = 0; edit < edits.Count; edit++)
      {
        RenderTreeEdit change = edits.Array[edits.Offset + edit];
        if (change.Type == RenderTreeEditType.SetAttribute
          && frames.Array[change.ReferenceFrameIndex] is { AttributeName: "aria-label", AttributeValue: string updated })
        {
          UpdatedLabels.Add(updated);
        }
      }
    }

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
        Add(AllClickHandlers, dataQa, onClick);
      }

      if (label is not null)
      {
        Labels[dataQa] = label;
        Add(AllLabels, dataQa, label);
      }
    }

    return Task.CompletedTask;
  }

  private static void Add<TValue>(Dictionary<string, List<TValue>> values, string dataQa, TValue value)
  {
    if (!values.TryGetValue(dataQa, out List<TValue>? list))
    {
      list = [];
      values[dataQa] = list;
    }

    list.Add(value);
  }
}
#pragma warning restore BL0006

internal sealed class RecordingJsRuntime : IJSRuntime
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

internal sealed class RecordingModule : IJSObjectReference
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
