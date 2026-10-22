#region Purpose
// Render FilterBar with HtmlRenderer and pin the toolbar, search box, and debounced callback.
#endregion

#region Design
// FilterBar is a leaf that needs FluentUI services and a TimeProvider. The markup cases use
// the system clock. The debounce cases use a fake TimeProvider whose Delay completes only when
// the test releases it, so there is no sleep. FilterBar injects that clock and passes it to
// SearchDebouncer. The debouncer tests call that type directly. Two more cases go through the
// component: a zero delay publishes from the renderer, and dispose cancels a pending publish.
#endregion

namespace FilterBarRender_;

using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class FilterBar_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FilterBar_Should_>();

  public static async Task Render_The_Toolbar_Role_And_Twe_Class()
  {
    string html = await RenderAsync([]);

    string root = RootTag(html);
    root.ShouldContain("twe-filter-bar");
    root.ShouldContain("role=\"toolbar\"");
    html.ShouldContain("fluent-text-input");
    html.ShouldNotContain("<style");
  }

  public static async Task Hide_The_Search_Box_When_ShowSearch_Is_False()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["ShowSearch"] = false,
      ["SearchPlaceholder"] = "Find reports"
    });

    html.ShouldNotContain("fluent-text-input");
    html.ShouldNotContain("Find reports");
    RootTag(html).ShouldContain("role=\"toolbar\"");
  }

  public static async Task Render_The_Placeholder_And_The_Value()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["SearchPlaceholder"] = "Find reports",
      ["SearchText"] = "acme"
    });

    html.ShouldContain("Find reports");
    html.ShouldContain("acme");
  }

  public static async Task Render_ChildContent()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["ShowSearch"] = false,
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-qa", "FilterChild");
        builder.AddContent(2, "Status");
        builder.CloseElement();
      })
    });

    html.ShouldContain("data-qa=\"FilterChild\"");
    html.ShouldContain("Status");
  }

  public static async Task Merge_Class_And_Extra_Attributes_On_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["ShowSearch"] = false,
      ["Class"] = "is-open",
      ["id"] = "filters",
      ["data-qa"] = "FilterBar"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-filter-bar");
    root.ShouldContain("is-open");
    root.ShouldContain("id=\"filters\"");
    root.ShouldContain("data-qa=\"FilterBar\"");
  }

  public static async Task Several_Keystrokes_Produce_One_Callback_With_The_Last_Text()
  {
    ManualDelay clock = new();
    List<string> seen = [];
    SearchDebouncer debouncer = new(clock);
    Task first = Schedule(debouncer, seen, "a");
    Task second = Schedule(debouncer, seen, "ac");
    Task third = Schedule(debouncer, seen, "acme");
    clock.Release();
    await Task.WhenAll(first, second, third);
    debouncer.Dispose();

    seen.ShouldBe(["acme"]);
  }

  public static async Task A_Clear_Fires_Immediately_And_Drops_The_Pending_Text()
  {
    ManualDelay clock = new();
    List<string> seen = [];
    SearchDebouncer debouncer = new(clock);
    Task typed = Schedule(debouncer, seen, "acme");
    Task cleared = debouncer.ScheduleAsync("", TimeSpan.FromMilliseconds(300), immediate: true, Publish(seen));
    seen.ShouldBe([""]);
    clock.Release();
    await Task.WhenAll(typed, cleared);
    debouncer.Dispose();

    seen.ShouldBe([""]);
  }

  public static async Task Dispose_Cancels_The_Pending_Callback()
  {
    ManualDelay clock = new();
    List<string> seen = [];
    SearchDebouncer debouncer = new(clock);
#pragma warning disable CA2025 // The pending wait is what dispose must cancel.
    Task pending = Schedule(debouncer, seen, "acme");
    debouncer.Dispose();
#pragma warning restore CA2025
    clock.Release();
    await pending;
    await debouncer.ScheduleAsync("later", TimeSpan.FromMilliseconds(300), immediate: true, Publish(seen));

    seen.ShouldBeEmpty();
  }

  public static async Task The_Bar_Publishes_Immediately_When_The_Delay_Is_Zero()
  {
    List<string> seen = [];
    (FilterBar bar, HtmlRenderer renderer) = await MountAsync(new Dictionary<string, object?>
    {
      ["SearchDelay"] = TimeSpan.Zero,
      ["SearchTextChanged"] = EventCallback.Factory.Create<string>(new object(), (string text) => seen.Add(text))
    });
    await using (renderer)
    {
      await renderer.Dispatcher.InvokeAsync(() => NotifyAsync(bar, "acme"));
    }

    seen.ShouldBe(["acme"]);
  }

  public static async Task Disposing_The_Bar_Drops_The_Pending_Callback()
  {
    ManualDelay clock = new();
    List<string> seen = [];
    (FilterBar bar, HtmlRenderer renderer) = await MountAsync(
      new Dictionary<string, object?>
      {
        ["SearchTextChanged"] = EventCallback.Factory.Create<string>(new object(), (string text) => seen.Add(text))
      },
      clock);
    await using (renderer)
    {
#pragma warning disable CA2025 // Dispose must cancel this in-flight wait.
      Task pending = NotifyAsync(bar, "acme");
      bar.Dispose();
#pragma warning restore CA2025
      clock.Release();
      await pending;
    }

    seen.ShouldBeEmpty();
  }

  private static Task Schedule(SearchDebouncer debouncer, List<string> seen, string text) =>
    debouncer.ScheduleAsync(text, TimeSpan.FromMilliseconds(300), immediate: false, Publish(seen));

  private static Func<string, Task> Publish(List<string> seen) =>
    text =>
    {
      seen.Add(text);
      return Task.CompletedTask;
    };

  private static Task NotifyAsync(FilterBar bar, string? text)
  {
    MethodInfo method = typeof(FilterBar)
      .GetMethod("OnSearchChanged", BindingFlags.Instance | BindingFlags.NonPublic)
      .ShouldNotBeNull();
    return (Task)method.Invoke(bar, [text])!;
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider provider = Services(TimeProvider.System);
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<FilterBar>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static async Task<(FilterBar Bar, HtmlRenderer Renderer)> MountAsync(
    Dictionary<string, object?> parameters,
    TimeProvider? clock = null)
  {
    ServiceProvider provider = Services(clock ?? TimeProvider.System);
    HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    FilterBar? bar = null;
    Dictionary<string, object?> attributes = new(parameters)
    {
      ["Capture"] = (Action<FilterBar>)(captured => bar = captured)
    };
    await renderer.Dispatcher.InvokeAsync(async () =>
      await renderer.RenderComponentAsync<FilterBarHost>(ParameterView.FromDictionary(attributes)));
    bar.ShouldNotBeNull();
    return (bar, renderer);
  }

  private static ServiceProvider Services(TimeProvider clock)
  {
    ServiceCollection services = new();
    services.AddLogging();
    services.AddFluentUIComponents();
    services.AddSingleton(clock);
    services.AddSingleton<IJSRuntime, NoopJsRuntime>();
    return services.BuildServiceProvider();
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-filter-bar", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "filter bar rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  public sealed class FilterBarHost : ComponentBase
  {
    [Parameter] public Action<FilterBar>? Capture { get; set; }
    [Parameter] public TimeSpan SearchDelay { get; set; } = TimeSpan.FromMilliseconds(300);
    [Parameter] public EventCallback<string> SearchTextChanged { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<FilterBar>(0);
      builder.AddAttribute(1, "SearchDelay", SearchDelay);
      builder.AddAttribute(2, "SearchTextChanged", SearchTextChanged);
      builder.AddComponentReferenceCapture(3, instance => Capture?.Invoke((FilterBar)instance));
      builder.CloseComponent();
    }
  }

  public sealed class NoopJsRuntime : IJSRuntime
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);
  }

  private sealed class ManualDelay : TimeProvider
  {
    private readonly List<(TimerCallback Callback, object? State)> Waits = [];

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
      lock (Waits)
      {
        Waits.Add((callback, state));
      }

      return new IdleTimer();
    }

    public void Release()
    {
      (TimerCallback Callback, object? State)[] pending;
      lock (Waits)
      {
        pending = [.. Waits];
        Waits.Clear();
      }

      foreach ((TimerCallback callback, object? state) in pending)
      {
        callback(state);
      }
    }

    private sealed class IdleTimer : ITimer
    {
      public bool Change(TimeSpan dueTime, TimeSpan period) => true;

      public void Dispose()
      {
      }

      public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
  }
}
