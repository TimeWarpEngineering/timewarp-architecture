#region Purpose
// Renders a page to HTML as if it ran in an interactive circuit, for page tests with no browser.
#endregion

#region Design
// HtmlRenderer's static host reports RendererInfo.IsInteractive false, and pages that fetch in
// OnInitializedAsync or OnParametersSetAsync skip that fetch when they are not interactive. This
// subclass of StaticHtmlRenderer reports an interactive renderer, so the page loads its data.
//
// StaticHtmlRenderer never calls OnAfterRender or OnAfterRenderAsync. It cancels the display
// update, so the after-render step of the component lifecycle does not run, even with this
// RendererInfo. A page that reads state or applies a draft in OnAfterRenderAsync needs the test
// to run that step itself. InvokeAfterRenderAsync does that: it calls the component's
// OnAfterRender(firstRender) and then awaits OnAfterRenderAsync(firstRender), the same order
// ComponentBase uses. Call it where an interactive circuit would: after QuiescenceTask, and after
// each action the test sends to the page. The methods are protected, so it calls them through
// reflection on ComponentBase; the call is virtual, so the page's override runs.
// firstRender defaults to false, because by the time a test calls it the page has rendered once.
// The helper calls the methods directly. A test that needs them on the renderer's thread wraps
// the call in renderer.Dispatcher.InvokeAsync.
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.Extensions.Logging;

/// <summary>Static HTML rendering with <see cref="RendererInfo.IsInteractive"/> set.</summary>
internal sealed class InteractiveHtmlRenderer : StaticHtmlRenderer
{
  private static readonly MethodInfo OnAfterRenderMethod = AfterRenderMethod("OnAfterRender");
  private static readonly MethodInfo OnAfterRenderAsyncMethod = AfterRenderMethod("OnAfterRenderAsync");

  public InteractiveHtmlRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
    : base(services, loggerFactory)
  {
  }

  protected override RendererInfo RendererInfo { get; } = new("Test", isInteractive: true);

  /// <summary>
  /// Runs <c>OnAfterRender</c> and <c>OnAfterRenderAsync</c> on <paramref name="component"/>,
  /// which <see cref="StaticHtmlRenderer"/> never calls.
  /// </summary>
  public static async Task InvokeAfterRenderAsync(ComponentBase component, bool firstRender = false)
  {
    ArgumentNullException.ThrowIfNull(component);
    OnAfterRenderMethod.Invoke(component, [firstRender]);
    Task task = (Task?)OnAfterRenderAsyncMethod.Invoke(component, [firstRender])
      ?? throw new InvalidOperationException("OnAfterRenderAsync returned no task.");
    await task.ConfigureAwait(false);
  }

  private static MethodInfo AfterRenderMethod(string name) =>
    typeof(ComponentBase).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, [typeof(bool)])
      ?? throw new InvalidOperationException($"ComponentBase.{name}(bool) not found.");
}
