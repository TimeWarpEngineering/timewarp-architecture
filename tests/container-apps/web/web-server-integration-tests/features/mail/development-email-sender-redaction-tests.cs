#region Purpose
// Prove service-defaults log redaction erases classified DevelopmentEmailSender values before both
// an in-process log provider (FakeLogger) and the OpenTelemetry log export pipeline see them.
#endregion

#region Design
// Task 344. Builds a plain HostApplicationBuilder with AddServiceDefaults (no web host, no OTLP
// endpoint) so the real EnableRedaction + AddRedaction wiring runs. A capturing BaseExporter is
// attached through ConfigureOpenTelemetryLoggerProvider: it sits at the same point the OTLP exporter
// would, so an erased attribute there means an erased attribute on the wire. The recipient
// ([PersonalData]) and body ([SensitiveData]) must be absent; the unclassified subject must survive,
// which proves the message itself was logged and only the classified values were erased.
#endregion

namespace DevelopmentEmailSender_;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry;
using OpenTelemetry.Logs;
using TimeWarp.Architecture.Mail;

public class SendAsync_Should
{
  private const string Recipient = "client.person@example.com";
  private const string Subject = "Quarterly statement ready";
  private const string Body = "Account 12345678 balance details";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SendAsync_Should>();

  public static async Task Redact_Classified_Values_Before_Log_Providers_And_Otel_Export()
  {
    CapturingLogExporter exporter = new();
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(
      new HostApplicationBuilderSettings { EnvironmentName = Environments.Production, DisableDefaults = true });
    builder.AddServiceDefaults();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    builder.Logging.AddFakeLogging();
    builder.Services.ConfigureOpenTelemetryLoggerProvider(
      logging => logging.AddProcessor(new SimpleLogRecordExportProcessor(exporter)));
    builder.Services.Configure<MailOptions>(_ => { });
    builder.Services.AddSingleton<DevelopmentEmailSender>();

    using (IHost host = builder.Build())
    {
      await host.Services.GetRequiredService<DevelopmentEmailSender>()
        .SendAsync(new EmailMessage(Recipient, Subject, Body));

      IReadOnlyList<FakeLogRecord> records = host.Services.GetFakeLogCollector().GetSnapshot();
      FakeLogRecord mail = records.Single(record => record.Id.Id == 1);
      mail.GetStructuredStateValue("To").ShouldBe(string.Empty);
      mail.GetStructuredStateValue("Subject").ShouldBe(Subject);
      FakeLogRecord body = records.Single(record => record.Id.Id == 2);
      body.GetStructuredStateValue("Body").ShouldBe(string.Empty);
      foreach (FakeLogRecord record in records)
      {
        record.Message.ShouldNotContain(Recipient);
        record.Message.ShouldNotContain(Body);
      }
    }

    exporter.Exported.ShouldContain(text => text.Contains(Subject, StringComparison.Ordinal));
    exporter.Exported.ShouldAllBe(text => !text.Contains(Recipient, StringComparison.Ordinal));
    exporter.Exported.ShouldAllBe(text => !text.Contains(Body, StringComparison.Ordinal));
  }

  private sealed class CapturingLogExporter : BaseExporter<LogRecord>
  {
    public List<string> Exported { get; } = [];

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
      foreach (LogRecord record in batch)
      {
        IEnumerable<string> attributes = (record.Attributes ?? [])
          .Select(attribute => $"{attribute.Key}={attribute.Value}");
        Exported.Add($"{record.FormattedMessage} | {record.Body} | {string.Join(", ", attributes)}");
      }

      return ExportResult.Success;
    }
  }
}
