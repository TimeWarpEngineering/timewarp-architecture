#region Purpose
// JS implementation of the WebMCP model context for the interactive circuit.
#endregion

#region Design
// Registered as this concrete type, not as IWebMcpModelContext, so a test can see that the
// template did not register the interface and can pass its own context to WebMcpRegistration.
// import() during prerender, or a disconnected circuit, is an absent API: registered 0.
// The module feature-detects document.modelContext, navigator.modelContext, and provideContext.
#endregion

namespace TimeWarp.Architecture.Services;

using TimeWarp.Architecture.Features.Applications;

/// <summary>Calls <c>web-mcp.js</c> Replace with the dispatcher as the tool host.</summary>
[SideEffectService]
public sealed class JsWebMcpModelContext : IWebMcpModelContext, IAsyncDisposable
{
  private readonly IJSRuntime JsRuntime;
  private readonly WebMcpDispatcher Dispatcher;
  private DotNetObjectReference<WebMcpDispatcher>? Host;

  public JsWebMcpModelContext(IJSRuntime jsRuntime, WebMcpDispatcher dispatcher)
  {
    JsRuntime = jsRuntime;
    Dispatcher = dispatcher;
  }

  public async ValueTask<WebMcpApplyResult> ReplaceAsync
  (
    IReadOnlyList<WebMcpToolDescriptor> tools,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(tools);
    Host ??= DotNetObjectReference.Create(Dispatcher);
    IJSObjectReference? module = null;
    try
    {
      module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, WebMcpJsModule.Specifier);
      return await module.InvokeAsync<WebMcpApplyResult>
      (
        WebMcpJsModule.ReplaceExport,
        cancellationToken,
        Host,
        tools
      );
    }
    catch (Exception exception) when
    (
      exception is JSException or JSDisconnectedException or InvalidOperationException
    )
    {
      return new WebMcpApplyResult(Available: false, Registered: 0);
    }
    finally
    {
      if (module is not null)
      {
        try
        {
          await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
          // The circuit is already gone.
        }
      }
    }
  }

  public ValueTask DisposeAsync()
  {
    Host?.Dispose();
    Host = null;
    return ValueTask.CompletedTask;
  }
}
