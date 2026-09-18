#region Purpose
// Unique DI entry so web-server can register the generated mediator without CS0121.
#endregion

#region Design
// Web.Spa and this assembly both emit GeneratedMediatorServiceCollectionExtensions in
// Microsoft.Extensions.DependencyInjection (TimeWarp.Mediator 14.0.0-beta.1 / issue 63).
// web-server references both, so it must not call AddGeneratedMediator. This uniquely named
// wrapper is compiled here, where Web.Spa is not referenced.
#endregion

namespace TimeWarp.Architecture.Web.Application;

public static class WebApplicationMediatorRegistration
{
  public static IServiceCollection AddWebApplicationGeneratedMediator(this IServiceCollection serviceCollection) =>
    serviceCollection.AddGeneratedMediator();
}
