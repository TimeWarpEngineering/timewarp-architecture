#region Purpose
// Unique DI entry for the SPA ClientPipeline generated mediator.
#endregion

#region Design
// Web.Server and SPA integration tests reference this assembly, whose generated
// AddGeneratedMediator would CS0121 with their own generator (Mediator issue 63).
// This uniquely named wrapper is compiled here, where only this host's generator exists.
#endregion

namespace TimeWarp.Architecture.Web.Spa;

public static class SpaGeneratedMediator
{
  public static IServiceCollection AddWebSpaGeneratedMediator(this IServiceCollection serviceCollection) =>
    serviceCollection.AddGeneratedMediator<ClientPipeline>();
}
