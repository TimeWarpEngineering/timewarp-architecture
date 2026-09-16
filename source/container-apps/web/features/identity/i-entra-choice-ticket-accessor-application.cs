#region Purpose
// Request-scoped access to the parked Entra bootstrap choice cookie.
#endregion

#region Design
// Handlers must not take IHttpContextAccessor (web-application has no ASP.NET Http package).
// The server adapter reads/clears the HttpOnly cookie; Set happens in EntraTicketHttp on park.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public interface IEntraChoiceTicketAccessor
{
  bool TryReadParkId(out string parkId);
  void Clear();
}
