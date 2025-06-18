namespace TimeWarp.Architecture.Services;

public class CurrentUserService : ICurrenUserService
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
