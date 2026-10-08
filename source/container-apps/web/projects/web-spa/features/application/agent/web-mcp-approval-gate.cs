#region Purpose
// Holds a WebMCP tool call until the person approves or rejects it in the shell.
#endregion

#region Design
// The browser calls InvokeTool and waits. The dispatcher arms this gate, dispatches
// ShowApproval, and awaits the result. ResolveApproval completes the gate.
// Continuations run asynchronously so Execute is not nested inside the resolve handler.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>One in-flight WebMCP confirmation.</summary>
public sealed class WebMcpApprovalGate
{
  private TaskCompletionSource<bool>? Pending;

  public void Arm()
  {
    Pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
  }

  public Task<bool> WaitAsync(CancellationToken cancellationToken)
  {
    TaskCompletionSource<bool>? pending = Pending;
    if (pending is null)
    {
      return Task.FromResult(false);
    }

    return pending.Task.WaitAsync(cancellationToken);
  }

  public void Complete(bool approved)
  {
    Pending?.TrySetResult(approved);
    Pending = null;
  }
}
