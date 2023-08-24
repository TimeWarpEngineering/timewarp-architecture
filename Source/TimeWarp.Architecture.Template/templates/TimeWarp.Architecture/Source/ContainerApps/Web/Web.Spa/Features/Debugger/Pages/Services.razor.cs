namespace TimeWarp.Architecture.Pages;

[Page("/services")]
public partial class Services
{
  [Inject]
  private IServiceCollection ServiceCollection { get; set; } = null!;

  private List<ServiceDescriptor> IPipelineBehaviors => FilterServices(typeof(IPipelineBehavior<,>));
  private List<ServiceDescriptor> IRequestPreProcessors => FilterServices(typeof(IRequestPreProcessor<>));
  private List<ServiceDescriptor> IRequestPostProcessors => FilterServices(typeof(IRequestPostProcessor<,>));
  private List<ServiceDescriptor> IStreamPipelineBehaviors => FilterServices(typeof(IStreamPipelineBehavior<,>));

  private List<ServiceDescriptor> FilterServices(Type serviceType)
  {
    return ServiceCollection.Where(s => s.ServiceType == serviceType || s.ServiceType.GetInterfaces().Contains(serviceType)).ToList();
  }
}
