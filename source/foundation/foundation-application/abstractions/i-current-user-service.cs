#region Purpose
// Abstraction exposing the authenticated user's id so application code never reaches into HttpContext or auth plumbing.
#endregion

#region Open Questions
// Q1 (2026-09-12, code-review): Should UserId be a strongly typed id? The repo already has TypedId infrastructure.
#endregion

namespace TimeWarp.Foundation.Abstractions;

/// <summary>
/// Exposes the authenticated caller's identity to application code without HttpContext coupling.
/// </summary>
public interface ICurrentUserService
{
  /// <summary>
  /// Authenticated user id when present; null for anonymous requests.
  /// </summary>
  Guid? UserId { get; }
}
