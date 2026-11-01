#region Purpose
// Unique DI entry so web-server can register the generated mediator without CS0121.
#endregion

#region Design
// Each host emits an internal AddGeneratedMediator in TimeWarp.Mediator.Generated.
// web-server references both Web.Spa and this assembly, so it calls this wrapper
// instead of the generated method. The wrapper is compiled here, where Web.Spa is not referenced.
#endregion

namespace TimeWarp.Architecture.Web.Application;

using TimeWarp.Mediator.Generated;

public static class WebApplicationMediatorRegistration
{
  public static IServiceCollection AddWebApplicationGeneratedMediator(this IServiceCollection serviceCollection) =>
    serviceCollection.AddGeneratedMediator();
}
