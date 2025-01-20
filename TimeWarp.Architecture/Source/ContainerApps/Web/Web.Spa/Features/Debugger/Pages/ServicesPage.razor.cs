namespace TimeWarp.Architecture.Pages;

[Page("/Services")]
partial class ServicesPage
{
  public static string Title => "Services";

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
