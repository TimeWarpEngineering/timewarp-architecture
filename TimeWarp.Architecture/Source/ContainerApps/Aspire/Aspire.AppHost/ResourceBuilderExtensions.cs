namespace Aspire.Customization.AppHost;

internal static class ResourceBuilderExtensions
{
  internal static IResourceBuilder<T> WithScalar<T>(this IResourceBuilder<T> resourceBuilder)
    where T : IResourceWithEndpoints
  {
    return resourceBuilder.WithOpenApiDocs(name: "scalar-docs", displayName: "Scalar API Documentation",
      openApiUiPath: "scalar/v1");
  }

  private static IResourceBuilder<T> WithOpenApiDocs<T>
  (
    this IResourceBuilder<T> resourceBuilder,
    string name,
    string displayName,
    string openApiUiPath
  ) where T : IResourceWithEndpoints
  {
    return resourceBuilder
      .WithCommand
      (
        name,
        displayName,
        executeCommand: _ =>
        {
          try
          {
            //Base URL
            EndpointReference endpoint = resourceBuilder.GetEndpoint("https");
            string url = $"{endpoint.Url}/{openApiUiPath}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return Task.FromResult(new ExecuteCommandResult { Success = true });
          }
          catch (Exception e)
          {
            return Task.FromResult(new ExecuteCommandResult { Success = false, ErrorMessage = e.Message });
          }
        },
        updateState: context => context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy
          ? ResourceCommandState.Enabled
          : ResourceCommandState.Disabled,
        iconName: "Document",
        iconVariant: IconVariant.Filled
      );
  }
}
