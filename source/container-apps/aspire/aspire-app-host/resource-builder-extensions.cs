#region Purpose
// Adds an Aspire dashboard command that opens a resource's Scalar OpenAPI UI in the local browser.
#endregion

#region Design
// Local-dev convenience only: Process.Start with UseShellExecute launches the default browser on the
// AppHost machine, so the command is meaningless in remote or containerized runs.
// UpdateState gates the command on Healthy so the dashboard never offers a link to a dead endpoint.
// Hard-wired to the "https" endpoint; a resource without one fails at click time, surfaced via the
// caught exception rather than crashing the AppHost.
#endregion

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
            return Task.FromResult(new ExecuteCommandResult { Success = false, Message = e.Message });
          }
        },
        new CommandOptions
        {
          UpdateState = context => context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy
            ? ResourceCommandState.Enabled
            : ResourceCommandState.Disabled,
          IconName = "Document",
          IconVariant = IconVariant.Filled
        }
      );
  }
}
