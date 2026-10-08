#region Purpose
// Reports whether a host registered an IChatClient for the in-app ask UI.
#endregion

#region Design
// The template never registers a client. Ask mode is the presence of IChatClient in DI, so a
// generated app stays fully usable with no model and no secret. WebMCP does not consult this:
// an external browser agent needs no in-app model.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using Microsoft.Extensions.AI;

public static class CatalogAgentAvailability
{
  public static bool IsConfigured(IServiceProvider serviceProvider)
  {
    ArgumentNullException.ThrowIfNull(serviceProvider);
    return serviceProvider.GetService<IChatClient>() is not null;
  }
}
