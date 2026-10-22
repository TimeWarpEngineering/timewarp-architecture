#region Purpose
// Holds FilterBar's search timer so a pause in typing raises one callback.
#endregion

#region Design
// The wait lives here, not on the page. A page-level timer would be local UI state, and a
// per-keystroke callback would fetch once per character. The page only dispatches an action
// when this type publishes. TimeProvider is the clock so a test completes the wait without
// sleeping. Each schedule cancels the previous wait. An immediate schedule (cleared text)
// publishes without waiting. Dispose cancels the pending wait and drops later schedules.
// FilterBar sends the publish through ComponentBase.InvokeAsync.
#endregion

namespace TimeWarp.Architecture.Components;

internal sealed class SearchDebouncer : IDisposable
{
  private readonly TimeProvider Clock;
  private CancellationTokenSource? Pending;
  private int Disposed;

  public SearchDebouncer(TimeProvider clock)
  {
    Clock = clock;
  }

  public async Task ScheduleAsync(string text, TimeSpan delay, bool immediate, Func<string, Task> publish)
  {
    CancelPending();
    if (Volatile.Read(ref Disposed) != 0)
    {
      return;
    }

    if (immediate || delay <= TimeSpan.Zero)
    {
      await publish(text);
      return;
    }

    CancellationTokenSource source = new();
    CancellationToken token = source.Token;
    Pending = source;
    try
    {
      await Task.Delay(delay, Clock, token);
      if (Volatile.Read(ref Disposed) != 0 || token.IsCancellationRequested)
      {
        return;
      }

      if (ReferenceEquals(Pending, source))
      {
        Pending = null;
      }

      await publish(text);
    }
    catch (OperationCanceledException)
    {
    }
    finally
    {
      if (ReferenceEquals(Pending, source))
      {
        Pending = null;
      }

      source.Dispose();
    }
  }

  public void Dispose()
  {
    if (Interlocked.Exchange(ref Disposed, 1) != 0)
    {
      return;
    }

    Pending?.Cancel();
    Pending?.Dispose();
    Pending = null;
  }

  private void CancelPending()
  {
    if (Pending is null)
    {
      return;
    }

    CancellationTokenSource source = Pending;
    Pending = null;
    source.Cancel();
  }
}
