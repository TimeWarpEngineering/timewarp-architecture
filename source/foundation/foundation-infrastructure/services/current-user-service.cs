#region Purpose
// Exposes the authenticated user's identity to handlers without coupling them to HttpContext.
#endregion

#region Design
// Claims are read once in the constructor, so this must be registered scoped — a singleton would
// pin the first request's user for the process lifetime.
// The claim type is nameof(UserId): token issuance must emit a "UserId" claim or every request
// appears anonymous. Absent claim yields null/false defaults rather than throwing, letting
// anonymous endpoints share the same abstraction.
#endregion

namespace TimeWarp.Foundation.Services;

/// <summary>
/// Scoped <see cref="ICurrentUserService"/> that reads the <c>UserId</c> claim from the current HTTP user.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
  /// <summary>
  /// Authenticated user id from the <c>UserId</c> claim, or null when absent.
  /// </summary>
  public Guid? UserId { get; }
  /// <summary>
  /// True when the current HTTP user is authenticated.
  /// </summary>
  public bool IsAuthenticated { get; }
  /// <summary>
  /// Reads identity claims once from the current <see cref="IHttpContextAccessor"/>.
  /// </summary>
  public CurrentUserService(IHttpContextAccessor httpContextAccessor)
  {
    string? claim  = httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType: nameof(UserId));
    if (claim is null) return;
    UserId = Guid.Parse(claim);
    IsAuthenticated = httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
  }
}
