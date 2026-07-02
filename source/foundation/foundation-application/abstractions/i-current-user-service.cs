#region Purpose
// Abstraction exposing the authenticated user's id so application code never reaches into HttpContext or auth plumbing.
#endregion

namespace TimeWarp.Foundation.Abstractions;

public interface ICurrentUserService
{
  // TODO: Should this be a strongly typed UserId?
  Guid? UserId { get; }
}
