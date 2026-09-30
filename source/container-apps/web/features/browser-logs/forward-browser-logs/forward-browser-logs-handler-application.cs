#region Purpose
// Server-side handler that relays forwarded browser console entries into ILogger (category Web.Spa.Browser).
#endregion

#region Design
// Order: environment gate (404, fail-closed) -> rate limit (429) -> redact + log -> Accepted count.
// The shared SharedProblemDetails Status drives the HTTP status in the generated endpoint.
// Entries are logged with a structured template (source / page path / message as separate
// attributes) via the LoggerMessage generator; level is mapped from the browser level string.
#endregion

namespace TimeWarp.Architecture.Features.BrowserLogs.Application;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static TimeWarp.Architecture.Features.BrowserLogs.ForwardBrowserLogs;

public sealed partial class ForwardBrowserLogs
{
  public sealed partial class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IHostEnvironment Environment;
    private readonly BrowserLogRateLimiter RateLimiter;
    private readonly ILogger Logger;

    public Handler(IHostEnvironment environment, BrowserLogRateLimiter rateLimiter, ILoggerFactory loggerFactory)
    {
      Environment = environment;
      RateLimiter = rateLimiter;
      Logger = loggerFactory.CreateLogger(BrowserLogForwarding.CategoryName);
    }

    public Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      if (!BrowserLogForwarding.IsEnabled(Environment))
      {
        return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new SharedProblemDetails
        {
          Title = "Not found",
          Status = 404,
          Detail = "Browser log forwarding is only available in Development and Testing."
        });
      }

      if (!RateLimiter.TryAcquire(command.Entries.Count))
      {
        return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new SharedProblemDetails
        {
          Title = "Too many requests",
          Status = 429,
          Detail = "Browser log forwarding rate limit exceeded."
        });
      }

      string pagePath = command.PagePath ?? string.Empty;

      foreach (Entry entry in command.Entries)
      {
        LogLevel level = ToLogLevel(entry.Level);

        if (Logger.IsEnabled(level))
        {
          string message = BrowserLogRedactor.Redact(entry.Message);
          LogEntry(level, entry.Source, pagePath, message);
        }
      }

      return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response { Accepted = command.Entries.Count });
    }

    private static LogLevel ToLogLevel(string level) => level switch
    {
      "error" => LogLevel.Error,
      "warn" => LogLevel.Warning,
      "debug" => LogLevel.Debug,
      _ => LogLevel.Information
    };

    [LoggerMessage(Message = "Browser {BrowserLogSource} {BrowserLogPagePath}: {BrowserLogMessage}")]
    private partial void LogEntry(LogLevel level, string browserLogSource, string browserLogPagePath, string browserLogMessage);
  }
}
