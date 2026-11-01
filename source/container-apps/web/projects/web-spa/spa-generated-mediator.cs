#region Purpose
// Unique DI entry for the SPA ClientPipeline generated mediator.
#endregion

#region Design
// AddGeneratedMediator is internal to TimeWarp.Mediator.Generated in the emitting assembly.
// Web.Server and the SPA integration tests call this public wrapper. The wrapper is compiled
// here, where only this host's generator exists.
#endregion

namespace TimeWarp.Architecture.Web.Spa;

using TimeWarp.Mediator.Generated;

public static class SpaGeneratedMediator
{
  public static IServiceCollection AddWebSpaGeneratedMediator(this IServiceCollection serviceCollection) =>
    serviceCollection.AddGeneratedMediator<ClientPipeline>();
}
