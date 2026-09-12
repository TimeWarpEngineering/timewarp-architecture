#region Purpose
// Abstraction exposing the authenticated user's id so application code never reaches into HttpContext or auth plumbing.
#endregion

#region Open Questions
// Q1 (2026-09-12, code-review): Should UserId be a strongly typed id? The repo already has TypedId infrastructure.
#endregion

namespace TimeWarp.Foundation.Abstractions;

public interface ICurrentUserService
{
  Guid? UserId { get; }
}
