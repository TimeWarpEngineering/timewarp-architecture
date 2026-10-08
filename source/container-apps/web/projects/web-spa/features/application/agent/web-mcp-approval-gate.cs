#region Purpose
// Holds one WebMCP tool call until the person approves or rejects that exact call in the shell.
#endregion

#region Design
// One slot, correlated by id. TryBegin hands the dispatcher a fresh call id and its own decision
// task; a second call while the slot is taken is refused (the dispatcher returns a "waiting for
// confirmation" error) so it can never overwrite the call the person is looking at. Complete
// resolves only the call with the matching id and frees the slot; a stale id is a no-op, so a
// late click cannot release a different call. Navigation cancels by completing the id with false.
// Continuations run asynchronously so Execute is not nested inside the resolve handler. The lock
// matters only for the multi-threaded test host; the browser is single-threaded.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>The single in-flight WebMCP confirmation, correlated by call id.</summary>
public sealed class WebMcpApprovalGate
{
  private readonly Lock Sync = new();
  private Guid? PendingId;
  private TaskCompletionSource<bool>? PendingDecision;

  /// <summary>Claims the slot for a new call. False when another call is already waiting.</summary>
  public bool TryBegin(out Guid callId, out Task<bool> decision)
  {
    lock (Sync)
    {
      if (PendingId is not null)
      {
        callId = Guid.Empty;
        decision = Task.FromResult(false);
        return false;
      }

      callId = Guid.NewGuid();
      PendingId = callId;
      PendingDecision = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      decision = PendingDecision.Task;
      return true;
    }
  }

  /// <summary>Resolves the call with this id and frees the slot. False for a stale or unknown id.</summary>
  public bool Complete(Guid callId, bool approved)
  {
    TaskCompletionSource<bool>? decision;
    lock (Sync)
    {
      if (PendingId != callId)
      {
        return false;
      }

      decision = PendingDecision;
      PendingId = null;
      PendingDecision = null;
    }

    decision?.TrySetResult(approved);
    return true;
  }
}
