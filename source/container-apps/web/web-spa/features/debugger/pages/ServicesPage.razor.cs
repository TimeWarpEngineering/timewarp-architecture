#region Purpose
// Debug page listing the mediator pipeline registrations resolved from DI.
#endregion

#region Design
// Pipeline ordering and duplicate registrations are invisible at runtime; this page
// exposes them by injecting IServiceCollection, which program.cs registers into
// itself as a singleton specifically to enable this introspection.
// Diagnostic-only — nothing here should be load-bearing for features.
#endregion

namespace TimeWarp.Architecture.Features.Debugger;

[Page("/Services")]
partial class ServicesPage
{

  [Inject]
  private IServiceCollection ServiceCollection { get; set; } = null!;

  private List<ServiceDescriptor> PipelineBehaviors => FilterServices(typeof(IPipelineBehavior<,>));
  private List<ServiceDescriptor> RequestPreProcessors => FilterServices(typeof(IRequestPreProcessor<>));
  private List<ServiceDescriptor> RequestPostProcessors => FilterServices(typeof(IRequestPostProcessor<,>));
  private List<ServiceDescriptor> StreamPipelineBehaviors => FilterServices(typeof(IStreamPipelineBehavior<,>));

  private List<ServiceDescriptor> FilterServices(Type serviceType) =>
    ServiceCollection.Where
      (s => s.ServiceType == serviceType || s.ServiceType.GetInterfaces().Contains(serviceType)).ToList();
}
