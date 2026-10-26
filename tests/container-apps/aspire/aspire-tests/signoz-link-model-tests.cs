#region Purpose
// AppHost model inspection (task 417): a configured absolute https URL adds a run-mode signoz
// link; unset adds nothing; an invalid value fails startup; publish mode never adds the resource.
#endregion

#region Design
// Model-only, like state-dev-tools-model-tests: DistributedApplicationTestingBuilder.CreateAsync runs
// the AppHost entry point up to Build, so nothing starts. Command-line configuration overrides user
// secrets, so each fact passes Observability:SigNoz:Url explicitly (an empty value is the unset
// case). The dashboard link text is the ResourceUrlAnnotation DisplayText AddExternalService
// records at build time. The publish fact passes a valid URL and still expects no signoz resource.
#endregion

namespace Aspire.Tests;

[TestTag("Integration")]
public class SigNozLinkModel_Given_
{
  private const string ConfigurationArgumentPrefix = "--Observability:SigNoz:Url=";
  private const string ResourceName = "signoz";
  private const string DisplayText = "SigNoz";
  private const string ValidUrl = "https://observability.example/traces";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SigNozLinkModel_Given_>();

  public static async Task RunMode_Should_AddSigNozLinkWhenUrlIsSet()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync(ValidUrl);

    ExternalServiceResource signoz = appHost.Resources.OfType<ExternalServiceResource>()
      .Single(resource => resource.Name == ResourceName);
    ResourceUrlAnnotation link = signoz.Annotations.OfType<ResourceUrlAnnotation>().Single();
    link.Url.ShouldBe(new Uri(ValidUrl).ToString());
    link.DisplayText.ShouldBe(DisplayText);
    signoz.Annotations.OfType<HealthCheckAnnotation>().ShouldBeEmpty();
  }

  public static async Task RunMode_Should_AddNothingWhenUrlIsUnset()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreateRunModeAsync("");

    appHost.Resources.ShouldNotContain(resource => resource.Name == ResourceName);
  }

  [Input("http://observability.example")]
  [Input("observability.example")]
  [Input("https://")]
  public static async Task InvalidUrl_Should_FailStartup(string candidate)
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
      async () => await CreateRunModeAsync(candidate));

    exception.Message.ShouldContain("Observability:SigNoz:Url");
    exception.Message.ShouldContain("https");
  }

  public static async Task PublishMode_Should_OmitSigNozWhenUrlIsSet()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", "--step", "publish-compose", ConfigurationArgumentPrefix + ValidUrl],
        (_, settings) => settings.EnvironmentName = "Production");

    appHost.Resources.ShouldNotContain(resource => resource.Name == ResourceName);
  }

  private static Task<IDistributedApplicationTestingBuilder> CreateRunModeAsync(string url) =>
    DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
      [ConfigurationArgumentPrefix + url]);
}
