#region Purpose
// Adds the optional run-mode SigNoz dashboard link configured by Observability:SigNoz:Url.
#endregion

#region Design
// SigNoz (task 417): Observability:SigNoz:Url is operator configuration (AppHost user secrets or
// Observability__SigNoz__Url), never a committed template value, because this file ships in every
// generated app. An absolute https URL adds a run-mode external service named signoz. The dashboard
// link text is "SigNoz" and the target is that URL. Unset or blank adds nothing. Any other value
// throws at startup and names the key. AddExternalService (Aspire.Hosting 13.6.4) already excludes
// the resource from the manifest; the call is also IsRunMode-only, so publish and deploy models
// never contain it. There is no HTTP health check: the resource is a link, and a probe would mark
// it unhealthy when that host is unreachable from this machine. No telemetry is exported there.
#endregion

namespace TimeWarp.Architecture.Aspire;

/// <summary>The SigNoz external-service link on the Aspire dashboard (task 417).</summary>
internal static class SigNozDashboardLink
{
  /// <summary>
  /// Adds the link when the URL is set. Outside every template flag: every generated app can link it.
  /// </summary>
  internal static void Add(IDistributedApplicationBuilder builder)
  {
    const string configurationKey = "Observability:SigNoz:Url";
    const string resourceName = "signoz";
    const string displayText = "SigNoz";

    string? configuredUrl = builder.Configuration[configurationKey];
    if (string.IsNullOrWhiteSpace(configuredUrl))
    {
      return;
    }

    string candidate = configuredUrl.Trim();
    if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? signozUri)
      || signozUri.Scheme != Uri.UriSchemeHttps
      || string.IsNullOrEmpty(signozUri.Host))
    {
      throw new InvalidOperationException(
        $"{configurationKey} '{candidate}' must be an absolute https URL.");
    }

    if (!builder.ExecutionContext.IsRunMode)
    {
      return;
    }

    // AddExternalService already records the URL. A second WithUrl would add another link.
    IResourceBuilder<ExternalServiceResource> signoz = builder.AddExternalService(resourceName, signozUri);
    ResourceUrlAnnotation dashboardLink = signoz.Resource.Annotations.OfType<ResourceUrlAnnotation>().Single();
    dashboardLink.DisplayText = displayText;
  }
}
