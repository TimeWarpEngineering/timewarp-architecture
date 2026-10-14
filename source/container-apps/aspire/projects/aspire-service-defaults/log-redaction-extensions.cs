#region Purpose
// Registers redactors for the DataTaxonomy classes and turns on log redaction for every server host.
#endregion

#region Design
// Task 344. EnableRedaction swaps in the extended logger factory, which redacts classified tags
// once, before fan-out to providers, so console, FakeLogger and the OpenTelemetry log exporter all
// receive the erased value; there is no per-provider wiring. Only values carrying a
// DataClassification are redacted, and only the [LoggerMessage] generator from
// Microsoft.Extensions.Telemetry.Abstractions records that classification: a host that tags
// parameters must reference that package (analyzers do not flow through project references).
// LoggerMessage.Define and ILogger.LogX calls carry no classification and log unchanged.
// Every class uses ErasingRedactor in every environment: a dev box still exports to the Aspire
// dashboard, and the development mail pickup file already holds the unredacted message. The
// fallback is ErasingRedactor too, so an unmapped future class fails closed.
#endregion

namespace Microsoft.Extensions.Hosting;

using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using TimeWarp.Architecture.Compliance;

public static class LogRedactionExtensions
{
  public static IHostApplicationBuilder AddLogRedaction(this IHostApplicationBuilder builder)
  {
    builder.Logging.EnableRedaction();

    builder.Services.AddRedaction(redaction =>
    {
      redaction.SetRedactor<ErasingRedactor>(
        new DataClassificationSet(DataTaxonomy.PersonalData),
        new DataClassificationSet(DataTaxonomy.SensitiveData));
      redaction.SetFallbackRedactor<ErasingRedactor>();
    });

    return builder;
  }
}
