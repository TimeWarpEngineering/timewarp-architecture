namespace TimeWarp.Architecture.Testing;

#region Purpose
// Owner of a C-create host graph: one class's Api/Web/Yarp instances disposed in reverse boot order.
#endregion

#region Design
// Epic 145 / task 143 §6 C-create: no process-static sharing. HostGraphFactory always returns a
// fresh graph; the Jaribu class that called SetupOnce must DisposeAsync in CleanUpOnce.
// Dispose order is Yarp → Web → Api so dependents shut down before dependencies.
#endregion

/// <summary>
/// Per-class-owned set of in-proc test hosts (fixed ports). Dispose after the class finishes.
/// </summary>
[NotTest]
public sealed class HostGraph : IAsyncDisposable
{
  public ApiTestServerApplication? Api { get; init; }
  public WebTestServerApplication? Web { get; init; }
  public YarpTestServerApplication? Yarp { get; init; }

  public async ValueTask DisposeAsync()
  {
    // Reverse of HostGraphFactory boot order (Yarp last to start → first to stop).
    if (Yarp is not null)
      await Yarp.DisposeAsync().ConfigureAwait(false);

    if (Web is not null)
      await Web.DisposeAsync().ConfigureAwait(false);

    if (Api is not null)
      await Api.DisposeAsync().ConfigureAwait(false);
  }
}
