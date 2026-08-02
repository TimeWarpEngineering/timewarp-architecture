namespace TimeWarp.Architecture.Testing;

#region Purpose
// Owner of a C-create host graph: one class's Api/Web/Yarp instances disposed in reverse boot order.
#endregion

#region Design
// Epic 145 / task 143 §6 C-create: no process-static sharing. HostGraphFactory always returns a
// fresh graph; the Jaribu class that called SetupOnce must DisposeAsync in CleanUpOnce.
// Dispose order is Yarp → Web → Api so dependents shut down before dependencies.
// Per-family conditional-compilation guards (task 145-002 R2-1 fix follow-up, template-smoke SmokeNoApi regression):
// this file ships unconditionally (it is not template-excluded like the Test*ServerApplication
// files, since SOME combination of Api/Web/Yarp is always present), so each property/dispose
// branch that names a family-specific type must be guarded — Test*ServerApplication.cs files are
// template-excluded per family (.template.config/template.json), and an unguarded reference here
// would not compile once that type's file is gone (e.g. `--api false` strips
// applications/api-test-server-application.cs but this file kept referencing ApiTestServerApplication
// unconditionally).
#endregion

/// <summary>
/// Per-class-owned set of in-proc test hosts (fixed ports). Dispose after the class finishes.
/// </summary>
public sealed class HostGraph : IAsyncDisposable
{
#if(api)
  public ApiTestServerApplication? Api { get; init; }
#endif
#if(web)
  public WebTestServerApplication? Web { get; init; }
#endif
#if(yarp)
  public YarpTestServerApplication? Yarp { get; init; }
#endif

  public async ValueTask DisposeAsync()
  {
    // Reverse of HostGraphFactory boot order (Yarp last to start → first to stop).
#if(yarp)
    if (Yarp is not null)
      await Yarp.DisposeAsync().ConfigureAwait(false);
#endif
#if(web)

    if (Web is not null)
      await Web.DisposeAsync().ConfigureAwait(false);
#endif
#if(api)

    if (Api is not null)
      await Api.DisposeAsync().ConfigureAwait(false);
#endif
  }
}
