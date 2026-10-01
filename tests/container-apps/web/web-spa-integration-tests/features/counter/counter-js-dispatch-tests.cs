#region Purpose
// Counter JS-dispatch demo: the page's JavaScript onclick → Spa.Counter → timeWarpState.DispatchRequest
// path names a real action and changes the store count.
#endregion

#region Design
// Task 265: CounterPage no longer round-trips through a C# handler and IJSRuntime (TWA0026); the
// button's JavaScript onclick calls Spa.Counter.DispatchIncrementCountAction (counter.ts), which
// dispatches by assembly-qualified type name through TimeWarp.State's JsonRequestHandler.Handle —
// the [JSInvokable] that timeWarpState.DispatchRequest calls. No browser here (workers never start
// an AppHost), so the test pins the two agreements a browser run would exercise:
//   1. CounterPage.razor's onclick names a function counter.ts exports;
//   2. the action name and payload in counter.ts, fed to JsonRequestHandler.Handle exactly as the
//      JS sends them, resolve to IncrementCounterActionSet.Action and add 7 to CounterState.Count.
// Sources are read from the repo so renaming the action, the export or the payload field fails here.
// C-create AnalyticsSpaTestApplication (host-free ServiceProvider, fake IJSRuntime) — JsonRequestHandler
// resolves from DI exactly as in the browser, and no AppHost or HostGraph boots.
#endregion

namespace CounterState_;

using System.Text.RegularExpressions;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Analytics;
using TimeWarp.Features.JavaScriptInterop;
using static TimeWarp.Architecture.Features.Counters.CounterState;

[TestTag("Integration")]
public partial class JsDispatch_Should
{
  private static AnalyticsSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<JsDispatch_Should>();

  public static Task SetupOnce()
  {
    Spa = new AnalyticsSpaTestApplication();
    return Task.CompletedTask;
  }

  public static Task CleanUpOnce()
  {
    Spa?.Dispose();
    Spa = null;
    return Task.CompletedTask;
  }

  public static Task CounterPage_Button_Call_A_Counter_Ts_Export()
  {
    string page = File.ReadAllText(Path.Combine(WebSpaDirectory(), "features", "counter", "pages", "CounterPage.razor"));
    string counterTs = File.ReadAllText(Path.Combine(WebSpaDirectory(), "source", "features", "counter.ts"));

    Match onclick = OnclickPattern().Match(page);
    onclick.Success.ShouldBeTrue("CounterPage.razor should carry a JavaScript onclick=\"Spa.Counter.<Export>()\" button");
    page.ShouldNotContain("@inject IJSRuntime");
    page.ShouldNotContain("InvokeVoidAsync");
    counterTs.ShouldContain($"{onclick.Groups["export"].Value}: () =>");
    return Task.CompletedTask;
  }

  public static async Task Increment_Count_By_Seven_When_Dispatched_As_Counter_Ts_Does()
  {
    string counterTs = File.ReadAllText(Path.Combine(WebSpaDirectory(), "source", "features", "counter.ts"));
    Match actionName = ActionNamePattern().Match(counterTs);
    actionName.Success.ShouldBeTrue("counter.ts should declare IncrementCountActionName");
    Match dispatch = DispatchPattern().Match(counterTs);
    dispatch.Success.ShouldBeTrue("counter.ts should call timeWarpState.DispatchRequest(IncrementCountActionName, { amount: N })");

    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.Store.GetState<CounterState>().Initialize(count: 3);
    JsonRequestHandler jsonRequestHandler = scope.ServiceProvider.GetRequiredService<JsonRequestHandler>();

    // JSON.stringify({ amount: 7 }) — the body DispatchRequest hands to the [JSInvokable].
    await jsonRequestHandler.Handle(actionName.Groups["name"].Value, $"{{\"amount\":{dispatch.Groups["amount"].Value}}}");

    Type.GetType(actionName.Groups["name"].Value).ShouldBe(typeof(IncrementCounterActionSet.Action));
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
  }

  private static string WebSpaDirectory()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")))
      {
        return Path.Combine(dir.FullName, "source", "container-apps", "web", "projects", "web-spa");
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
  }

  [GeneratedRegex("""onclick="Spa\.Counter\.(?<export>\w+)\(\)""")]
  private static partial Regex OnclickPattern();

  [GeneratedRegex("""IncrementCountActionName\s*=\s*"(?<name>[^"]+)";""")]
  private static partial Regex ActionNamePattern();

  [GeneratedRegex("""DispatchRequest\(IncrementCountActionName,\s*\{\s*amount:\s*(?<amount>-?\d+)\s*\}\)""")]
  private static partial Regex DispatchPattern();
}
