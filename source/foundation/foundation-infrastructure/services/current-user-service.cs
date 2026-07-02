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

public class CurrentUserService : ICurrentUserService
{
  public Guid? UserId { get; }
  public bool IsAuthenticated { get; }
  public CurrentUserService(IHttpContextAccessor httpContextAccessor)
  {
    string? claim  = httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType: nameof(UserId));
    if (claim is null) return;
    UserId = Guid.Parse(claim);
    IsAuthenticated = httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
  }
}
